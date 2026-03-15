use crate::data::{Image, Registry};
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct ExtractRequest {
    pub url: String,
    pub identified: IdentifyResponse
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
    pub registry_id: String,
    pub image_number: Option<u32>,
}
