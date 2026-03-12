use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct PluginOption {
    pub id: String,
    pub name: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ExtractRequest {
    pub url: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ExtractResponse {
    pub archive_reference: String,
    pub title: Option<String>,
    pub total_images: u32,
}
