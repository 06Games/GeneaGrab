use serde::{Deserialize, Serialize};
use std::collections::{HashMap, HashSet};

#[derive(Debug, Serialize, Deserialize)]
pub struct RegistryMeta {
    pub source_id: String,
    pub archive_reference: String,
    pub source_types: HashSet<String>,
    pub town: String,
    pub repository_url: String,
    pub total_images: u32,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct UserImageMeta {
    pub name: Option<String>,
    pub date_range: Option<String>,
    pub notes: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ImageMeta {
    pub name: Option<String>,
    pub date_range: Option<String>,
    pub notes: Option<String>,
    pub image_number: u32,
    pub act_types: HashMap<String, u32>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct EventRow {
    pub event_id: u32,
    pub date: String,
    pub event_type: String,
    pub title: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct PersonEntry {
    pub person_id: String,
    pub role: String,
    pub first_name: String,
    pub last_name: String,
    pub sex: String,
    pub title: String,
    pub age: String,
    pub is_deceased: bool,
    pub occupation: String,
    pub origin_place: String,
    pub residence_place: String,
    pub sequence_number: String,
    pub notes: String,
    pub relationship_type: String,
    pub relationship_to: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct EventDetail {
    pub event_id: u32,
    pub date: String,
    pub date_normalized: String,
    pub event_type: String,
    pub title: String,
    pub act_number: String,
    pub page: String,
    pub image_number: String,
    pub town: String,
    pub parish: String,
    pub hamlet: String,
    pub transcription_text: String,
    pub notes: String,
    pub people: Vec<PersonEntry>,
}
