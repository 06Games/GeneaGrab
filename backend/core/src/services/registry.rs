use crate::{
    comm_models::{CursorPayload, CursorResponse, RegistryFilters, RegistryMeta},
    db_entries::{
        image_entry,
        registry_entry::{self},
    },
    errors::CoreError,
    plugins::PluginManager,
};
use geneagrab_plugin_core::com_structs::ExtractRequest;
use geneagrab_plugin_core::com_structs::HostPluginBase;
use sea_orm::{
    ActiveModelTrait, ActiveValue::NotSet, ColumnTrait, DbConn, EntityTrait, QueryFilter,
    TransactionTrait,
};

impl RegistryMeta {
    pub fn from_entry(registry: registry_entry::Model, total_images: u32, acts_count: u32) -> Self {
        Self {
            id: registry.id,
            archive_reference: registry.archive_reference,
            source_types: registry.registry_types.0,
            places: registry.places.0,
            collection: registry.collection.0,
            ark_url: registry.ark_url,

            title: registry.title,
            subtitle: registry.subtitle,
            author: registry.author,
            date_from: registry.date_from,
            date_to: registry.date_to,
            notes: registry.notes,

            total_images: total_images,
            acts_count: acts_count,
        }
    }
}

pub async fn get_all_registries(
    db: &DbConn,
    payload: CursorPayload<RegistryFilters>,
) -> Result<CursorResponse<RegistryMeta>, CoreError> {
    let mut query = registry_entry::Entity::find();

    // Dynamically apply filters if they exist
    if let Some(filters) = payload.filters {
        if let Some(term) = filters.search_term.filter(|s| !s.trim().is_empty()) {
            let term = format!("%{}%", term);
            query = query.filter(
                sea_orm::Condition::any()
                    .add(registry_entry::Column::ArchiveReference.like(&term))
                    .add(registry_entry::Column::Title.like(&term))
                    .add(registry_entry::Column::Author.like(&term)),
            );
        }

        // Use custom expressions to query the raw serialized JSON arrays gracefully
        if let Some(t) = filters.source_type.filter(|s| !s.trim().is_empty()) {
            query = query.filter(sea_orm::sea_query::Expr::cust_with_values(
                "registry_types LIKE ?",
                vec![format!("%\"{}\"%", t)],
            ));
        }
        if let Some(p) = filters.place.filter(|s| !s.trim().is_empty()) {
            query = query.filter(sea_orm::sea_query::Expr::cust_with_values(
                "places LIKE ?",
                vec![format!("%\"{}\"%", p)],
            ));
        }
        if let Some(c) = filters.collection.filter(|s| !s.trim().is_empty()) {
            query = query.filter(sea_orm::sea_query::Expr::cust_with_values(
                "collection LIKE ?",
                vec![format!("%\"{}\"%", c)],
            ));
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

    // TODO: Fetch actual image and act counts
    let metas = data
        .into_iter()
        .map(|reg| RegistryMeta::from_entry(reg, 0, 0))
        .collect();

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
        .map_err(|e| CoreError::DbError(format!("DB error: {}", e)))?
        .ok_or_else(|| CoreError::NotFound(format!("Registry {} not found", id)))?;

    Ok(RegistryMeta::from_entry(registry, 0, 0)) // TODO: Fetch actual image and act counts
}

pub async fn add_registry(
    db: &DbConn,
    plugin_manager: &PluginManager,
    url: String,
    plugin_id: String,
) -> Result<RegistryMeta, CoreError> {
    log::info!(
        "add_registry called with url: {}, plugin_id: {}",
        url,
        plugin_id
    );

    let res = plugin_manager.execute(&plugin_id, |plugin| {
        plugin.extract_registry(ExtractRequest { url })
    })?;

    let txn = db
        .begin()
        .await
        .map_err(|e| CoreError::DbError(format!("Failed to start transaction: {}", e)))?;

    let mut active_registry: registry_entry::ActiveModel =
        registry_entry::Model::from_registry(res.registry, 0).into();
    active_registry.id = NotSet;
    let new_registry: registry_entry::Model = active_registry
        .insert(&txn)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error inserting registry: {}", e)))?;

    let num_images = res.images.len() as u32;
    if !res.images.is_empty() {
        let image_active_models: Vec<image_entry::ActiveModel> = res
            .images
            .into_iter()
            .map(|img| {
                let mut active_image: image_entry::ActiveModel =
                    image_entry::Model::from_image(img, 0, new_registry.id).into();
                active_image.id = NotSet;
                active_image
            })
            .collect();

        image_entry::Entity::insert_many(image_active_models)
            .exec(&txn)
            .await
            .map_err(|e| CoreError::DbError(format!("DB error inserting images: {}", e)))?;
    }

    txn.commit()
        .await
        .map_err(|e| CoreError::DbError(format!("Failed to commit transaction: {}", e)))?;

    let meta = RegistryMeta::from_entry(new_registry, num_images, 0);

    Ok(meta)
}
