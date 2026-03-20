use chrono::{DateTime, Utc};
use serde::{Deserialize, Deserializer, Serialize};
use std::collections::{HashMap, HashSet};
use std::str::FromStr;
use std::fmt::Display;

fn empty_string_as_none<'de, D, T>(deserializer: D) -> Result<Option<T>, D::Error>
where
    D: Deserializer<'de>,
    T: FromStr,
    T::Err: Display,
{
    let opt: Option<String> = Option::deserialize(deserializer)?;
    match opt {
        None => Ok(None),
        Some(s) if s.trim().is_empty() => Ok(None),
        Some(s) => s.parse::<T>()
            .map(Some)
            .map_err(serde::de::Error::custom),
    }
}

fn empty_string_as_none_patch<'de, D, T>(deserializer: D) -> Result<Option<Option<T>>, D::Error>
where
    D: Deserializer<'de>,
    T: FromStr,
    T::Err: Display,
{
    match empty_string_as_none(deserializer) {
        Ok(opt) => Ok(Some(opt)),
        Err(e) => Err(e),
    }
}

#[derive(Debug, Serialize, Deserialize)]
pub struct RegistryFilters {
    #[serde(default, deserialize_with = "empty_string_as_none")]
    pub search_term: Option<String>,
    #[serde(default, deserialize_with = "empty_string_as_none")]
    pub source_type: Option<String>,
    #[serde(default, deserialize_with = "empty_string_as_none")]
    pub place: Option<String>,
    #[serde(default, deserialize_with = "empty_string_as_none")]
    pub collection: Option<String>,
    #[serde(default, deserialize_with = "empty_string_as_none")]
    pub date_from: Option<DateTime<Utc>>,
    #[serde(default, deserialize_with = "empty_string_as_none")]
    pub date_to: Option<DateTime<Utc>>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct CursorPayload<T> {
    pub limit: u64,
    pub cursor: Option<u32>,
    pub filters: Option<T>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct CursorResponse<T> {
    pub data: Vec<T>,
    pub next_cursor: Option<u32>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct RegistryMeta {
    pub id: u32,
    pub archive_reference: Option<String>,
    pub source_types: HashSet<String>,
    pub places: HashSet<String>,
    pub collection: Vec<String>,
    pub ark_url: Option<String>,

    pub title: Option<String>,
    pub subtitle: Option<String>,
    pub author: Option<String>,
    pub date_from: Option<String>,
    pub date_to: Option<String>,
    pub notes: Option<String>,

    pub total_images: u32,
    pub acts_count: u32
}

#[derive(Debug, Serialize, Deserialize)]
pub struct UserImageMeta {
    #[serde(default, deserialize_with = "empty_string_as_none_patch")]
    pub name: Option<Option<String>>,
    #[serde(default, deserialize_with = "empty_string_as_none_patch")]
    pub date_range: Option<Option<String>>,
    #[serde(default, deserialize_with = "empty_string_as_none_patch")]
    pub notes: Option<Option<String>>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ImageMeta {
    pub image_number: u32,
    pub name: Option<String>,
    pub date_range: Option<String>,
    pub notes: Option<String>,
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
