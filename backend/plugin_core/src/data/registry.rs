use std::collections::{HashMap, HashSet};

use chrono::{DateTime, Utc};
use derive_builder::Builder;
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize, PartialEq, Builder)]
pub struct Registry {
    pub source_id: String,
    pub registry_id: String,
    #[builder(default)]
    pub archive_reference: Option<String>,

    #[builder(default)]
    pub registry_types: HashSet<String>,
    #[builder(default)]
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
    pub places: HashSet<String>,
    #[builder(default)]
    pub notes: Option<String>,

    #[builder(default)]
    pub extra: HashMap<String, String>,
}
