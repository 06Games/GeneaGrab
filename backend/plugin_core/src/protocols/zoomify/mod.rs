use anyhow::Error;
use serde::Deserialize;

use crate::{data::Image, protocols::utils::Fetch};

#[derive(Debug, Deserialize)]
#[serde(rename_all = "UPPERCASE")]
struct ImageProperties {
    width: u32,
    height: u32,
    tilesize: u32,
}

/// Represents a Zoomify-enabled image source
pub struct Zoomify {
    pub width: u32,
    pub height: u32,
    pub tile_size: u32,
    base_url: String,
}

/// Information about a specific zoom level's geometry
pub struct ZoomLevel {
    pub index: u32,
    pub width: u32,
    pub height: u32,
    pub tiles_x: u32,
    pub tiles_y: u32,
    pub scale_factor: u32,
}

impl Zoomify {
    pub fn new(image: Image) -> Self {
        Self {
            width: image.width.unwrap_or(0),
            height: image.height.unwrap_or(0),
            tile_size: image.tile_size.unwrap_or(256),
            base_url: image
                .manifest_url
                .unwrap_or_default()
                .trim_end_matches('/')
                .to_string(),
        }
    }

    /// Initialize by fetching metadata from the server
    pub fn fetch(base_url: &str, fetcher: impl Fetch) -> Result<Self, Error> {
        let base_url = base_url.trim_end_matches('/').to_string();
        let xml_url = format!("{}/ImageProperties.xml", base_url);
        let raw_xml = fetcher.fetch(&xml_url)?;

        // Some Zoomify servers omit a root node; we wrap it to be safe
        let wrapped = format!("<r>{}</r>", raw_xml);

        #[derive(Deserialize)]
        struct Root {
            #[serde(rename = "IMAGE_PROPERTIES")]
            props: ImageProperties,
        }
        let parsed: Root = quick_xml::de::from_str(&wrapped)?;

        Ok(Self {
            width: parsed.props.width,
            height: parsed.props.height,
            tile_size: parsed.props.tilesize,
            base_url,
        })
    }

    /// The maximum zoom level available (original resolution)
    pub fn max_level(&self) -> u32 {
        let max_dim = self.width.max(self.height) as f64;
        let ratio = max_dim / self.tile_size as f64;
        ratio.log2().ceil() as u32
    }

    /// Returns the geometry for a specific level index
    pub fn level(&self, index: u32) -> ZoomLevel {
        let max = self.max_level();
        let index = index.min(max);
        let scale_factor = 2u32.pow(max - index);

        // Dimensions at this specific zoom level
        let l_width = self.width / scale_factor;
        let l_height = self.height / scale_factor;

        ZoomLevel {
            index,
            width: l_width,
            height: l_height,
            tiles_x: (l_width as f32 / self.tile_size as f32).ceil() as u32,
            tiles_y: (l_height as f32 / self.tile_size as f32).ceil() as u32,
            scale_factor,
        }
    }

    /// Helper to generate a tile URL for a specific level and coordinate
    pub fn tile_url(&self, level: u32, x: u32, y: u32) -> String {
        format!("{}/TileGroup0/{}-{}-{}.jpg", self.base_url, level, x, y)
    }
}
