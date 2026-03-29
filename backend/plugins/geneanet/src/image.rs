use geneagrab_plugin_core::{
    com_structs::{
        ExtractImageRequest, ExtractImageResponse, PluginError, TileRequest, TileResponse,
    },
    protocols::{
        fetchers::{Fetcher, FlareSolverrFetcher},
        zoomify::Zoomify,
    },
};

pub(crate) fn is_image_missing_data(req: ExtractImageRequest) -> Result<(), PluginError> {
    Zoomify::try_from(req.image)?;
    Ok(())
}

fn extract_image_internal(
    req: ExtractImageRequest,
    fetcher: impl Fetcher,
) -> Result<ExtractImageResponse, PluginError> {
    let mut image = req.image.clone();

    let base_url = image
        .manifest_url
        .clone()
        .ok_or(PluginError::MissingField("Manifest URL".into()))?;

    let zoomify = Zoomify::fetch(&base_url, fetcher)?;

    image.width = Some(zoomify.width);
    image.height = Some(zoomify.height);
    image.tile_size = Some(zoomify.tile_size);

    Ok(ExtractImageResponse { image })
}
pub(crate) fn extract_image(req: ExtractImageRequest) -> Result<ExtractImageResponse, PluginError> {
    // Image API is now under Cloudflare protection
    extract_image_internal(req, FlareSolverrFetcher::from_config()?)
}

pub(crate) fn generate_tile_request(req: TileRequest) -> Result<TileResponse, PluginError> {
    let zoomify: Zoomify = req.clone().into();

    Ok(TileResponse {
        url: zoomify.tile_url(req.zoom, req.x, req.y)?,
        headers: None,
    })
}
