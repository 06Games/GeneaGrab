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
pub mod fetcher;
pub mod image;

const PLUGIN_METADATA: PluginMetadata = PluginMetadata {
    id: Cow::Borrowed("ad06"),
    name: Cow::Borrowed("AD06"),
    description: Some(Cow::Borrowed(
        "A plugin for extracting data from Alpes-Maritimes (France) Departmental Archives.",
    )),
    author: Some(Cow::Borrowed("Evan Galli")),
    version: Some(Cow::Borrowed("1.0.0")),
    source_url: Some(Cow::Borrowed(
        "https://github.com/06Games/GeneaGrab/tree/v4/backend/plugins/ad06",
    )),
    suggested_websites: Cow::Borrowed(&[std::borrow::Cow::Borrowed("https://archives06.fr/")]),
};

static ARK_REGEX: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(r"/ark:/(?P<naan>[\w\.]+)(?:/(?P<document_id>[\w\.]+))?(?:/(?P<view_type>[\w\.]+))?(?:/(?P<sequence>\d+))?(?:/(?P<image_number>\d+))?")
        .expect("Invalid regex pattern")
});

pub struct Ad06Provider {
    pub flaresolverr_url: String,
    pub fetcher: std::sync::Arc<dyn Fetcher>,
}

impl Ad06Provider {
    #[must_use]
    pub fn new(flaresolverr_url: &str, fetcher: std::sync::Arc<dyn Fetcher>) -> Self {
        Self {
            flaresolverr_url: flaresolverr_url.to_string(),
            fetcher,
        }
    }
}

#[async_trait::async_trait]
impl ArchiveProvider for Ad06Provider {
    fn metadata(&self) -> PluginMetadata {
        PLUGIN_METADATA.clone()
    }

    fn identify(&self, req: IdentifyRequest) -> Result<IdentifyResponse, ProviderError> {
        let url =
            url::Url::parse(&req.url).map_err(|e| ProviderError::InvalidField(e.to_string()))?;

        let host = url.host_str();
        if host != Some("archives06.fr") || !url.path().starts_with("/ark:/") {
            return Err(ProviderError::InvalidField("Not an AD06 URL".into()));
        }
        let host = host.unwrap();

        let captures = ARK_REGEX
            .captures(url.path())
            .ok_or(ProviderError::InvalidField("Invalid ARK URL".into()))?;
        let name_assigning_authority_number =
            captures.name("naan").map_or("", |m| m.as_str()).to_string();
        let document_id = captures
            .name("document_id")
            .map_or("", |m| m.as_str())
            .to_string();
        let image_number = captures
            .name("image_number")
            .and_then(|m| m.as_str().parse::<u32>().ok())
            .unwrap_or(1);

        let ark_url =
            format!("https://{host}/ark:/{name_assigning_authority_number}/{document_id}");
        Ok(IdentifyResponse {
            registry_id: document_id,
            image_number: Some(image_number),
            ark_url: Some(ark_url),
        })
    }

    async fn extract_registry(
        &self,
        req: ExtractRequest,
    ) -> Result<ExtractResponse, ProviderError> {
        extract::extract_registry(&req, &self.flaresolverr_url, &*self.fetcher).await
    }

    fn is_image_missing_data(&self, req: &ExtractImageRequest) -> Result<Option<String>, ProviderError> {
        Ok(image::is_image_missing_data(req))
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
        req: DownloadRequest,
    ) -> Result<Option<TileResponse>, ProviderError> {
        image::download_image(&req, &self.flaresolverr_url, &*self.fetcher).await
    }
}
