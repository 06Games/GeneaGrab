use crate::{
    comm_models::{ImageMeta, UserImageMeta},
    db_entries::{image_entry, registry_entry},
    errors::CoreError,
    plugins::PluginManager,
    services::registry,
};
use futures::StreamExt;
use geneagrab_plugin_core::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, HostPluginBase, TileRequest, TileResponse,
    },
    utils::ImageGeometry,
};
use image::{DynamicImage, GenericImage, RgbImage};
use sea_orm::{
    sea_query::Nullable,
    ActiveValue::{self, Set},
    ColumnTrait, DbConn, EntityTrait, QueryFilter, Value,
};
use std::collections::HashMap;
use std::io::Cursor;

pub fn patch_field<T>(field: &mut ActiveValue<Option<T>>, val: Option<Option<T>>) -> bool
where
    T: Into<Value> + Nullable,
{
    match val {
        None => false,
        Some(None) => {
            *field = Set(None);
            true
        }
        Some(Some(value)) => {
            *field = Set(Some(value));
            true
        }
    }
}

pub(crate) async fn get_image(
    db: &DbConn,
    registry_id: u32,
    image_id: u32,
) -> Result<image_entry::Model, CoreError> {
    image_entry::Entity::find()
        .filter(image_entry::Column::RegistryEntryId.eq(registry_id))
        .filter(image_entry::Column::ImageNumber.eq(image_id))
        .one(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {e}")))?
        .ok_or_else(|| CoreError::NotFound(format!("Image {image_id} not found")))
}

pub(crate) async fn set_image(
    db: &DbConn,
    registry_id: u32,
    image_id: u32,
    image: image_entry::ActiveModel,
) -> Result<(), CoreError> {
    let update_result = image_entry::Entity::update_many()
        .filter(image_entry::Column::RegistryEntryId.eq(registry_id))
        .filter(image_entry::Column::ImageNumber.eq(image_id))
        .set(image)
        .exec(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {e}")))?;

    // Check if any rows were actually updated
    if update_result.rows_affected == 0 {
        return Err(CoreError::NotFound("Image not found to update".into()));
    }

    Ok(())
}

pub async fn fetch_image_meta(
    db: &DbConn,
    registry_id: u32,
    image_id: u32,
) -> Result<ImageMeta, CoreError> {
    log::info!("fetch_image_meta called for registry {registry_id} image {image_id}");

    let image = get_image(db, registry_id, image_id).await?;

    // TODO: Map actual act_types
    let act_types = HashMap::new();

    Ok(ImageMeta {
        name: image.name,
        date_range: image.date_range,
        notes: image.notes,
        image_number: image.image_number,
        act_types,
        width: image.width,
        height: image.height,
        tile_size: image.tile_size,
        ark_url: image.ark_url,
    })
}

pub async fn save_image_meta(
    db: &DbConn,
    registry_id: u32,
    image_id: u32,
    meta: UserImageMeta,
) -> Result<(), CoreError> {
    log::info!("save_image_meta called for registry {registry_id} image {image_id}");

    let mut update_model = image_entry::ActiveModel {
        ..Default::default()
    };
    let mut has_updates = false;

    // Set only the fields provided in the patch payload
    has_updates |= patch_field(&mut update_model.name, meta.name);
    has_updates |= patch_field(&mut update_model.date_range, meta.date_range);
    has_updates |= patch_field(&mut update_model.notes, meta.notes);

    if !has_updates {
        return Ok(());
    }

    set_image(db, registry_id, image_id, update_model).await
}

pub async fn prepare_image(
    db: &DbConn,
    plugin_manager: &PluginManager,
    registry_id: u32,
    image_id: u32,
) -> Result<(registry_entry::Model, image_entry::Model), CoreError> {
    log::info!("fetch_image called for image {image_id}");

    let registry = registry::get_registry(db, registry_id).await?;
    let mut image = get_image(db, registry_id, image_id).await?;
    let extract_req = ExtractImageRequest {
        registry: registry.clone().into(),
        image: image.clone().into(),
    };

    if let Some(field) = plugin_manager
        .execute(&registry.source_id, |plugin| {
            plugin.is_image_missing_data(extract_req.clone())
        })
        .await?
    {
        log::info!("Image {image_id} is missing data ({field}), extracting...");
        let res = plugin_manager
            .execute(&registry.source_id, |plugin| {
                plugin.extract_image(extract_req)
            })
            .await?;

        let mut image_model: image_entry::ActiveModel = image.into();
        let mut has_updates = false;

        // TODO: Adjust how extract should work
        has_updates |= patch_field(&mut image_model.width, res.image.width.map(Some));
        has_updates |= patch_field(&mut image_model.height, res.image.height.map(Some));
        has_updates |= patch_field(&mut image_model.tile_size, res.image.tile_size.map(Some));
        has_updates |= patch_field(&mut image_model.ark_url, res.image.ark_url.map(Some));

        if has_updates {
            set_image(db, registry_id, image_id, image_model).await?;
        } else {
            log::warn!("Didn't find new data for image {image_id}, but image is said to be missing data. Trying to proceed anyway.");
        }
        image = get_image(db, registry_id, image_id).await?; // Refetch the image with updated data
    }

    Ok((registry, image))
}

pub async fn fetch_image_tile(
    _db: &DbConn,
    plugin_manager: &PluginManager,
    registry: &registry_entry::Model,
    image: &image_entry::Model,
    level: u32,
    x: u32,
    y: u32,
) -> Result<TileResponse, CoreError> {
    log::info!(
        "fetch_image_tile called for image id={}, level={}, x={}, y={}",
        image.id,
        level,
        x,
        y
    );

    let req = TileRequest {
        image: image.clone().into(),
        zoom: level,
        x,
        y,
    };

    let image_data = plugin_manager
        .execute(&registry.source_id, |plugin| plugin.fetch_tile(req))
        .await?;

    Ok(image_data)
}

pub async fn fetch_image<F>(
    db: &DbConn,
    plugin_manager: &PluginManager,
    registry: registry_entry::Model,
    image: image_entry::Model,
    mut progress_callback: F,
    zoom_level: Option<u32>,
) -> Result<TileResponse, CoreError>
where
    F: FnMut(u32, u32) + Send,
{
    let width = image
        .width
        .ok_or_else(|| CoreError::Other("Missing width".into()))?;
    let height = image
        .height
        .ok_or_else(|| CoreError::Other("Missing height".into()))?;
    let tile_size = image.tile_size.unwrap_or(256);

    let geometry = ImageGeometry::new(width, height, tile_size);
    let base_layer = geometry.level(zoom_level.unwrap_or_else(|| geometry.max_level()));

    log::info!("Stitching image {} from tiles at {}", image.id, base_layer);

    let mut canvas = RgbImage::new(base_layer.width, base_layer.height);

    let mut coords = Vec::new();
    for y in 0..base_layer.tiles_y {
        for x in 0..base_layer.tiles_x {
            coords.push((x, y));
        }
    }

    let total_tiles = u32::try_from(coords.len())?;
    let mut completed_tiles = 0;

    let registry_ref = &registry;
    let image_ref = &image;

    // Fetch tiles concurrently
    let max_concurrent_requests = 5;
    let mut stream = futures::stream::iter(coords)
        .map(|(x, y)| async move {
            let tile = fetch_image_tile(
                db,
                plugin_manager,
                registry_ref,
                image_ref,
                base_layer.level,
                x,
                y,
            )
            .await?;
            Ok::<((u32, u32), TileResponse), CoreError>(((x, y), tile))
        })
        .buffer_unordered(max_concurrent_requests);

    // Process responses as they complete
    while let Some(result) = stream.next().await {
        let ((x, y), tile) = result?;

        let tile_img = image::load_from_memory(&tile.data)
            .map_err(|e| CoreError::Other(format!("Failed to decode tile {x},{y}: {e}")))?;

        let tile_rgb = tile_img.to_rgb8();

        let pos_x = x * tile_size;
        let pos_y = y * tile_size;

        canvas
            .copy_from(&tile_rgb, pos_x, pos_y)
            .map_err(|e| CoreError::Other(format!("Failed to stitch tile {x},{y}: {e}")))?;

        completed_tiles += 1;
        progress_callback(completed_tiles, total_tiles);
    }

    let mut buffer = Cursor::new(Vec::new());
    DynamicImage::ImageRgb8(canvas)
        .write_to(&mut buffer, image::ImageFormat::Jpeg)
        .map_err(|e| CoreError::Other(format!("Failed to encode final image: {e}")))?;

    Ok(TileResponse {
        data: buffer.into_inner(),
        mime_type: "image/jpeg".to_string(),
    })
}

pub async fn download_image<F>(
    db: &DbConn,
    plugin_manager: &PluginManager,
    registry: registry_entry::Model,
    image: image_entry::Model,
    mut progress_callback: F,
) -> Result<TileResponse, CoreError>
where
    F: FnMut(u32, u32) + Send,
{
    log::info!("download_image called for image id={}", image.id);

    let req = DownloadRequest {
        image: image.clone().into(),
    };

    let image_data = plugin_manager
        .execute(&registry.source_id, |plugin| plugin.download_image(req))
        .await?;

    if let Some(res) = image_data {
        progress_callback(1, 1);
        Ok(res)
    } else {
        fetch_image(db, plugin_manager, registry, image, progress_callback, None).await
    }
}
