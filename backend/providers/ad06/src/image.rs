use geneagrab_providers::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, ExtractImageResponse, TileRequest,
        TileResponse,
    },
    errors::ProviderError,
    traits::Fetcher,
    protocols::iiif_image_api,
};

use crate::fetcher::AdamFetcher;

#[must_use]
pub(crate) fn is_image_missing_data(req: &ExtractImageRequest) -> Option<String> {
    iiif_image_api::is_image_missing_data(req)
}

pub(crate) async fn extract_image(
    req: &ExtractImageRequest,
    flaresolverr_url: &str,
    base_fetcher: &dyn Fetcher,
) -> Result<ExtractImageResponse, ProviderError> {
    let adam_fetcher = AdamFetcher::new(flaresolverr_url, base_fetcher);
    iiif_image_api::extract_image(req, &adam_fetcher).await
}

pub(crate) async fn fetch_tile(
    req: &TileRequest,
    flaresolverr_url: &str,
    base_fetcher: &dyn Fetcher,
) -> Result<TileResponse, ProviderError> {
    let adam_fetcher = AdamFetcher::new(flaresolverr_url, base_fetcher);
    // AD06 specific constraint: neither supports ^ and ! modifiers nor percentages.
    // We pass a custom formatter that strictly limits the parameter to "{width},".
    iiif_image_api::fetch_tile(
        req,
        &adam_fetcher,
        Some(|requested_width, _scale| format!("{requested_width},")),
    ).await
}

pub(crate) async fn download_image(
    req: &DownloadRequest,
    flaresolverr_url: &str,
    base_fetcher: &dyn Fetcher,
) -> Result<Option<TileResponse>, ProviderError> {
    let adam_fetcher = AdamFetcher::new(flaresolverr_url, base_fetcher);
    iiif_image_api::download_image(req, &adam_fetcher).await
}
