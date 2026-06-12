use serde::{Deserialize, Serialize};

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
