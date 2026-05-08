use geneagrab_plugin_core::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, ExtractImageResponse, PluginError, TileRequest,
        TileResponse,
    },
    protocols::fetchers::{Fetcher, SimpleFetcher},
};

pub(crate) fn is_image_missing_data(
    _req: ExtractImageRequest,
) -> Result<Option<String>, PluginError> {
    todo!();
}

fn extract_image_internal(
    _req: &ExtractImageRequest,
    _fetcher: &impl Fetcher,
) -> Result<ExtractImageResponse, PluginError> {
    todo!();
}
pub(crate) fn extract_image(
    req: &ExtractImageRequest,
) -> Result<ExtractImageResponse, PluginError> {
    extract_image_internal(req, &SimpleFetcher)
}

fn fetch_tile_internal(
    _req: &TileRequest,
    _fetcher: &impl Fetcher,
) -> Result<TileResponse, PluginError> {
    todo!();
}

pub(crate) fn fetch_tile(req: &TileRequest) -> Result<TileResponse, PluginError> {
    fetch_tile_internal(req, &SimpleFetcher)
}

#[allow(clippy::unnecessary_wraps, reason = "WIP")]
pub(crate) fn download_image(_req: DownloadRequest) -> Result<Option<TileResponse>, PluginError> {
    Ok(None) // Sometimes, the images are freely downloadable. We might want to check for that in the future.
}
