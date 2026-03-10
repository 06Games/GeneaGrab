use sea_orm::entity::prelude::*;
use serde::{Deserialize, Serialize};

#[derive(Clone, Debug, PartialEq, DeriveEntityModel, Serialize, Deserialize)]
#[sea_orm(table_name = "image_entry")]
pub struct Model {
    #[sea_orm(primary_key, auto_increment = false)]
    pub id: String,
    pub registry_entry_id: String,

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
