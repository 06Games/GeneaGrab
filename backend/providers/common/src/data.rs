use std::collections::{HashMap, HashSet};
use serde::{Deserialize, Serialize};
use std::borrow::Cow;
use dates::HistoricalDate;
use derive_builder::Builder;
use strum::Display;

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct PluginMetadata {
    pub id: Cow<'static, str>,
    pub name: Cow<'static, str>,
    pub description: Option<Cow<'static, str>>,
    pub author: Option<Cow<'static, str>>,
    pub version: Option<Cow<'static, str>>,
    pub source_url: Option<Cow<'static, str>>,
    pub suggested_websites: Cow<'static, [Cow<'static, str>]>,
}

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
    pub date_from: Option<HistoricalDate>,
    #[builder(default)]
    pub date_from_normalized: Option<i32>,
    #[builder(default)]
    pub date_to: Option<HistoricalDate>,
    #[builder(default)]
    pub date_to_normalized: Option<i32>,

    #[builder(default)]
    #[serde(default)]
    pub places: HashSet<Vec<String>>,
    #[builder(default)]
    pub notes: Option<String>,

    #[builder(default)]
    #[serde(default)]
    pub extra: HashMap<String, String>,
}

#[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]
#[serde(tag = "category", content = "label", rename_all = "snake_case")]
pub enum RegistryType {
    Vital(Cow<'static, str>),
    Union(Cow<'static, str>),
    Mortality(Cow<'static, str>),
    Census(Cow<'static, str>),
    Legal(Cow<'static, str>),
    Land(Cow<'static, str>),
    Media(Cow<'static, str>),
    Military(Cow<'static, str>),
    Other(Cow<'static, str>),
    Unknown,
}

#[derive(Debug, Serialize, Deserialize, PartialEq, Clone)]
pub struct Image {
    pub width: Option<u32>,
    pub height: Option<u32>,
    pub tile_size: Option<u32>,
    pub manifest_url: Option<String>,
    pub api_url: Option<String>,
    pub download_url: Option<String>,
    pub ark_url: Option<String>,

    pub image_number: u32,
    pub name: Option<String>,
    pub date_range: Option<String>,
    pub notes: Option<String>,
}

#[derive(Debug, Clone, Copy, Serialize, Deserialize, Display, PartialEq, Eq)]
pub enum FetchMethod {
    GET,
    POST,
    PUT,
    DELETE,
    HEAD,
    OPTIONS,
    PATCH,
}

#[derive(Builder, Clone, Serialize, Deserialize)]
pub struct Request {
    pub url: String,
    #[builder(default = "vec![]")]
    pub headers: Vec<(String, String)>,
    #[builder(default = "FetchMethod::GET")]
    pub method: FetchMethod,
    #[builder(default = "None")]
    pub body: Option<String>,
}

impl From<String> for Request {
    fn from(url: String) -> Self {
        Self {
            url,
            headers: vec![],
            method: FetchMethod::GET,
            body: None,
        }
    }
}
