use crate::{
    comm_models::{ImageMeta, UserImageMeta},
    db_entries::image_entry,
    errors::CoreError,
    plugins::PluginManager,
    services::registry,
};
use geneagrab_plugin_core::com_structs::{ExtractImageRequest, HostPluginBase};
use sea_orm::{
    sea_query::Nullable,
    ActiveValue::{self, Set},
    ColumnTrait, DbConn, EntityTrait, QueryFilter, Value,
};
use std::collections::HashMap;

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
        .filter(image_entry::Column::RegistryEntryId.eq(registry_id.clone()))
        .filter(image_entry::Column::ImageNumber.eq(image_id))
        .one(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {}", e)))?
        .ok_or_else(|| CoreError::NotFound(format!("Image {} not found", image_id)))
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
        .map_err(|e| CoreError::Other(format!("DB error: {}", e)))?;

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
    log::info!(
        "fetch_image_meta called for registry {} image {}",
        registry_id,
        image_id
    );

    let image = get_image(db, registry_id, image_id).await?;

    // TODO: Map actual act_types if you add them to your image_entry schema
    let act_types = HashMap::new();

    Ok(ImageMeta {
        name: image.name,
        date_range: image.date_range,
        notes: image.notes,
        image_number: image.image_number,
        act_types,
    })
}

pub async fn save_image_meta(
    db: &DbConn,
    registry_id: u32,
    image_id: u32,
    meta: UserImageMeta,
) -> Result<(), CoreError> {
    log::info!(
        "save_image_meta called for registry {} image {}",
        registry_id,
        image_id
    );

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

pub async fn fetch_image(
    db: &DbConn,
    plugin_manager: &PluginManager,
    registry_id: u32,
    image_id: u32,
    _thumbnail: bool,
) -> Result<Vec<u8>, CoreError> {
    log::info!("fetch_image called for image {}", image_id);

    let registry = registry::get_registry(db, registry_id).await?;
    let image = get_image(db, registry_id, image_id).await?;
    let extract_req = ExtractImageRequest {
        registry: registry.clone().into(),
        image: image.clone().into(),
    };

    if let Some(field) = plugin_manager.execute(&registry.source_id, |plugin| {
        plugin.is_image_missing_data(extract_req.clone())
    })? {
        log::info!(
            "Image {} is missing data ({}), extracting...",
            image_id,
            field
        );
        let res = plugin_manager.execute(&registry.source_id, |plugin| {
            plugin.extract_image(extract_req)
        })?;

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
            log::warn!("Didn't find new data for image {}, but image is said to be missing data. Trying to proceed anyway.", image_id);
        }
    }

    let image_path = format!("/home/evan/.local/share/GeneaGrab/Registries/Geneanet/17522/p2.jpg");

    let image_data = std::fs::read(image_path)
        .map_err(|e| CoreError::Other(format!("Failed to read image: {}", e)))?;

    Ok(image_data)
}
