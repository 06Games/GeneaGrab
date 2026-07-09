use serde::Deserialize;
use crate::{
    errors::ProviderError, data::Image, traits::Fetcher, utils::ImageGeometry
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

pub struct Zoomify {
    pub geometry: ImageGeometry,
    base_url: Option<String>,
}

impl TryFrom<Image> for Zoomify {
    type Error = ProviderError;

    fn try_from(value: Image) -> Result<Self, Self::Error> {
        let width = value
            .width
            .ok_or_else(|| ProviderError::MissingField("width".into()))?;
        let height = value
            .height
            .ok_or_else(|| ProviderError::MissingField("height".into()))?;
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
    pub async fn fetch(base_url: &str, fetcher: &dyn Fetcher) -> Result<Self, ProviderError> {
        let base_url = base_url.trim_end_matches('/').to_string();
        let raw_xml = fetcher.fetch(format!("{base_url}/ImageProperties.xml").into()).await?;

        let parsed = quick_xml::de::from_str::<ImageProperties>(&raw_xml)
            .map_err(|e| ProviderError::ParsingError(e.to_string()))?;

        Ok(Self {
            geometry: ImageGeometry::new(parsed.width, parsed.height, parsed.tile_size),
            base_url: Some(base_url),
        })
    }

    pub fn tile_url(&self, level: u32, x: u32, y: u32) -> Result<String, ProviderError> {
        let base_url = self
            .base_url
            .as_ref()
            .ok_or_else(|| ProviderError::MissingField("base_url".into()))?;

        let properties = self.geometry.level(level);
        if level != properties.level {
            return Err(ProviderError::InvalidField("zoom".into()));
        }
        if x >= properties.tiles_x || y >= properties.tiles_y {
            return Err(ProviderError::InvalidField("tile_coordinates".into()));
        }

        Ok(format!("{base_url}/TileGroup0/{level}-{x}-{y}.jpg"))
    }
}
