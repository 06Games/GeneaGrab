use geneagrab_plugin_core::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, ExtractImageResponse, PluginError, TileRequest,
        TileResponse,
    },
    protocols::{
        fetchers::{Fetcher, FlareSolverrFetcher},
        zoomify::Zoomify,
    },
};

pub(crate) fn is_image_missing_data(
    req: ExtractImageRequest,
) -> Result<Option<String>, PluginError> {
    match Zoomify::try_from(req.image) {
        Ok(_) => Ok(None),
        Err(PluginError::MissingField(field)) => Ok(Some(field)),
        Err(e) => Err(e),
    }
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

fn fetch_tile_internal(
    req: TileRequest,
    fetcher: impl Fetcher,
) -> Result<TileResponse, PluginError> {
    let zoomify: Zoomify = req.image.clone().try_into()?;
    let tile_url = zoomify.tile_url(req.zoom, req.x, req.y)?;
    let tile_data = fetcher.fetch_raw(tile_url.into())?;

    Ok(TileResponse {
        data: tile_data,
        mime_type: "image/jpeg".into(),
    })
}

pub(crate) fn fetch_tile(req: TileRequest) -> Result<TileResponse, PluginError> {
    // Tile API is now under Cloudflare protection
    fetch_tile_internal(req, FlareSolverrFetcher::from_config()?)
}

pub(crate) fn download_image(_req: DownloadRequest) -> Result<Option<TileResponse>, PluginError> {
    Ok(None) // Sometimes, the images are freely downloadable. We might want to check for that in the future.
}
