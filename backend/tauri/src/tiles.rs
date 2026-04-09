use std::vec;

use crate::state::AppState;
use anyhow::Error;
use geneagrab_core::{plugins::PluginManager, services::image};
use geneagrab_plugin_core::com_structs::TileResponse;
use sea_orm::DbConn;
use tauri::{
    http::{Request, Response, StatusCode},
    State,
};

pub async fn handle_tile_request(
    request: Request<Vec<u8>>,
    state: State<'_, AppState>,
) -> Result<Response<Vec<u8>>, Error> {
    let path = request.uri().path();
    let parts: Vec<&str> = path.trim_start_matches('/').split('/').collect();

    log::info!("Received tile request: {path}");

    if parts.len() >= 2 {
        let registry_id = parts[0].parse::<u32>().unwrap_or(0);
        let image_id = parts[1].parse::<u32>().unwrap_or(0);

        let endpoint = match parts.len() {
            2 => {
                Some(download_image(&state.db, &state.plugin_manager, registry_id, image_id).await)
            }
            5 => {
                let zoom = parts[2].parse::<u32>().unwrap_or(0);
                let x = parts[3].parse::<u32>().unwrap_or(0);
                let y = parts[4].parse::<u32>().unwrap_or(0);

                Some(
                    fetch_tiles(
                        &state.db,
                        &state.plugin_manager,
                        registry_id,
                        image_id,
                        zoom,
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

async fn fetch_tiles(
    db: &DbConn,
    plugin_manager: &PluginManager,
    registry_id: u32,
    image_id: u32,
    level: u32,
    x: u32,
    y: u32,
) -> Result<TileResponse, Error> {
    let (registry, image) = image::prepare_image(db, plugin_manager, registry_id, image_id).await?;
    Ok(image::fetch_image_tile(db, plugin_manager, &registry, &image, level, x, y).await?)
}

async fn download_image(
    db: &DbConn,
    plugin_manager: &PluginManager,
    registry_id: u32,
    image_id: u32,
) -> Result<TileResponse, Error> {
    let (registry, image) = image::prepare_image(db, plugin_manager, registry_id, image_id).await?;
    Ok(image::download_image(db, plugin_manager, registry, image).await?)
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
