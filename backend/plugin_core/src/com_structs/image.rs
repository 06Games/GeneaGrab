use std::collections::HashMap;

use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct TileRequest {
    pub image_id: u32,
    pub width: u32,
    pub height: u32,
    pub tile_size: u32,
    pub z: u32,
    pub x: u32,
    pub y: u32,
    pub manifest_url: Option<String>,
    pub ark_url: Option<String>,
    pub extra_data: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct TileResponse {
    pub url: String,
    pub headers: Option<HashMap<String, String>>,
}

// Replaces `Ark`
#[derive(Debug, Serialize, Deserialize)]
pub struct ArkRequest {
    pub frame_number: u32,
    pub registry_ark: Option<String>,
    pub image_ark: Option<String>,
    pub extra_data: Option<String>,
}
