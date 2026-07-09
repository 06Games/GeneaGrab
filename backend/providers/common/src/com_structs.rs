use crate::data::{Image, Registry};
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct ExtractImageRequest {
    pub registry: Registry,
    pub image: Image,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct ExtractImageResponse {
    pub image: Image,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct TileRequest {
    pub image: Image,
    pub zoom: u32,
    pub x: u32,
    pub y: u32,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct TileResponse {
    pub data: Vec<u8>,
    pub mime_type: String,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct DownloadRequest {
    pub image: Image,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct ExtractRequest {
    pub url: String,
    pub identified: IdentifyResponse,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct ExtractResponse {
    pub registry: Registry,
    pub images: Vec<Image>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct IdentifyRequest {
    pub url: String,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct IdentifyResponse {
    pub registry_id: String,
    pub image_number: Option<u32>,
    pub ark_url: Option<String>,
}
