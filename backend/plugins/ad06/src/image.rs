use geneagrab_plugin_core::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, ExtractImageResponse, PluginError, TileRequest,
        TileResponse,
    },
    protocols::fetchers::{Fetcher, SimpleFetcher},
};

pub(crate) fn is_image_missing_data(
    req: &ExtractImageRequest,
) -> std::option::Option<std::string::String> {
    if req.image.manifest_url.is_none() {
        Some("manifest_url".to_string())
    } else if req.image.width.is_none() || req.image.height.is_none() {
        Some("dimensions".to_string())
    } else {
        None
    }
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

pub(crate) fn download_image(req: DownloadRequest) -> Result<Option<TileResponse>, PluginError> {
    let manifest_url = req
        .image
        .manifest_url
        .ok_or(PluginError::MissingField("manifest_url".into()))?;

    let iiif_request = format!(
        "{}/full/max/0/default.jpg",
        manifest_url.trim_end_matches('/')
    );

    let fetcher = SimpleFetcher {};
    let data = fetcher.fetch_raw(iiif_request.into())?;

    Ok(Some(TileResponse {
        data,
        mime_type: "image/jpeg".to_string(),
    }))
}
