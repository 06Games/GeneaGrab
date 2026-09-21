#![warn(clippy::pedantic)]

use geneagrab_providers::protocols::ligeo;
use std::borrow::Cow;

use geneagrab_providers::com_structs::{
    DownloadRequest, ExtractImageRequest, ExtractImageResponse, ExtractRequest, ExtractResponse,
    IdentifyRequest, IdentifyResponse, TileRequest, TileResponse,
};
use geneagrab_providers::data::ProviderMetadata;
use geneagrab_providers::errors::ProviderError;
use geneagrab_providers::traits::{ArchiveProvider, Fetcher};

pub mod extract;
pub mod fetcher;
pub mod image;

const PROVIDER_METADATA: ProviderMetadata = ProviderMetadata {
    id: Cow::Borrowed("ad06"),
    name: Cow::Borrowed("AD06"),
    description: Some(Cow::Borrowed(
        "A provider for extracting data from Alpes-Maritimes (France) Departmental Archives.",
    )),
    author: Some(Cow::Borrowed("Evan Galli")),
    version: Some(Cow::Borrowed("1.0.0")),
    source_url: Some(Cow::Borrowed(
        "https://github.com/06Games/GeneaGrab/tree/v4/backend/providers/ad06",
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
    fn metadata(&self) -> ProviderMetadata {
        PROVIDER_METADATA.clone()
    }

    fn identify(&self, req: IdentifyRequest) -> Result<IdentifyResponse, ProviderError> {
        let url =
            url::Url::parse(&req.url).map_err(|e| ProviderError::InvalidField(e.to_string()))?;

        if url.host_str() != Some("archives06.fr") || !url.path().starts_with("/ark:/") {
            return Err(ProviderError::InvalidField("Not an AD06 URL".into()));
        }

        ligeo::parse_url(url).ok_or(ProviderError::InvalidField("Invalid ARK URL".into()))
    }

    async fn extract_registry(
        &self,
        req: ExtractRequest,
    ) -> Result<ExtractResponse, ProviderError> {
        extract::extract_registry(&req, &self.flaresolverr_url, &*self.fetcher).await
    }

    fn is_image_missing_data(
        &self,
        req: &ExtractImageRequest,
    ) -> Result<Option<String>, ProviderError> {
        Ok(image::is_image_missing_data(req))
    }

    async fn extract_image(
        &self,
        req: ExtractImageRequest,
    ) -> Result<ExtractImageResponse, ProviderError> {
        image::extract_image(&req, &self.flaresolverr_url, &*self.fetcher).await
    }

    async fn fetch_tile(&self, req: TileRequest) -> Result<TileResponse, ProviderError> {
        image::fetch_tile(&req, &self.flaresolverr_url, &*self.fetcher).await
    }

    async fn download_image(
        &self,
        req: DownloadRequest,
    ) -> Result<Option<TileResponse>, ProviderError> {
        image::download_image(&req, &self.flaresolverr_url, &*self.fetcher).await
    }
}
