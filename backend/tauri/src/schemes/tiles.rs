use crate::{events::DownloadProgressPayload, state::AppState};
use anyhow::{anyhow, Error};
use geneagrab_core::services::image;
use geneagrab_providers::com_structs::TileResponse;
use tauri::{
    http::{Request, Response, StatusCode},
    AppHandle, Manager,
};

/// Handles tile and image requests from the custom `tiles://` URI scheme.
///
/// Supported routes:
/// - `/{registry_id}/{image_id}` -> download full image
/// - `/{registry_id}/{image_id}/{level}/{x}/{y}` -> fetch image tile
///
/// # Errors
///
/// Returns an error if registry/image identifiers are invalid, database lookup or image
/// preparation fails, or if constructing the HTTP response fails.
pub async fn handle_tile_request(
    request: Request<Vec<u8>>,
    app_handle: AppHandle,
) -> Result<Response<Vec<u8>>, Error> {
    let state = app_handle.state::<AppState>();
    let path = request.uri().path();
    let parts: Vec<&str> = path.trim_start_matches('/').split('/').collect();

    tracing::info!("Received tile request: {path}");

    if parts.len() < 2 {
        return Ok(Response::builder()
            .status(StatusCode::NOT_FOUND)
            .body(Vec::new())?);
    }

    let registry_id = parts[0].parse::<u32>().map_err(|_| anyhow!("Invalid registry ID"))?;
    let image_id = parts[1].parse::<u32>().map_err(|_| anyhow!("Invalid image ID"))?;

    let (registry, image) = image::prepare_image(&state.db, registry_id, image_id).await?;

    let tile_result = match parts.len() {
        2 => {
            let cb = DownloadProgressPayload::download_progress_callback(
                app_handle.clone(),
                registry_id,
                image_id,
            );
            image::download_image(&state.db, registry, image, cb).await
        }
        5 => {
            let level = parts[2].parse::<u32>().map_err(|_| anyhow!("Invalid level"))?;
            let x = parts[3].parse::<u32>().map_err(|_| anyhow!("Invalid tile x"))?;
            let y = parts[4].parse::<u32>().map_err(|_| anyhow!("Invalid tile y"))?;

            image::fetch_image_tile(&state.db, &registry, &image, level, x, y).await
        }
        _ => {
            return Ok(Response::builder()
                .status(StatusCode::NOT_FOUND)
                .body(Vec::new())?);
        }
    };

    match tile_result {
        Ok(image_data) => build_tile_response(image_data),
        Err(e) => {
            tracing::error!("Failed to handle image request for {path}: {e}");
            Ok(Response::builder()
                .status(StatusCode::INTERNAL_SERVER_ERROR)
                .body(Vec::new())?)
        }
    }
}

fn build_tile_response(image_data: TileResponse) -> Result<Response<Vec<u8>>, Error> {
    Ok(Response::builder()
        .header("Content-Type", image_data.mime_type)
        .header("Cache-Control", "public, max-age=86400")
        .status(StatusCode::OK)
        .body(image_data.data)?)
}
