use sea_orm::{DbConn, EntityTrait};
use crate::{
    comm_models::RegistryMeta,
    db_entries::registry_entry,
    errors::CoreError,
};

pub async fn fetch_registry_meta(db: &DbConn, id: u32) -> Result<RegistryMeta, CoreError> {
    log::info!("fetch_registry_meta called with id: {}", id);

    let registry = registry_entry::Entity::find_by_id(id.clone())
        .one(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {}", e)))?
        .ok_or_else(|| CoreError::NotFound(format!("Registry {} not found", id)))?;

    // Assuming we map the HashSet from the DB model to the Comm model
    Ok(RegistryMeta {
        registry_id: registry.id,
        archive_reference: registry.archive_reference,
        source_types: registry.registry_types.0, 
        // Note: 'town', 'repository_url', and 'total_images' weren't explicitly in the 
        // RegistryEntry model we built earlier. You might need to extract them from 
        // the `extra` HashMap, the `places` collection, or add them to the DB schema.
        town: registry.places.0.first().cloned().unwrap_or_else(|| "Unknown".into()),
        repository_url: registry.ark_url.unwrap_or_default(),
        total_images: 0, // TODO: Run a COUNT() query on the image_entry table if needed
    })
}
