use std::cmp::Reverse;
use std::str::FromStr;

use crate::{
    comm_models::{
        AvailableOption, CursorPayload, CursorResponse,
        RegistryFilters, RegistryMeta, UserImageMeta, UserRegistryMeta,
    },
    db_entries::{
        image_entry,
        registry_entry::{self},
    },
    errors::CoreError,
};
use dates::HistoricalDate;
use geneagrab_providers::com_structs::{IdentifyRequest, IdentifyResponse, ExtractRequest};
use geneagrab_providers::data::ProviderMetadata;
use sea_orm::QueryOrder;
use sea_orm::{
    ActiveModelTrait, ActiveValue::{NotSet, Set}, ColumnTrait, DbConn, EntityTrait, FromQueryResult,
    QueryFilter, QuerySelect, TransactionTrait,
};

#[derive(FromQueryResult)]
struct RegistryPlaceSummary {
    #[allow(dead_code)]
    id: u32,
    places: crate::db_entries::utils::JsonField<std::collections::HashSet<Vec<String>>>,
}

#[derive(FromQueryResult)]
struct RegistryCollectionSummary {
    #[allow(dead_code)]
    id: u32,
    collection: crate::db_entries::utils::JsonField<Vec<String>>,
}

#[derive(FromQueryResult)]
struct RegistryTypeSummary {
    #[allow(dead_code)]
    id: u32,
    registry_types: crate::db_entries::utils::JsonField<std::collections::HashSet<geneagrab_providers::data::RegistryType>>,
}

impl RegistryMeta {
    #[must_use]
    pub fn from_entry(
        registry: registry_entry::Model,
        image_count: usize,
        images: Option<Vec<UserImageMeta>>,
        acts_count: u32,
    ) -> Self {
        Self {
            id: registry.id,
            source_id: registry.source_id,
            registry_id: registry.registry_id,
            archive_reference: registry.archive_reference,
            source_types: registry.registry_types.0,
            places: registry.places.0,
            collection: registry.collection.0,
            ark_url: registry.ark_url,

            title: registry.title,
            subtitle: registry.subtitle,
            author: registry.author,
            date_from: registry.date_from.as_deref().map(ToString::to_string),
            date_from_gregorian: registry
                .date_from
                .as_deref()
                .filter(|d| !matches!(d, HistoricalDate::Gregorian { .. }))
                .and_then(dates::HistoricalDate::to_modern_date)
                .as_ref()
                .map(ToString::to_string),
            date_to: registry.date_to.as_deref().map(ToString::to_string),
            date_to_gregorian: registry
                .date_to
                .as_deref()
                .filter(|d| !matches!(d, HistoricalDate::Gregorian { .. }))
                .and_then(dates::HistoricalDate::to_modern_date)
                .as_ref()
                .map(ToString::to_string),
            notes: registry.notes,

            total_images: image_count,
            images,
            acts_count,
        }
    }
}

impl From<PartialImageMeta> for UserImageMeta {
    fn from(value: PartialImageMeta) -> Self {
        Self {
            name: Some(value.name),
            date_range: Some(value.date_range),
            notes: Some(value.notes),
        }
    }
}

#[derive(FromQueryResult)]
pub struct PartialImageMeta {
    pub name: Option<String>,
    pub date_range: Option<String>,
    pub notes: Option<String>,
}

#[derive(FromQueryResult)]
struct RegistryWithJoins {
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
        ).column_as(
        //sea_orm::sea_query::Expr::cust("(SELECT COUNT(*) FROM event WHERE event.registry_id = registry_entry.id)"),
        sea_orm::sea_query::Expr::cust("(SELECT 0)"), // TODO: Placeholder until events are implemented
        "event_count")
}

pub fn normalize_place_hierarchy(raw_parts: &[String]) -> Vec<String> {
    let mut parts = Vec::new();
    for part in raw_parts {
        if part.contains(',') {
            for sub in part.split(',') {
                let trimmed = sub.trim();
                if !trimmed.is_empty() {
                    parts.push(trimmed.to_string());
                }
            }
        } else {
            let trimmed = part.trim();
            if !trimmed.is_empty() {
                parts.push(trimmed.to_string());
            }
        }
    }
    parts
}

