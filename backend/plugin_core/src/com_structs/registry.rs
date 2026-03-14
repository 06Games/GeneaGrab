use crate::data::{Image, Registry};
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct ExtractRequest {
    pub url: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ExtractResponse {
    pub registry: Registry,
    pub images: Vec<Image>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct IdentifyRequest {
    pub url: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct IdentifyResponse {
    pub is_supported: bool,
    pub registry_id: Option<String>,
    pub frame_number: Option<u32>,
}
