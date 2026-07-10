use geneagrab_providers::{
    com_structs::{
        ExtractImageRequest, ExtractImageResponse, TileRequest,
        TileResponse,
    },
    errors::ProviderError,
    protocols::zoomify::Zoomify,
    traits::Fetcher,
};

pub(crate) fn is_image_missing_data(
    req: &ExtractImageRequest,
) -> Result<Option<String>, ProviderError> {
    match Zoomify::try_from(req.image.clone()) {
        Ok(_) => Ok(None),
        Err(ProviderError::MissingField(field)) => Ok(Some(field)),
        Err(e) => Err(e),
    }
}

async fn extract_image_internal(
    req: &ExtractImageRequest,
    fetcher: &dyn Fetcher,
) -> Result<ExtractImageResponse, ProviderError> {
    let mut image = req.image.clone();

    let base_url = image
        .manifest_url
        .clone()
        .ok_or_else(|| ProviderError::MissingField("Manifest URL".into()))?;

    let zoomify = Zoomify::fetch(&base_url, fetcher).await?;

    image.width = Some(zoomify.geometry.width);
    image.height = Some(zoomify.geometry.height);
    image.tile_size = Some(zoomify.geometry.tile_size);

    Ok(ExtractImageResponse { image })
}

pub(crate) async fn extract_image(
    req: &ExtractImageRequest,
    flaresolverr_url: &str,
    base_fetcher: &dyn Fetcher,
) -> Result<ExtractImageResponse, ProviderError> {
    let fs_fetcher = geneagrab_providers::protocols::fetchers::CachedFlareSolverrFetcher::new(
        flaresolverr_url,
        base_fetcher,
    );
    extract_image_internal(req, &fs_fetcher).await
}

async fn fetch_tile_internal(
    req: &TileRequest,
    fetcher: &dyn Fetcher,
) -> Result<TileResponse, ProviderError> {
    let zoomify: Zoomify = req.image.clone().try_into()?;
    let tile_url = zoomify.tile_url(req.zoom, req.x, req.y)?;
    let tile_data = fetcher.fetch_raw(tile_url.into()).await?;

    Ok(TileResponse {
        data: tile_data,
        mime_type: "image/jpeg".into(),
    })
}

pub(crate) async fn fetch_tile(
    req: &TileRequest,
    flaresolverr_url: &str,
    base_fetcher: &dyn Fetcher,
) -> Result<TileResponse, ProviderError> {
    let fs_fetcher = geneagrab_providers::protocols::fetchers::CachedFlareSolverrFetcher::new(
        flaresolverr_url,
        base_fetcher,
    );
    fetch_tile_internal(req, &fs_fetcher).await
}

#[allow(clippy::unnecessary_wraps, reason = "WIP")]
pub(crate) fn download_image() -> Result<Option<TileResponse>, ProviderError> {
    Ok(None)
}
