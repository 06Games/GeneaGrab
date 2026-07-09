use geneagrab_providers::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, ExtractImageResponse, TileRequest,
        TileResponse,
    },
    errors::ProviderError,
    traits::Fetcher,
    protocols::iiif_image_api,
};

#[must_use]
pub fn is_image_missing_data(req: &ExtractImageRequest) -> Option<String> {
    iiif_image_api::is_image_missing_data(req)
}

pub async fn extract_image(
    req: &ExtractImageRequest,
    fetcher: &dyn Fetcher,
) -> Result<ExtractImageResponse, ProviderError> {
    iiif_image_api::extract_image(req, fetcher).await
}

pub async fn fetch_tile(req: &TileRequest, fetcher: &dyn Fetcher) -> Result<TileResponse, ProviderError> {
    // AD06 specific constraint: neither supports ^ and ! modifiers nor percentages.
    // We pass a custom formatter that strictly limits the parameter to "{width},".
    iiif_image_api::fetch_tile(
        req,
        fetcher,
        Some(|requested_width, _scale| format!("{requested_width},")),
    ).await
}

pub async fn download_image(req: &DownloadRequest, fetcher: &dyn Fetcher) -> Result<Option<TileResponse>, ProviderError> {
    iiif_image_api::download_image(req, fetcher).await
}
