use std::collections::{HashMap, HashSet};

use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct Registry {
    pub source_id: String,
    pub registry_id: String,
    pub archive_reference: String,

    pub registry_types: HashSet<String>,
    pub collection: Vec<String>,

    pub manifest_url: Option<String>,
    pub ark_url: Option<String>,

    pub title: Option<String>,
    pub subtitle: Option<String>,
    pub author: Option<String>,
    pub date_from: Option<String>,
    pub date_from_normalized: Option<DateTime<Utc>>,
    pub date_to: Option<String>,
    pub date_to_normalized: Option<DateTime<Utc>>,

    pub places: HashSet<String>,
    pub notes: Option<String>,

    pub extra: HashMap<String, String>,
}
