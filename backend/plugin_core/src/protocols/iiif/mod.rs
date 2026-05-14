use std::fmt::Display;

use serde::{Deserialize, Serialize};

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct Manifest {
    #[serde(rename = "@id")]
    pub id: String,
    #[serde(default)]
    pub metadata: Vec<Metadata>,
    #[serde(default)]
    pub sequences: Vec<Sequence>,
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct Metadata {
    pub label: String,
    pub value: serde_json::Value,
}

impl Display for Metadata {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match &self.value {
            serde_json::Value::String(s) => write!(f, "{s}"),
            serde_json::Value::Array(arr) => {
                let strings = arr
                    .iter()
                    .filter_map(|v| v.as_str())
                    .collect::<Vec<_>>()
                    .join("\n");
                write!(f, "{strings}")
            }
            v => write!(f, "{v}"),
        }
    }
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct Sequence {
    #[serde(rename = "@id")]
    pub id: String,
    pub label: Option<String>,
    #[serde(default)]
    pub canvases: Vec<Canvas>,
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct Canvas {
    #[serde(rename = "@id")]
    pub id: String,
    pub label: Option<String>,
    #[serde(default)]
    pub images: Vec<ImageAnno>,
    #[serde(flatten)]
    pub extra: serde_json::Value, // Dynamically captures protocol extensions like Ligeo
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct ImageAnno {
    #[serde(rename = "@id")]
    pub id: String,
    pub resource: Option<Resource>,
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct Resource {
    #[serde(rename = "@id")]
    pub id: String,
    pub format: Option<String>,
    pub width: Option<u32>,
    pub height: Option<u32>,
    pub service: Option<Service>,
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct Service {
    #[serde(rename = "@id")]
    pub id: String,
}
