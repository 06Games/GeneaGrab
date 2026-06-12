use geneagrab_plugin_core::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, ExtractImageResponse, PluginError, TileRequest,
        TileResponse,
    },
    protocols::iiif::image_api,
};

use crate::fetcher::AdamFetcher;

pub(crate) fn is_image_missing_data(req: &ExtractImageRequest) -> Option<String> {
    image_api::is_image_missing_data(req)
}

pub(crate) fn extract_image(
    req: &ExtractImageRequest,
) -> Result<ExtractImageResponse, PluginError> {
    image_api::extract_image(req, &AdamFetcher::default())
}

pub(crate) fn fetch_tile(req: &TileRequest) -> Result<TileResponse, PluginError> {
    // AD06 specific constraint: neither supports ^ and ! modifiers nor percentages.
    // We pass a custom formatter that strictly limits the parameter to "{width},".
    image_api::fetch_tile(
        req,
        &AdamFetcher::default(),
        Some(|requested_width, _scale| format!("{requested_width},")),
    )
}

pub(crate) fn download_image(req: &DownloadRequest) -> Result<Option<TileResponse>, PluginError> {
    image_api::download_image(req, &AdamFetcher::default())
}
