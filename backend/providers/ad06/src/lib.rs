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
    long_name: Some(Cow::Borrowed(
        "Alpes-Maritimes (France) Departmental Archives",
    )),
    description: Some(Cow::Borrowed("You might want to install the UserScript for easier use.")),
    suggested_websites: Cow::Borrowed(&[std::borrow::Cow::Borrowed("https://archives06.fr/")]),
};

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
