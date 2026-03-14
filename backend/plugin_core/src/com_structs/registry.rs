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