pub fn place_matches_filter(normalized: &[String], filter: &str) -> bool {
    let filter = filter.trim();
    if filter.is_empty() {
        return true;
    }
    if filter == "__unknown__" {
        return false;
    }
    let filter_lower = filter.to_lowercase();
    let key = normalized
        .iter()
        .map(|s| s.to_lowercase())
        .collect::<Vec<_>>()
        .join(" > ");

    if key == filter_lower || key.contains(&filter_lower) {
        return true;
    }

    let filter_parts: Vec<&str> = if filter_lower.contains(" > ") {
        filter_lower.split(" > ").map(str::trim).filter(|s| !s.is_empty()).collect()
    } else if filter_lower.contains(',') {
        filter_lower.split(',').map(str::trim).filter(|s| !s.is_empty()).collect()
    } else {
        vec![filter_lower.as_str()]
    };

    filter_parts.iter().all(|fp| {
        normalized.iter().any(|part| part.to_lowercase().contains(fp))
    })
}

async fn apply_registry_filters(
    mut query: sea_orm::Select<registry_entry::Entity>,
    filters: Option<&RegistryFilters>,
) -> Result<sea_orm::Select<registry_entry::Entity>, CoreError> {
    if let Some(filters) = filters {
        if let Some(term) = filters.search_term.as_deref().filter(|s| !s.trim().is_empty()) {
            let mut any_cond = sea_orm::Condition::any();

            if url::Url::parse(term).is_ok() {
                if let Ok(providers) = get_providers_for_url(term).await {
                    for (meta, identified) in providers {
                        any_cond = any_cond.add(
                            sea_orm::Condition::all()
                                .add(registry_entry::Column::SourceId.eq(meta.id.to_string()))
                                .add(registry_entry::Column::RegistryId.eq(identified.registry_id)),
                        );
                    }
                }
            }

            let term_like = format!("%{term}%");
            any_cond = any_cond
                .add(registry_entry::Column::ArchiveReference.like(&term_like))
                .add(registry_entry::Column::Title.like(&term_like))
                .add(registry_entry::Column::Author.like(&term_like))
                .add(registry_entry::Column::ArkUrl.like(&term_like))
                .add(registry_entry::Column::ManifestUrl.like(&term_like));

            query = query.filter(any_cond);
        }

        // Use custom expressions to query the raw serialized JSON arrays gracefully
        if let Some(t) = filters.source_type.as_deref().filter(|s| !s.trim().is_empty()) {
            query = query.filter(sea_orm::sea_query::Expr::cust_with_values(
                "registry_types LIKE ?",
                vec![format!("%\"{t}\"%")],
            ));
        }
        if let Some(p) = filters.place.as_deref().filter(|s| !s.trim().is_empty()) {
            let p_trimmed = p.trim();
            if p_trimmed == "__unknown__" {
                query = query.filter(sea_orm::sea_query::Expr::cust(
                    "places = '[]' OR places = '[\"\"]' OR places = '[[]]' OR places = ''",
                ));
            } else {
                let parts: Vec<&str> = if p_trimmed.contains(" > ") {
                    p_trimmed.split(" > ").map(str::trim).filter(|s| !s.is_empty()).collect()
                } else if p_trimmed.contains(',') {
                    p_trimmed.split(',').map(str::trim).filter(|s| !s.is_empty()).collect()
                } else {
                    vec![p_trimmed]
                };

                for part in parts {
                    query = query.filter(sea_orm::sea_query::Expr::cust_with_values(
                        "places LIKE ?",
                        vec![format!("%{part}%")],
                    ));
                }
            }
        }
        if let Some(c) = filters.collection.as_deref().filter(|s| !s.trim().is_empty()) {
            query = query.filter(sea_orm::sea_query::Expr::cust_with_values(
                "collection LIKE ?",
                vec![format!("%\"{c}\"%")],
            ));
        }

        if let Some(s) = filters.date_from.as_deref().filter(|s| !s.trim().is_empty()) {
            if let Ok(d) = dates::HistoricalDate::from_str(s) {
                query = query.filter(registry_entry::Column::DateToNormalized.gte(d.to_jdn()));
            }
        }
        if let Some(s) = filters.date_to.as_deref().filter(|s| !s.trim().is_empty()) {
            if let Ok(d) = dates::HistoricalDate::from_str(s) {
                query = query.filter(registry_entry::Column::DateFromNormalized.lte(d.to_jdn()));
            }
        }

        let is_unknown = filters.is_unknown_location == Some(true)
            || filters.location.as_ref().map_or(false, |l| l.is_empty());
        if is_unknown {
            query = query.filter(sea_orm::sea_query::Expr::cust(
                "places = '[]' OR places = '[\"\"]' OR places = '[[]]' OR places = ''",
            ));
        } else if let Some(loc) = &filters.location {
            for part in loc {
                query = query.filter(sea_orm::sea_query::Expr::cust_with_values(
                    "places LIKE ?",
                    vec![format!("%\"{part}\"%")],
                ));
            }
        }
    }
    Ok(query)
}

