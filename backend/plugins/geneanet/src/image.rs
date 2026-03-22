use extism_pdk::Error;
use geneagrab_plugin_core::{
    com_structs::{ExtractImageRequest, ExtractImageResponse, TileRequest, TileResponse},
    protocols::{utils::fetch_string, zoomify::Zoomify},
};

pub(crate) fn extract_image(req: ExtractImageRequest) -> Result<ExtractImageResponse, Error> {
    if req.image.tile_size.is_some() {
        return Ok(ExtractImageResponse { image: req.image });
    }

    let mut image = req.image.clone();

    let base_url = image
        .manifest_url
        .clone()
        .ok_or(Error::msg("Missing manifest URL"))?;

    let zoomify = Zoomify::fetch(&base_url, fetch_string)?;

    image.width = Some(zoomify.width);
    image.height = Some(zoomify.height);
    image.tile_size = Some(zoomify.tile_size);

    Ok(ExtractImageResponse { image })
}

pub(crate) fn generate_tile_request(_req: TileRequest) -> Result<TileResponse, Error> {
    todo!()

    /*
        var stream = await Data.TryGetImageFromDrive(page, scale);
    if (stream != null) return stream;

    progress?.Invoke(Progress.Unknown);

    if (!page.TileSize.HasValue)
        (page.Width, page.Height, page.TileSize) = await Zoomify.ImageData(page.DownloadUrl, HttpClient);
    var maxZoom = Zoomify.CalculateIndex(page);
    var scaleZoom = maxZoom * scale switch
    {
        Scale.Thumbnail => 0,
        Scale.Navigation => 0.75,
        _ => 1
    };
    var zoom = Math.Min((int)Math.Ceiling(scaleZoom), maxZoom);
    var (tiles, diviser) = Zoomify.GetTilesNumber(page, zoom);

    progress?.Invoke(0);
    Image image = new Image<Rgb24>(page.Width!.Value / diviser, page.Height!.Value / diviser);
    var tasks = new Dictionary<Task<Image>, (int tileSize, int scale, Point pos)>();
    for (var y = 0; y < tiles.Y; y++)
    for (var x = 0; x < tiles.X; x++)
        tasks.Add(Grabber.GetImage($"{page.DownloadUrl}TileGroup0/{zoom}-{x}-{y}.jpg", HttpClient)
            .ContinueWith(task =>
            {
                progress?.Invoke(tasks.Keys.Count(t => t.IsCompleted) / (float)tasks.Count);
                return task.Result;
            }), (page.TileSize.GetValueOrDefault(), diviser, new Point(x, y)));

    await Task.WhenAll(tasks.Keys).ConfigureAwait(false);
    image = tasks.Aggregate(image, (current, tile) => current.MergeTile(tile.Key.Result, tile.Value));
    page.ImageSize = scale;
    progress?.Invoke(Progress.Finished);

    await Data.SaveImage(page, image, false).ConfigureAwait(false);
    return image.ToStream();
     */
}
