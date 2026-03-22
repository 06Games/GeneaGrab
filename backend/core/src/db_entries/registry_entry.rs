use geneagrab_plugin_core::data::Registry;
use sea_orm::entity::prelude::*;
use serde::{Deserialize, Serialize};
use std::collections::{HashMap, HashSet};

use crate::db_entries::utils::JsonField;

#[derive(Clone, Debug, PartialEq, DeriveEntityModel, Serialize, Deserialize)]
#[sea_orm(table_name = "registry_entry")]
pub struct Model {
    #[sea_orm(primary_key, auto_increment = true)]
    pub id: u32,

    pub source_id: String,
    pub registry_id: String,
    pub archive_reference: Option<String>,

    pub registry_types: JsonField<HashSet<String>>,
    pub collection: JsonField<Vec<String>>,

    pub manifest_url: Option<String>,
    pub ark_url: Option<String>,

    pub title: Option<String>,
    pub subtitle: Option<String>,
    pub author: Option<String>,
    pub date_from: Option<String>,
    pub date_from_normalized: Option<DateTimeUtc>,
    pub date_to: Option<String>,
    pub date_to_normalized: Option<DateTimeUtc>,

    pub places: JsonField<HashSet<Vec<String>>>,
    pub notes: Option<String>,

    pub extra: JsonField<HashMap<String, String>>,
}

#[derive(Copy, Clone, Debug, EnumIter, DeriveRelation)]
pub enum Relation {
    #[sea_orm(has_many = "super::image_entry::Entity")]
    ImageEntry,
}

impl Related<super::image_entry::Entity> for Entity {
    fn to() -> RelationDef {
        Relation::ImageEntry.def()
    }
}

impl ActiveModelBehavior for ActiveModel {}

impl From<Model> for Registry {
    fn from(model: Model) -> Self {
        Self {
            source_id: model.source_id,
            registry_id: model.registry_id,
            archive_reference: model.archive_reference,
            registry_types: model.registry_types.0,
            collection: model.collection.0,
            manifest_url: model.manifest_url,
            ark_url: model.ark_url,
            title: model.title,
            subtitle: model.subtitle,
            author: model.author,
            date_from: model.date_from,
            date_from_normalized: model.date_from_normalized,
            date_to: model.date_to,
            date_to_normalized: model.date_to_normalized,
            places: model.places.0,
            notes: model.notes,
            extra: model.extra.0,
        }
    }
}

impl Model {
    pub fn from_registry(registry: Registry, id: u32) -> Self {
        Self {
            id,
            source_id: registry.source_id,
            registry_id: registry.registry_id,
            archive_reference: registry.archive_reference,
            registry_types: JsonField(registry.registry_types),
            collection: JsonField(registry.collection),
            manifest_url: registry.manifest_url,
            ark_url: registry.ark_url,
            title: registry.title,
            subtitle: registry.subtitle,
            author: registry.author,
            date_from: registry.date_from,
            date_from_normalized: registry.date_from_normalized,
            date_to: registry.date_to,
            date_to_normalized: registry.date_to_normalized,
            places: JsonField(registry.places),
            notes: registry.notes,
            extra: JsonField(registry.extra),
        }
    }
}