pub async fn get_registries(
    db: &DbConn,
    payload: CursorPayload<RegistryFilters>,
) -> Result<CursorResponse<RegistryMeta>, CoreError> {
    let query = registry_entry::Entity::find();
    let query = apply_registry_filters(query, payload.filters.as_ref()).await?;
    let query = add_count_subqueries(query);

    let offset = payload.cursor.unwrap_or(0);
    let limit = payload.limit;

    let mut data = query
        .order_by_asc(registry_entry::Column::DateFrom)
        .order_by_asc(registry_entry::Column::ArchiveReference)
        .order_by_asc(registry_entry::Column::Title)
        .order_by_asc(registry_entry::Column::Id)
        .offset(u64::from(offset))
        .limit(limit + 1) // Lookahead +1 to determine if there's a next page
        .into_model::<RegistryWithJoins>()
        .all(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {e}")))?;

    if let Some(filters) = &payload.filters {
        let is_unknown = filters.is_unknown_location == Some(true)
            || filters.location.as_ref().map_or(false, |l| l.is_empty());
        if is_unknown {
            data.retain(|row| {
                row.registry.places.0.is_empty()
                    || row.registry.places.0.iter().all(|p| normalize_place_hierarchy(p).is_empty())
            });
        } else if let Some(loc) = &filters.location {
            data.retain(|row| {
                row.registry.places.0.iter().any(|raw_p| {
                    normalize_place_hierarchy(raw_p) == *loc
                })
            });
        }

        if let Some(p) = filters.place.as_deref().filter(|s| !s.trim().is_empty()) {
            let p_trimmed = p.trim();
            if p_trimmed == "__unknown__" {
                data.retain(|row| {
                    row.registry.places.0.is_empty()
                        || row.registry.places.0.iter().all(|p| normalize_place_hierarchy(p).is_empty())
                });
            } else if filters.location.is_none() {
                data.retain(|row| {
                    row.registry.places.0.iter().any(|raw_p| {
                        let normalized = normalize_place_hierarchy(raw_p);
                        place_matches_filter(&normalized, p_trimmed)
                    })
                });
            }
        }
    }

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
                usize::try_from(row.image_count).unwrap_or(0),
                None,
                u32::try_from(row.event_count).unwrap_or(0),
            )
        })
        .collect();

    Ok(CursorResponse {
        data: metas,
        next_cursor,
    })
}

