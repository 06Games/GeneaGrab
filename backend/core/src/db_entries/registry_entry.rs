use sea_orm::entity::prelude::*;
use serde::{Deserialize, Serialize};
use std::collections::{HashMap, HashSet};

#[derive(Clone, Debug, PartialEq, Serialize, Deserialize, FromJsonQueryResult)]
pub struct RegistryTypes(pub HashSet<String>);

#[derive(Clone, Debug, PartialEq, Serialize, Deserialize, FromJsonQueryResult)]
pub struct StringCollection(pub Vec<String>);

#[derive(Clone, Debug, PartialEq, Serialize, Deserialize, FromJsonQueryResult)]
pub struct ExtraData(pub HashMap<String, String>);

#[derive(Clone, Debug, PartialEq, DeriveEntityModel, Serialize, Deserialize)]
#[sea_orm(table_name = "registry_entry")]
pub struct Model {
    #[sea_orm(primary_key, auto_increment = false)]
    pub id: String,

    pub source_id: String,
    pub registry_id: String,
    pub archive_reference: String,
    
    pub registry_types: RegistryTypes,
    pub collection: StringCollection,
    
    pub manifest_url: Option<String>,
    pub ark_url: Option<String>,

    pub title: Option<String>,
    pub subtitle: Option<String>,
    pub author: Option<String>,
    pub date_from: Option<String>, 
    pub date_from_normalized: Option<DateTimeUtc>,
    pub date_to: Option<String>,
    pub date_to_normalized: Option<DateTimeUtc>,
    
    pub places: StringCollection, 
    pub notes: Option<String>,

    pub extra: ExtraData,
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
