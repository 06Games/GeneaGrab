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

fn de_opt_u32_from_str_or_number<'de, D>(deserializer: D) -> Result<Option<u32>, D::Error>
where
    D: serde::Deserializer<'de>,
{
    #[derive(Deserialize)]
    #[serde(untagged)]
    enum IntOrString {
        Int(u32),
        String(String),
    }

    Ok(match Option::<IntOrString>::deserialize(deserializer)? {
        Some(IntOrString::Int(i)) => Some(i),
        Some(IntOrString::String(s)) => s.parse().ok(),
        None => None,
    })
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
    #[serde(rename = "@id", default)]
    pub id: Option<String>,
    pub resource: Option<Resource>,
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct Resource {
    #[serde(rename = "@id", default)]
    pub id: Option<String>,
    pub format: Option<String>,
    #[serde(default, deserialize_with = "de_opt_u32_from_str_or_number")]
    pub width: Option<u32>,
    #[serde(default, deserialize_with = "de_opt_u32_from_str_or_number")]
    pub height: Option<u32>,
    pub service: Option<Service>,
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct Service {
    #[serde(rename = "@id", default)]
    pub id: Option<String>,
}
