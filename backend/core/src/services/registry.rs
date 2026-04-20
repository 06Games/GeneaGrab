use std::cmp::Reverse;

use crate::{
    comm_models::{CursorPayload, CursorResponse, RegistryFilters, RegistryMeta},
    db_entries::{
        image_entry,
        registry_entry::{self},
    },
    errors::CoreError,
    plugins::PluginManager,
};
use geneagrab_plugin_core::com_structs::{HostPluginBase, IdentifyRequest, IdentifyResponse};
use geneagrab_plugin_core::{com_structs::ExtractRequest, data::PluginMetadata};
use sea_orm::QueryOrder;
use sea_orm::{
    ActiveModelTrait, ActiveValue::NotSet, ColumnTrait, DbConn, EntityTrait, FromQueryResult,
    QueryFilter, QuerySelect, TransactionTrait,
};

impl RegistryMeta {
    #[must_use]
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

            total_images,
            acts_count,
        }
    }
}

#[derive(FromQueryResult)]
struct RegistryWithCounts {
    #[sea_orm(nested)]
    pub registry: registry_entry::Model,
    pub image_count: i64,
    pub event_count: i64,
}

fn add_count_subqueries(
    query: sea_orm::Select<registry_entry::Entity>,
) -> sea_orm::Select<registry_entry::Entity> {
    query
        .column_as(
            sea_orm::sea_query::Expr::cust("(SELECT COUNT(*) FROM image_entry WHERE image_entry.registry_entry_id = registry_entry.id)"),
            "image_count"
        )
        /* */.column_as(
            //sea_orm::sea_query::Expr::cust("(SELECT COUNT(*) FROM event WHERE event.registry_id = registry_entry.id)"),
            sea_orm::sea_query::Expr::cust("(SELECT 0)"), // TODO: Placeholder until events are implemented
            "event_count"
        )
}

pub async fn get_all_registries(
    db: &DbConn,
    payload: CursorPayload<RegistryFilters>,
) -> Result<CursorResponse<RegistryMeta>, CoreError> {
    let mut query = registry_entry::Entity::find();

    // Dynamically apply filters if they exist
    if let Some(filters) = payload.filters {
        if let Some(term) = filters.search_term.filter(|s| !s.trim().is_empty()) {
            let term = format!("%{term}%");
            query = query.filter(
                sea_orm::Condition::any()
                    .add(registry_entry::Column::ArchiveReference.like(&term))
                    .add(registry_entry::Column::Title.like(&term))
                    .add(registry_entry::Column::Author.like(&term))
                    .add(registry_entry::Column::ArkUrl.like(&term))
                    .add(registry_entry::Column::ManifestUrl.like(&term)),
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

    let query = add_count_subqueries(query);

    let offset = payload.cursor.unwrap_or(0);
    let limit = payload.limit;

    let mut data = query
        .order_by_asc(registry_entry::Column::Places)
        .order_by_asc(registry_entry::Column::RegistryTypes)
        .order_by_asc(registry_entry::Column::DateFrom)
        .order_by_asc(registry_entry::Column::Id)
        .offset(u64::from(offset))
        .limit(limit + 1) // Lookahead +1 to determine if there's a next page
        .into_model::<RegistryWithCounts>()
        .all(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {e}")))?;

    let next_cursor = if data.len() > usize::try_from(limit).unwrap_or(usize::MAX) {
        data.pop();
        Some(offset.saturating_add(u32::try_from(limit).unwrap_or(0)))
    } else {
        None
    };

    let metas = data
        .into_iter()
        .map(|row| {
            RegistryMeta::from_entry(
                row.registry,
                u32::try_from(row.image_count).unwrap_or(0),
                u32::try_from(row.event_count).unwrap_or(0),
            )
        })
        .collect();

    Ok(CursorResponse {
        data: metas,
        next_cursor,
    })
}

pub(crate) async fn get_registry(db: &DbConn, id: u32) -> Result<registry_entry::Model, CoreError> {
    registry_entry::Entity::find_by_id(id)
        .one(db)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error: {e}")))?
        .ok_or_else(|| CoreError::NotFound(format!("Registry {id} not found")))
}

pub async fn get_registry_meta(db: &DbConn, id: u32) -> Result<RegistryMeta, CoreError> {
    log::info!("fetch_registry_meta called with id: {id}");

    let row = add_count_subqueries(registry_entry::Entity::find_by_id(id))
        .into_model::<RegistryWithCounts>()
        .one(db)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error: {e}")))?
        .ok_or_else(|| CoreError::NotFound(format!("Registry {id} not found")))?;

    Ok(RegistryMeta::from_entry(
        row.registry,
        u32::try_from(row.image_count).unwrap_or(0),
        u32::try_from(row.event_count).unwrap_or(0),
    ))
}

pub async fn add_registry(
    db: &DbConn,
    plugin_manager: &PluginManager,
    url: String,
    plugin_id: String,
) -> Result<RegistryMeta, CoreError> {
    log::info!("add_registry called with url: {url}, plugin_id: {plugin_id}");

    let res = plugin_manager
        .execute(&plugin_id, |plugin| {
            let identified = plugin.identify(IdentifyRequest { url: url.clone() })?; // TODO: Avoid re-extracting data from the URL
            plugin.extract_registry(ExtractRequest { url, identified })
        })
        .await?;

    let txn = db
        .begin()
        .await
        .map_err(|e| CoreError::DbError(format!("Failed to start transaction: {e}")))?;

    let mut active_registry: registry_entry::ActiveModel =
        registry_entry::Model::from_registry(res.registry, 0).into();
    active_registry.id = NotSet;
    let new_registry: registry_entry::Model = active_registry
        .insert(&txn)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error inserting registry: {e}")))?;

    let num_images = u32::try_from(res.images.len())
        .map_err(|e| CoreError::InvalidInput(format!("Invalid number of images: {e}")))?;
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
            .map_err(|e| CoreError::DbError(format!("DB error inserting images: {e}")))?;
    }

    txn.commit()
        .await
        .map_err(|e| CoreError::DbError(format!("Failed to commit transaction: {e}")))?;

    let meta = RegistryMeta::from_entry(new_registry, num_images, 0);

    Ok(meta)
}

pub async fn get_plugins_for_url(
    plugin_manager: &PluginManager,
    url: &str,
) -> Result<Vec<(PluginMetadata, IdentifyResponse)>, CoreError> {
    let plugins = plugin_manager.list_plugins()?;
    let mut compatible_plugins = Vec::new();

    for meta in plugins {
        if let Ok(identified) = plugin_manager
            .execute(&meta.id, |plugin| {
                plugin.identify(IdentifyRequest {
                    url: url.to_string(),
                })
            })
            .await
        {
            compatible_plugins.push((meta, identified));
        }
    }

    // Prioritize plugins that explicitly list the website as compatible
    compatible_plugins.sort_by_key(|(meta, _)| {
        Reverse(
            meta.suggested_websites
                .iter()
                .any(|site| url.starts_with(site.as_ref())),
        )
    });

    Ok(compatible_plugins)
}
