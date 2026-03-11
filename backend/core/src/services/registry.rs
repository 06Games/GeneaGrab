use std::collections::{HashMap, HashSet};

use crate::{
    comm_models::{CursorPayload, CursorResponse, RegistryFilters, RegistryMeta},
    db_entries::{
        image_entry,
        registry_entry::{self, ExtraData, RegistryTypes, StringCollection},
    },
    errors::CoreError,
};
use sea_orm::{ActiveModelTrait, DbConn, EntityTrait, QueryFilter, ColumnTrait, TransactionTrait};

impl RegistryMeta {
    pub fn from_entry(registry: registry_entry::Model, total_images: u32) -> Self {
        Self {
            registry_id: registry.id,
            archive_reference: registry.archive_reference,
            source_types: registry.registry_types.0,
            town: registry
                .places
                .0
                .first()
                .cloned()
                .unwrap_or_else(|| "Unknown".into()),
            repository_url: registry.ark_url.unwrap_or_default(),
            total_images: total_images,
        }
    }
}

fn stub(url: String) -> Result<(registry_entry::Model, Vec<image_entry::Model>), CoreError> {
    let registry = registry_entry::Model {
        id: 0,
        source_id: "example_source_id".into(),
        registry_id: "example_registry_id".into(),
        archive_reference: "example_archive_reference".into(),
        registry_types: RegistryTypes(HashSet::new()),
        collection: StringCollection(vec![]),
        manifest_url: None,
        ark_url: Some(url.clone()),
        title: Some("Example Title".into()),
        subtitle: Some("Example Subtitle".into()),
        author: Some("Example Author".into()),
        date_from: None,
        date_from_normalized: None,
        date_to: None,
        date_to_normalized: None,
        places: StringCollection(vec![]),
        notes: None,
        extra: ExtraData(HashMap::new()),
    };
    let images: Vec<image_entry::Model> = vec![];

    Ok((registry, images))
}

pub async fn get_all_registries(db: &DbConn, payload: CursorPayload<RegistryFilters>) -> Result<CursorResponse<RegistryMeta>, CoreError> {
    let mut query = registry_entry::Entity::find();

    // Dynamically apply filters if they exist
    if let Some(filters) = payload.filters {
        if let Some(term) = filters.search_term.filter(|s| !s.trim().is_empty()) {
            let term = format!("%{}%", term);
            query = query.filter(
                sea_orm::Condition::any()
                    .add(registry_entry::Column::ArchiveReference.like(&term))
                    .add(registry_entry::Column::Title.like(&term))
                    .add(registry_entry::Column::Author.like(&term))
            );
        }
        
        // Use custom expressions to query the raw serialized JSON arrays gracefully 
        if let Some(t) = filters.source_type.filter(|s| !s.trim().is_empty()) {
            query = query.filter(sea_orm::sea_query::Expr::cust_with_values("registry_types LIKE ?", vec![format!("%\"{}\"%", t)]));
        }
        if let Some(p) = filters.place.filter(|s| !s.trim().is_empty()) {
            query = query.filter(sea_orm::sea_query::Expr::cust_with_values("places LIKE ?", vec![format!("%\"{}\"%", p)]));
        }
        if let Some(c) = filters.collection.filter(|s| !s.trim().is_empty()) {
            query = query.filter(sea_orm::sea_query::Expr::cust_with_values("collection LIKE ?", vec![format!("%\"{}\"%", c)]));
        }
        
        if let Some(d) = filters.date_from {
            query = query.filter(registry_entry::Column::DateTo.gte(d)); 
        }
        if let Some(d) = filters.date_to {
            query = query.filter(registry_entry::Column::DateFrom.lte(d));
        }
    }

    let mut cursor_query = query.cursor_by(registry_entry::Column::Id);
    if let Some(last_id) = payload.cursor {
        cursor_query.after(last_id);
    }

    // Lookahead +1 to determine if there's a next page
    let limit = payload.limit;
    let mut data = cursor_query
        .first(limit + 1)
        .all(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {}", e)))?;

    let next_cursor = if data.len() > limit as usize {
        data.pop();
        data.last().map(|item| item.id)
    } else {
        None
    };

    let metas = data.into_iter().map(|reg| RegistryMeta::from_entry(reg, 0)).collect();

    Ok(CursorResponse {
        data: metas,
        next_cursor,
    })
}

pub async fn get_registry(db: &DbConn, id: u32) -> Result<RegistryMeta, CoreError> {
    log::info!("fetch_registry_meta called with id: {}", id);

    let registry = registry_entry::Entity::find_by_id(id.clone())
        .one(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {}", e)))?
        .ok_or_else(|| CoreError::NotFound(format!("Registry {} not found", id)))?;

    Ok(RegistryMeta {
        registry_id: registry.id,
        archive_reference: registry.archive_reference,
        source_types: registry.registry_types.0,
        town: registry
            .places
            .0
            .first()
            .cloned()
            .unwrap_or_else(|| "Unknown".into()),
        repository_url: registry.ark_url.unwrap_or_default(),
        total_images: 0, // TODO: Run a COUNT() query on the image_entry table if needed
    })
}

pub async fn add_registry(db: &DbConn, url: String) -> Result<RegistryMeta, CoreError> {
    log::info!("add_registry called with url: {}", url);

    let result: Result<(registry_entry::Model, Vec<image_entry::Model>), CoreError> = stub(url);
    let (registry, images) = result?;

    let txn = db
        .begin()
        .await
        .map_err(|e| CoreError::Other(format!("Failed to start transaction: {}", e)))?;

    let active_registry: registry_entry::ActiveModel = registry.into();
    let new_registry: registry_entry::Model = active_registry
        .insert(&txn)
        .await
        .map_err(|e| CoreError::Other(format!("DB error inserting registry: {}", e)))?;

    let num_images = images.len() as u32;
    if !images.is_empty() {
        let image_active_models: Vec<image_entry::ActiveModel> = images
            .into_iter()
            .map(|mut img| {
                img.registry_entry_id = new_registry.id;
                img.into()
            })
            .collect();

        image_entry::Entity::insert_many(image_active_models)
            .exec(&txn)
            .await
            .map_err(|e| CoreError::Other(format!("DB error inserting images: {}", e)))?;
    }

    txn.commit()
        .await
        .map_err(|e| CoreError::Other(format!("Failed to commit transaction: {}", e)))?;

    let meta = RegistryMeta::from_entry(new_registry, num_images);

    Ok(meta)
}
