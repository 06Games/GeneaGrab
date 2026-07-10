use std::borrow::Cow;
use std::sync::LazyLock;
use regex::Regex;

use geneagrab_providers::traits::{ArchiveProvider, Fetcher};
use geneagrab_providers::data::PluginMetadata;
use geneagrab_providers::com_structs::{
    IdentifyRequest, IdentifyResponse, ExtractRequest, ExtractResponse,
    ExtractImageRequest, ExtractImageResponse, TileRequest, TileResponse, DownloadRequest
};
use geneagrab_providers::errors::ProviderError;

pub mod extract;
pub mod image;

const PLUGIN_METADATA: PluginMetadata = PluginMetadata {
    id: Cow::Borrowed("geneanet"),
    name: Cow::Borrowed("Geneanet"),
    description: Some(Cow::Borrowed("A plugin for extracting data from Geneanet.")),
    author: Some(Cow::Borrowed("Evan Galli")),
    version: Some(Cow::Borrowed("1.0.0")),
    source_url: Some(Cow::Borrowed(
        "https://github.com/06Games/GeneaGrab/tree/v4/backend/plugins/geneanet",
    )),
    suggested_websites: Cow::Borrowed(&[std::borrow::Cow::Borrowed("https://www.geneanet.org/")]),
};

static URL_REGEX: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(
        r"(?:idcollection=(?<col1>\d*).*page=(?<page1>\d*))|(?:/(?<col2>\d+)(?:\z|/(?<page2>\d*)))",
    )
    .expect("Invalid regex pattern")
});

pub struct GeneanetProvider {
    pub flaresolverr_url: String,
    pub fetcher: std::sync::Arc<dyn Fetcher>,
}

impl GeneanetProvider {
    #[must_use]
    pub fn new(flaresolverr_url: &str, fetcher: std::sync::Arc<dyn Fetcher>) -> Self {
        Self {
            flaresolverr_url: flaresolverr_url.to_string(),
            fetcher,
        }
    }
}

#[async_trait::async_trait]
impl ArchiveProvider for GeneanetProvider {
    fn metadata(&self) -> PluginMetadata {
        PLUGIN_METADATA.clone()
    }

    fn identify(&self, req: IdentifyRequest) -> Result<IdentifyResponse, ProviderError> {
        let captures = URL_REGEX.captures(&req.url);

        let col_value = captures
            .as_ref()
            .and_then(|caps| caps.name("col1").or(caps.name("col2")))
            .map(|m| m.as_str())
            .filter(|s| !s.is_empty())
            .ok_or_else(|| ProviderError::InvalidField("Collection ID not found".into()))?;

        let image_number = captures
            .as_ref()
            .and_then(|caps| caps.name("page1").or(caps.name("page2")))
            .map(|m| m.as_str())
            .filter(|s| !s.is_empty())
            .and_then(|s| s.parse::<u32>().ok())
            .unwrap_or(1);

        Ok(IdentifyResponse {
            registry_id: col_value.into(),
            image_number: Some(image_number),
            ark_url: None,
        })
    }

    async fn extract_registry(
        &self,
        req: ExtractRequest,
    ) -> Result<ExtractResponse, ProviderError> {
        extract::extract_registry(&req, &self.flaresolverr_url, &*self.fetcher).await
    }

    fn is_image_missing_data(&self, req: &ExtractImageRequest) -> Result<Option<String>, ProviderError> {
        image::is_image_missing_data(req)
    }

    async fn extract_image(
        &self,
        req: ExtractImageRequest,
    ) -> Result<ExtractImageResponse, ProviderError> {
        image::extract_image(&req, &self.flaresolverr_url, &*self.fetcher).await
    }

    async fn fetch_tile(
        &self,
        req: TileRequest,
    ) -> Result<TileResponse, ProviderError> {
        image::fetch_tile(&req, &self.flaresolverr_url, &*self.fetcher).await
    }

    async fn download_image(
        &self,
        _req: DownloadRequest,
    ) -> Result<Option<TileResponse>, ProviderError> {
        image::download_image()
    }
}
