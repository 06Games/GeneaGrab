use std::collections::{HashMap, HashSet};

use chrono::{DateTime, Utc};
use derive_builder::Builder;
use serde::{Deserialize, Serialize};

use crate::data::RegistryType;

#[derive(Debug, Serialize, Deserialize, PartialEq, Builder, Clone)]
pub struct Registry {
    pub source_id: String,
    pub registry_id: String,
    #[builder(default)]
    pub archive_reference: Option<String>,

    #[builder(default)]
    #[serde(default)]
    pub registry_types: HashSet<RegistryType>,
    #[builder(default)]
    #[serde(default)]
    pub collection: Vec<String>,

    #[builder(default)]
    pub manifest_url: Option<String>,
    #[builder(default)]
    pub ark_url: Option<String>,

    #[builder(default)]
    pub title: Option<String>,
    #[builder(default)]
    pub subtitle: Option<String>,
    #[builder(default)]
    pub author: Option<String>,
    #[builder(default)]
    pub date_from: Option<String>,
    #[builder(default)]
    pub date_from_normalized: Option<DateTime<Utc>>,
    #[builder(default)]
    pub date_to: Option<String>,
    #[builder(default)]
    pub date_to_normalized: Option<DateTime<Utc>>,

    #[builder(default)]
    #[serde(default)]
    pub places: HashSet<Vec<String>>,
    #[builder(default)]
    pub notes: Option<String>,

    #[builder(default)]
    #[serde(default)]
    pub extra: HashMap<String, String>,
}
