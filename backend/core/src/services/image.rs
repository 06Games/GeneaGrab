use crate::{
    comm_models::{ImageMeta, UserImageMeta},
    db_entries::image_entry,
    errors::CoreError,
};
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

    let image = image_entry::Entity::find()
        .filter(image_entry::Column::RegistryEntryId.eq(registry_id.clone()))
        .filter(image_entry::Column::ImageNumber.eq(image_id))
        .one(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {}", e)))?
        .ok_or_else(|| CoreError::NotFound(format!("Image {} not found", image_id)))?;

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

    let update_result = image_entry::Entity::update_many()
        .filter(image_entry::Column::RegistryEntryId.eq(registry_id))
        .filter(image_entry::Column::ImageNumber.eq(image_id))
        .set(update_model)
        .exec(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {}", e)))?;

    // Check if any rows were actually updated
    if update_result.rows_affected == 0 {
        return Err(CoreError::NotFound("Image not found to update".into()));
    }

    Ok(())
}

pub async fn fetch_image(
    _db: &DbConn,
    _registry_id: u32,
    image_id: u32,
    _thumbnail: bool,
) -> Result<Vec<u8>, CoreError> {
    log::info!("fetch_image called for image {}", image_id);

    // Assuming images remain stored on the filesystem, not as BLOBs in the DB.
    // If you plan to store file paths in the DB, you would query `_db` here first to get `image_path`.
    let image_path = format!("/home/evan/.local/share/GeneaGrab/Registries/Geneanet/17522/p2.jpg");

    let image_data = std::fs::read(image_path)
        .map_err(|e| CoreError::Other(format!("Failed to read image: {}", e)))?;

    Ok(image_data)
}
