use serde::Deserialize;

use crate::{com_structs::PluginError, data::Image, protocols::fetchers::Fetcher};

#[derive(Debug, Deserialize)]
struct ImageProperties {
    #[serde(rename = "@WIDTH")]
    width: u32,
    #[serde(rename = "@HEIGHT")]
    height: u32,
    #[serde(rename = "@TILESIZE")]
    tile_size: u32,
}

/// Represents a Zoomify-enabled image source
pub struct Zoomify {
    pub width: u32,
    pub height: u32,
    pub tile_size: u32,
    base_url: Option<String>,
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

impl TryFrom<Image> for Zoomify {
    type Error = PluginError;

    fn try_from(value: Image) -> Result<Self, Self::Error> {
        Ok(Self {
            width: value
                .width
                .ok_or_else(|| PluginError::MissingField("width".into()))?,
            height: value
                .height
                .ok_or_else(|| PluginError::MissingField("height".into()))?,
            tile_size: value.tile_size.unwrap_or(256),
            base_url: value
                .manifest_url
                .map(|url| url.trim_end_matches('/').to_string()),
        })
    }
}

impl Zoomify {
    /// Initialize by fetching metadata from the server
    pub fn fetch(base_url: &str, fetcher: impl Fetcher) -> Result<Self, PluginError> {
        let base_url = base_url.trim_end_matches('/').to_string();
        let raw_xml = fetcher.fetch(format!("{}/ImageProperties.xml", base_url).into())?;

        let parsed = quick_xml::de::from_str::<ImageProperties>(&raw_xml)
            .map_err(|e| PluginError::ParsingError(e.to_string()))?;

        Ok(Self {
            width: parsed.width,
            height: parsed.height,
            tile_size: parsed.tile_size,
            base_url: Some(base_url),
        })
    }

    /// The maximum zoom level available
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
    pub fn tile_url(&self, level: u32, x: u32, y: u32) -> Result<String, PluginError> {
        let base_url = self
            .base_url
            .as_ref()
            .ok_or_else(|| PluginError::MissingField("base_url".into()))?;

        let properties = self.level(level);
        if level != properties.index {
            return Err(PluginError::InvalidField("zoom".into()));
        }
        if x >= properties.tiles_x || y >= properties.tiles_y {
            return Err(PluginError::InvalidField("tile_coordinates".into()));
        }

        Ok(format!("{}/TileGroup0/{}-{}-{}.jpg", base_url, level, x, y))
    }
}
