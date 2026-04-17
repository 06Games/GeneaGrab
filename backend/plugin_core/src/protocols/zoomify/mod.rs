use serde::Deserialize;

use crate::{
    com_structs::PluginError, data::Image, protocols::fetchers::Fetcher, utils::ImageGeometry,
};

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
    pub geometry: ImageGeometry,
    base_url: Option<String>,
}

impl TryFrom<Image> for Zoomify {
    type Error = PluginError;

    fn try_from(value: Image) -> Result<Self, Self::Error> {
        let width = value
            .width
            .ok_or_else(|| PluginError::MissingField("width".into()))?;
        let height = value
            .height
            .ok_or_else(|| PluginError::MissingField("height".into()))?;
        let tile_size = value.tile_size.unwrap_or(256);

        Ok(Self {
            geometry: ImageGeometry::new(width, height, tile_size),
            base_url: value
                .manifest_url
                .map(|url| url.trim_end_matches('/').to_string()),
        })
    }
}

impl Zoomify {
    /// Initialize by fetching metadata from the server
    ///
    /// # Errors
    ///
    /// Network or parsing errors
    pub fn fetch(base_url: &str, fetcher: &impl Fetcher) -> Result<Self, PluginError> {
        let base_url = base_url.trim_end_matches('/').to_string();
        let raw_xml = fetcher.fetch(format!("{base_url}/ImageProperties.xml").into())?;

        let parsed = quick_xml::de::from_str::<ImageProperties>(&raw_xml)
            .map_err(|e| PluginError::ParsingError(e.to_string()))?;

        Ok(Self {
            geometry: ImageGeometry::new(parsed.width, parsed.height, parsed.tile_size),
            base_url: Some(base_url),
        })
    }

    /// Helper to generate a tile URL for a specific level and coordinate
    ///
    /// # Errors
    ///
    /// If a required field is missing from the current struct or if there's an incoherence between what we know from Zoomify and what was given
    pub fn tile_url(&self, level: u32, x: u32, y: u32) -> Result<String, PluginError> {
        let base_url = self
            .base_url
            .as_ref()
            .ok_or_else(|| PluginError::MissingField("base_url".into()))?;

        let properties = self.geometry.level(level);
        if level != properties.level {
            return Err(PluginError::InvalidField("zoom".into()));
        }
        if x >= properties.tiles_x || y >= properties.tiles_y {
            return Err(PluginError::InvalidField("tile_coordinates".into()));
        }

        Ok(format!("{base_url}/TileGroup0/{level}-{x}-{y}.jpg"))
    }
}