pub async fn get_available_places(
    db: &DbConn,
    filters: Option<RegistryFilters>,
) -> Result<Vec<AvailableOption>, CoreError> {
    let query = registry_entry::Entity::find();
    let query = apply_registry_filters(query, filters.as_ref()).await?;
    let rows = query
        .select_only()
        .column(registry_entry::Column::Id)
        .column(registry_entry::Column::Places)
        .into_model::<RegistryPlaceSummary>()
        .all(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {e}")))?;

    let filter_place = filters
        .as_ref()
        .and_then(|f| f.place.as_deref())
        .map(str::trim)
        .filter(|s| !s.is_empty());

    // Map: key -> (location, display_name, count)
    let mut group_map: std::collections::HashMap<String, (Vec<String>, String, u32)> =
        std::collections::HashMap::new();

    for row in rows {
        let mut unique_places: Vec<Vec<String>> = Vec::new();
        let mut seen_keys = std::collections::HashSet::new();

        for raw_place in &row.places.0 {
            let normalized = normalize_place_hierarchy(raw_place);
            if !normalized.is_empty() {
                if let Some(fp) = filter_place {
                    if !place_matches_filter(&normalized, fp) {
                        continue;
                    }
                }

                let key = normalized
                    .iter()
                    .map(|s| s.to_lowercase())
                    .collect::<Vec<_>>()
                    .join(" > ");
                if seen_keys.insert(key) {
                    unique_places.push(normalized);
                }
            }
        }

        if unique_places.is_empty() {
            let is_filter_unknown = filter_place == Some("__unknown__")
                || filters.as_ref().and_then(|f| f.is_unknown_location).unwrap_or(false);
            if filter_place.is_none() || is_filter_unknown {
                let entry = group_map
                    .entry("__unknown__".to_string())
                    .or_insert_with(|| (Vec::new(), String::new(), 0));
                entry.2 += 1;
            }
        } else {
            // Count registry for each location group
            for place in unique_places {
                let display_name = place.join(" > ");
                let key = place
                    .iter()
                    .map(|s| s.to_lowercase())
                    .collect::<Vec<_>>()
                    .join(" > ");
                let entry = group_map
                    .entry(key)
                    .or_insert_with(|| (place, display_name, 0));
                entry.2 += 1;
            }
        }
    }

    let mut groups: Vec<AvailableOption> = group_map
        .into_iter()
        .map(|(key, (location, display_name, count))| AvailableOption {
            key,
            label: display_name,
            count,
            parts: location,
        })
        .collect();

    // Sort by biggest entity first (e.g. France > Alpes-Maritimes > Lantosque > Loda)
    // with unknown/empty location at the end.
    groups.sort_by(|a, b| {
        let a_unknown = a.parts.is_empty() || a.key == "__unknown__";
        let b_unknown = b.parts.is_empty() || b.key == "__unknown__";
        if a_unknown && b_unknown {
            return std::cmp::Ordering::Equal;
        }
        if a_unknown {
            return std::cmp::Ordering::Greater;
        }
        if b_unknown {
            return std::cmp::Ordering::Less;
        }

        let len = a.parts.len().min(b.parts.len());
        for i in 0..len {
            let cmp = a.parts[i]
                .to_lowercase()
                .cmp(&b.parts[i].to_lowercase());
            if cmp != std::cmp::Ordering::Equal {
                return cmp;
            }
        }
        a.parts.len().cmp(&b.parts.len())
    });

    Ok(groups)
}

pub async fn get_available_collections(
    db: &DbConn,
    filters: Option<RegistryFilters>,
) -> Result<Vec<AvailableOption>, CoreError> {
    let query = registry_entry::Entity::find();
    let query = apply_registry_filters(query, filters.as_ref()).await?;
    let rows = query
        .select_only()
        .column(registry_entry::Column::Id)
        .column(registry_entry::Column::Collection)
        .into_model::<RegistryCollectionSummary>()
        .all(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {e}")))?;

    let mut counts: std::collections::BTreeMap<String, u32> = std::collections::BTreeMap::new();
    for row in rows {
        let mut seen = std::collections::HashSet::new();
        for col in row.collection.0 {
            let trimmed = col.trim();
            if !trimmed.is_empty() && seen.insert(trimmed.to_string()) {
                *counts.entry(trimmed.to_string()).or_default() += 1;
            }
        }
    }

    let options = counts
        .into_iter()
        .map(|(name, count)| AvailableOption {
            key: name.clone(),
            label: name,
            count,
            parts: Vec::new(),
        })
        .collect();

    Ok(options)
}

pub async fn get_available_types(
    db: &DbConn,
    filters: Option<RegistryFilters>,
) -> Result<Vec<AvailableOption>, CoreError> {
    let query = registry_entry::Entity::find();
    let query = apply_registry_filters(query, filters.as_ref()).await?;
    let rows = query
        .select_only()
        .column(registry_entry::Column::Id)
        .column(registry_entry::Column::RegistryTypes)
        .into_model::<RegistryTypeSummary>()
        .all(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {e}")))?;

    let mut counts: std::collections::BTreeMap<String, u32> = std::collections::BTreeMap::new();
    for row in rows {
        let mut seen = std::collections::HashSet::new();
        for t in row.registry_types.0 {
            let cat = match t {
                geneagrab_providers::data::RegistryType::Vital(_) => "vital",
                geneagrab_providers::data::RegistryType::Union(_) => "union",
                geneagrab_providers::data::RegistryType::Mortality(_) => "mortality",
                geneagrab_providers::data::RegistryType::Census(_) => "census",
                geneagrab_providers::data::RegistryType::Legal(_) => "legal",
                geneagrab_providers::data::RegistryType::Land(_) => "land",
                geneagrab_providers::data::RegistryType::Media(_) => "media",
                geneagrab_providers::data::RegistryType::Military(_) => "military",
                geneagrab_providers::data::RegistryType::Other(_) => "other",
                geneagrab_providers::data::RegistryType::Unknown => "unknown",
            };
            if seen.insert(cat) {
                *counts.entry(cat.to_string()).or_default() += 1;
            }
        }
    }

    let options = counts
        .into_iter()
        .map(|(cat, count)| AvailableOption {
            key: cat.clone(),
            label: cat,
            count,
            parts: Vec::new(),
        })
        .collect();

    Ok(options)
}

pub(crate) async fn get_registry(db: &DbConn, id: u32) -> Result<registry_entry::Model, CoreError> {
    registry_entry::Entity::find_by_id(id)
        .one(db)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error: {e}")))?
        .ok_or_else(|| CoreError::NotFound(format!("Registry {id} not found")))
}

pub async fn get_registry_meta(db: &DbConn, id: u32) -> Result<RegistryMeta, CoreError> {
    tracing::info!("fetch_registry_meta called with id: {id}");

    let row = add_count_subqueries(registry_entry::Entity::find_by_id(id))
        .into_model::<RegistryWithJoins>()
        .one(db)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error: {e}")))?
        .ok_or_else(|| CoreError::NotFound(format!("Registry {id} not found")))?;

    let images = image_entry::Entity::find()
        .filter(image_entry::Column::RegistryEntryId.eq(id))
        .order_by_asc(image_entry::Column::ImageNumber)
        .select_only()
        .column(image_entry::Column::Name)
        .column(image_entry::Column::DateRange)
        .into_model::<PartialImageMeta>()
        .all(db)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error: {e}")))?;

    Ok(RegistryMeta::from_entry(
        row.registry,
        usize::try_from(row.image_count).unwrap_or(0),
        Some(images.into_iter().map(std::convert::Into::into).collect()),
        u32::try_from(row.event_count).unwrap_or(0),
    ))
}

pub async fn add_registry(
    db: &DbConn,
    url: String,
    provider_id: String,
) -> Result<RegistryMeta, CoreError> {
    tracing::info!("add_registry called with url: {url}, provider_id: {provider_id}");

    let provider = crate::providers::get_provider(db, &provider_id).await?;
    let identified = provider.identify(IdentifyRequest { url: url.clone() }).map_err(|e| CoreError::ProviderError(e.to_string()))?;
    let res = provider
        .extract_registry(ExtractRequest { url, identified })
        .await
        .map_err(|e| CoreError::ProviderError(e.to_string()))?;

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

    let num_images = res.images.len();
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

    let meta = RegistryMeta::from_entry(new_registry, num_images, None, 0);

    Ok(meta)
}

pub async fn get_providers_for_url(
    url: &str,
) -> Result<Vec<(ProviderMetadata, IdentifyResponse)>, CoreError> {
    let fetcher = crate::providers::get_fetcher()?;
    let providers = crate::providers::get_providers("", fetcher);
    let mut compatible_providers = Vec::new();

    for provider in providers {
        let meta = provider.metadata();
        if let Ok(identified) = provider.identify(IdentifyRequest {
            url: url.to_string(),
        }) {
            compatible_providers.push((meta, identified));
        }
    }

    // Prioritize providers that explicitly list the website as compatible
    compatible_providers.sort_by_key(|(meta, _)| {
        Reverse(
            meta.suggested_websites
                .iter()
                .any(|site| url.starts_with(site.as_ref())),
        )
    });

    Ok(compatible_providers)
}

pub async fn delete_registry(db: &DbConn, id: u32) -> Result<(), CoreError> {
    registry_entry::Entity::delete_by_id(id)
        .exec(db)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error: {e}")))?;
    Ok(())
}

pub async fn save_registry_meta(
    db: &DbConn,
    id: u32,
    meta: UserRegistryMeta,
) -> Result<(), CoreError> {
    tracing::info!("save_registry_meta called for registry {id}");

    let mut model: registry_entry::ActiveModel = registry_entry::Entity::find_by_id(id)
        .one(db)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error: {e}")))?
        .ok_or_else(|| CoreError::NotFound(format!("Registry {id} not found")))?
        .into();

    if let Some(archive_ref) = meta.archive_reference {
        model.archive_reference = Set(archive_ref);
    }
    if let Some(title) = meta.title {
        model.title = Set(title);
    }
    if let Some(subtitle) = meta.subtitle {
        model.subtitle = Set(subtitle);
    }
    if let Some(author) = meta.author {
        model.author = Set(author);
    }
    if let Some(notes) = meta.notes {
        model.notes = Set(notes);
    }
    if let Some(ark_url) = meta.ark_url {
        model.ark_url = Set(ark_url);
    }
    if let Some(date_from_opt) = meta.date_from {
        match date_from_opt {
            Some(s) if !s.trim().is_empty() => {
                if let Ok(d) = HistoricalDate::from_str(&s) {
                    model.date_from_normalized = Set(Some(d.to_jdn()));
                    model.date_from = Set(Some(crate::db_entries::utils::JsonField(d)));
                } else {
                    model.date_from = Set(None);
                    model.date_from_normalized = Set(None);
                }
            }
            _ => {
                model.date_from = Set(None);
                model.date_from_normalized = Set(None);
            }
        }
    }
    if let Some(date_to_opt) = meta.date_to {
        match date_to_opt {
            Some(s) if !s.trim().is_empty() => {
                if let Ok(d) = HistoricalDate::from_str(&s) {
                    model.date_to_normalized = Set(Some(d.to_jdn()));
                    model.date_to = Set(Some(crate::db_entries::utils::JsonField(d)));
                } else {
                    model.date_to = Set(None);
                    model.date_to_normalized = Set(None);
                }
            }
            _ => {
                model.date_to = Set(None);
                model.date_to_normalized = Set(None);
            }
        }
    }
    if let Some(places) = meta.places {
        model.places = Set(crate::db_entries::utils::JsonField(places));
    }
    if let Some(collection) = meta.collection {
        model.collection = Set(crate::db_entries::utils::JsonField(collection));
    }
    if let Some(source_types) = meta.source_types {
        model.registry_types = Set(crate::db_entries::utils::JsonField(source_types));
    }

    model
        .update(db)
        .await
        .map_err(|e| CoreError::DbError(format!("DB error updating registry: {e}")))?;

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_normalize_place_hierarchy() {
        let raw = vec![
            "France".to_string(),
            "Alpes-Maritimes, Lantosque".to_string(),
            "Loda".to_string(),
        ];
        let normalized = normalize_place_hierarchy(&raw);
        assert_eq!(
            normalized,
            vec!["France", "Alpes-Maritimes", "Lantosque", "Loda"]
        );

        let empty = vec!["   ".to_string(), "".to_string()];
        assert!(normalize_place_hierarchy(&empty).is_empty());
    }

    #[test]
    fn test_location_groups_sorting_biggest_entity_first() {
        let mut groups = vec![
            AvailableOption {
                key: "__unknown__".to_string(),
                label: String::new(),
                count: 1,
                parts: vec![],
            },
            AvailableOption {
                key: "france > var > brignoles".to_string(),
                label: "France > Var > Brignoles".to_string(),
                count: 1,
                parts: vec!["France".to_string(), "Var".to_string(), "Brignoles".to_string()],
            },
            AvailableOption {
                key: "france > alpes-maritimes > nice".to_string(),
                label: "France > Alpes-Maritimes > Nice".to_string(),
                count: 1,
                parts: vec!["France".to_string(), "Alpes-Maritimes".to_string(), "Nice".to_string()],
            },
            AvailableOption {
                key: "france > alpes-maritimes > lantosque > loda".to_string(),
                label: "France > Alpes-Maritimes > Lantosque > Loda".to_string(),
                count: 1,
                parts: vec![
                    "France".to_string(),
                    "Alpes-Maritimes".to_string(),
                    "Lantosque".to_string(),
                    "Loda".to_string(),
                ],
            },
            AvailableOption {
                key: "italie > piémont > turin".to_string(),
                label: "Italie > Piémont > Turin".to_string(),
                count: 1,
                parts: vec!["Italie".to_string(), "Piémont".to_string(), "Turin".to_string()],
            },
            AvailableOption {
                key: "france > alpes-maritimes > lantosque".to_string(),
                label: "France > Alpes-Maritimes > Lantosque".to_string(),
                count: 1,
                parts: vec![
                    "France".to_string(),
                    "Alpes-Maritimes".to_string(),
                    "Lantosque".to_string(),
                ],
            },
        ];

        groups.sort_by(|a, b| {
            let a_unknown = a.parts.is_empty() || a.key == "__unknown__";
            let b_unknown = b.parts.is_empty() || b.key == "__unknown__";
            if a_unknown && b_unknown {
                return std::cmp::Ordering::Equal;
            }
            if a_unknown {
                return std::cmp::Ordering::Greater;
            }
            if b_unknown {
                return std::cmp::Ordering::Less;
            }

            let len = a.parts.len().min(b.parts.len());
            for i in 0..len {
                let cmp = a.parts[i]
                    .to_lowercase()
                    .cmp(&b.parts[i].to_lowercase());
                if cmp != std::cmp::Ordering::Equal {
                    return cmp;
                }
            }
            a.parts.len().cmp(&b.parts.len())
        });

        let keys: Vec<&str> = groups.iter().map(|g| g.key.as_str()).collect();
        assert_eq!(
            keys,
            vec![
                "france > alpes-maritimes > lantosque",
                "france > alpes-maritimes > lantosque > loda",
                "france > alpes-maritimes > nice",
                "france > var > brignoles",
                "italie > piémont > turin",
                "__unknown__",
            ]
        );
    }

    #[test]
    fn test_place_matches_filter() {
        let place = vec![
            "France".to_string(),
            "Alpes-Maritimes".to_string(),
            "Lantosque".to_string(),
            "Loda".to_string(),
        ];

        // Exact match with different cases
        assert!(place_matches_filter(&place, "France > Alpes-Maritimes > Lantosque > Loda"));
        assert!(place_matches_filter(&place, "france > alpes-maritimes > lantosque > loda"));

        // Single part match
        assert!(place_matches_filter(&place, "Brignoles") == false);
        assert!(place_matches_filter(&place, "Lantosque"));
        assert!(place_matches_filter(&place, "France"));

        // Sub-hierarchy match
        assert!(place_matches_filter(&place, "Alpes-Maritimes > Lantosque"));
        assert!(place_matches_filter(&place, "Lantosque > Loda"));

        // Comma separated multi-part match
        assert!(place_matches_filter(&place, "France, Loda"));
        assert!(place_matches_filter(&place, "France, Var") == false);

        // Unknown and empty filters
        assert!(place_matches_filter(&place, "__unknown__") == false);
        assert!(place_matches_filter(&place, ""));
    }
}


