use serde::{Deserialize, Serialize};

use crate::data::{Image, Registry};

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct ExtractImageRequest {
    pub registry: Registry,
    pub image: Image,
}

#[derive(Debug, Serialize, Deserialize)]
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

#[derive(Debug, Serialize, Deserialize)]
pub struct TileResponse {
    pub data: Vec<u8>,
    pub mime_type: String,
}
