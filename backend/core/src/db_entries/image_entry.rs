use sea_orm::entity::prelude::*;
use serde::{Deserialize, Serialize};

use geneagrab_plugin_core::data::Image;

#[derive(Clone, Debug, PartialEq, DeriveEntityModel, Serialize, Deserialize)]
#[sea_orm(table_name = "image_entry")]
pub struct Model {
    #[sea_orm(primary_key, auto_increment = true)]
    pub id: u32,
    pub registry_entry_id: u32,

    pub width: Option<u32>,
    pub height: Option<u32>,
    pub tile_size: Option<u32>,
    pub manifest_url: Option<String>,
    pub ark_url: Option<String>,

    pub image_number: u32,
    pub name: Option<String>,
    pub date_range: Option<String>,
    pub notes: Option<String>,
}

#[derive(Copy, Clone, Debug, EnumIter, DeriveRelation)]
pub enum Relation {
    #[sea_orm(
        belongs_to = "super::registry_entry::Entity",
        from = "Column::RegistryEntryId",
        to = "super::registry_entry::Column::Id",
        on_update = "Cascade",
        on_delete = "Cascade"
    )]
    RegistryEntry,
}

impl Related<super::registry_entry::Entity> for Entity {
    fn to() -> RelationDef {
        Relation::RegistryEntry.def()
    }
}

impl ActiveModelBehavior for ActiveModel {}

impl From<Model> for Image {
    fn from(model: Model) -> Self {
        Self {
            width: model.width,
            height: model.height,
            tile_size: model.tile_size,
            manifest_url: model.manifest_url,
            ark_url: model.ark_url,

            image_number: model.image_number,
            name: model.name,
            date_range: model.date_range,
            notes: model.notes,
        }
    }
}

impl Model {
    #[must_use] 
    pub fn from_image(image: Image, id: u32, registry_entry_id: u32) -> Self {
        Self {
            id,
            registry_entry_id,
            width: image.width,
            height: image.height,
            tile_size: image.tile_size,
            manifest_url: image.manifest_url,
            ark_url: image.ark_url,
            image_number: image.image_number,
            name: image.name,
            date_range: image.date_range,
            notes: image.notes,
        }
    }
}
