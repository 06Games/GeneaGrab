use geneagrab_plugin_core::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, ExtractImageResponse, PluginError, TileRequest,
        TileResponse,
    },
    protocols::fetchers::{Fetcher, SimpleFetcher},
    utils::ImageGeometry,
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
    req: &ExtractImageRequest,
    fetcher: &impl Fetcher,
) -> Result<ExtractImageResponse, PluginError> {
    let mut image = req.image.clone();

    let manifest_url = image
        .manifest_url
        .as_ref()
        .ok_or_else(|| PluginError::MissingField("manifest_url".into()))?;

    let info_url = format!("{}/info.json", manifest_url.trim_end_matches('/'));
    let info_json = fetcher.fetch(info_url.into())?;

    let info: serde_json::Value = serde_json::from_str(&info_json)
        .map_err(|e| PluginError::ParsingError(format!("Failed to parse info.json: {e}")))?;

    if image.width.is_none() {
        image.width = info
            .get("width")
            .and_then(serde_json::Value::as_u64)
            .and_then(|v| u32::try_from(v).ok());
    }

    if image.height.is_none() {
        image.height = info
            .get("height")
            .and_then(serde_json::Value::as_u64)
            .and_then(|v| u32::try_from(v).ok());
    }

    if image.tile_size.is_none() {
        if let Some(tiles) = info.get("tiles").and_then(|t| t.as_array()) {
            if let Some(first_tile) = tiles.first() {
                image.tile_size = first_tile
                    .get("width")
                    .and_then(serde_json::Value::as_u64)
                    .and_then(|v| u32::try_from(v).ok());
            }
        }

        if image.tile_size.is_none() {
            image.tile_size = Some(512);
        }
    }

    Ok(ExtractImageResponse { image })
}

pub(crate) fn extract_image(
    req: &ExtractImageRequest,
) -> Result<ExtractImageResponse, PluginError> {
    extract_image_internal(req, &SimpleFetcher)
}

fn fetch_tile_internal(
    req: &TileRequest,
    fetcher: &impl Fetcher,
) -> Result<TileResponse, PluginError> {
    let manifest_url = req
        .image
        .manifest_url
        .as_ref()
        .ok_or_else(|| PluginError::MissingField("manifest_url".into()))?;

    let width = req
        .image
        .width
        .ok_or_else(|| PluginError::MissingField("width".into()))?;
    let height = req
        .image
        .height
        .ok_or_else(|| PluginError::MissingField("height".into()))?;

    let tile_size = req.image.tile_size.unwrap_or(512);

    let geometry = ImageGeometry::new(width, height, tile_size);

    let max_level = geometry.max_level();
    let scale_power = max_level.saturating_sub(req.zoom);
    let scale = 1_u32.checked_shl(scale_power).unwrap_or(1);

    let scaled_tile_size = tile_size * scale;

    let x = req.x * scaled_tile_size;
    let y = req.y * scaled_tile_size;

    let region_w = scaled_tile_size.min(width.saturating_sub(x));
    let region_h = scaled_tile_size.min(height.saturating_sub(y));

    let region = format!("{x},{y},{region_w},{region_h}");

    // AD06 specific constraint: neither supports ^ and ! modifiers nor percentages.
    let mut requested_width = region_w / scale;
    if requested_width == 0 {
        requested_width = 1;
    }
    let size = requested_width.to_string();

    let url = format!(
        "{}/{}/{}/0/default.jpg",
        manifest_url.trim_end_matches('/'),
        region,
        size
    );

    let data = fetcher.fetch_raw(url.into())?;

    Ok(TileResponse {
        data,
        mime_type: "image/jpeg".to_string(),
    })
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
