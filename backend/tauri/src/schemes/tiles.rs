use std::vec;

use crate::{events::DownloadProgressPayload, state::AppState};
use anyhow::Error;
use geneagrab_core::services::image;
use geneagrab_plugin_core::com_structs::TileResponse;
use tauri::{
    http::{Request, Response, StatusCode},
    AppHandle, Manager,
};

/// Handles the tile request by preparing and returning an image
/// # Errors
/// If the preparation of the image failed
/// If the response couldn't be constructed
pub async fn handle_tile_request(
    request: Request<Vec<u8>>,
    app_handle: AppHandle,
) -> Result<Response<Vec<u8>>, Error> {
    let state = app_handle.state::<AppState>();
    let path = request.uri().path();
    let parts: Vec<&str> = path.trim_start_matches('/').split('/').collect();

    log::info!("Received tile request: {path}");

    if parts.len() >= 2 {
        let registry_id = parts[0].parse::<u32>().unwrap_or(0);
        let image_id = parts[1].parse::<u32>().unwrap_or(0);
        let (registry, image) =
            image::prepare_image(&state.db, &state.plugin_manager, registry_id, image_id).await?;

        let endpoint = match parts.len() {
            2 => {
                let cb = DownloadProgressPayload::download_progress_callback(
                    app_handle.clone(),
                    registry_id,
                    image_id,
                );
                Some(
                    image::download_image(&state.db, &state.plugin_manager, registry, image, cb)
                        .await,
                )
            }
            5 => {
                let level = parts[2].parse::<u32>().unwrap_or(0);
                let x = parts[3].parse::<u32>().unwrap_or(0);
                let y = parts[4].parse::<u32>().unwrap_or(0);

                Some(
                    image::fetch_image_tile(
                        &state.db,
                        &state.plugin_manager,
                        &registry,
                        &image,
                        level,
                        x,
                        y,
                    )
                    .await,
                )
            }
            _ => None,
        };

        match endpoint {
            Some(Ok(image_data)) => build_tile_response(image_data),
            Some(Err(e)) => {
                log::error!("Failed to handle image request: {e}");
                Ok(Response::builder()
                    .status(tauri::http::StatusCode::INTERNAL_SERVER_ERROR)
                    .body(vec![])?)
            }
            None => Ok(Response::builder()
                .status(StatusCode::NOT_FOUND)
                .body(vec![])?),
        }
    } else {
        Ok(Response::builder()
            .status(StatusCode::NOT_FOUND)
            .body(vec![])?)
    }
}

fn build_tile_response(image_data: TileResponse) -> Result<Response<Vec<u8>>, Error> {
    let res = Response::builder()
        .header("Access-Control-Allow-Origin", "*")
        .header("Content-Type", image_data.mime_type)
        .header("Cache-Control", "public, max-age=86400")
        .status(StatusCode::OK)
        .body(image_data.data)?;
    Ok(res)
}
