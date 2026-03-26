use geneagrab_plugin_core::{
    com_structs::{
        ExtractImageRequest, ExtractImageResponse, PluginError, TileRequest, TileResponse,
    },
    protocols::{
        utils::{fetch_string, Fetch},
        zoomify::Zoomify,
    },
};

fn extract_image_internal(
    req: ExtractImageRequest,
    fetcher: impl Fetch,
) -> Result<ExtractImageResponse, PluginError> {
    if req.image.tile_size.is_some() {
        return Ok(ExtractImageResponse { image: req.image });
    }

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
    extract_image_internal(req, fetch_string)
}

pub(crate) fn generate_tile_request(req: TileRequest) -> Result<TileResponse, PluginError> {
    let zoomify: Zoomify = req.clone().into();

    Ok(TileResponse {
        url: zoomify.tile_url(req.zoom, req.x, req.y)?,
        headers: None,
    })
}
