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
    // TODO: Maybe switch to IIIF standard and add tiling when the image is still loading from the archive's website

    let path = request.uri().path();
    let parts: Vec<&str> = path.trim_start_matches('/').split('/').collect();

    log::info!("Received tile request: {}", path);

    if parts.len() >= 3 {
        let registry_id = parts[0].parse::<u32>().unwrap_or(0);
        let image_id = parts[1].parse::<u32>().unwrap_or(0);
        let thumbnail = parts[2] == "true";

        match fetch_tiles(
            &state.db,
            &state.plugin_manager,
            registry_id,
            image_id,
            thumbnail,
        )
        .await
        {
            Ok(image_data) => build_tile_response(image_data),
            Err(e) => {
                log::error!("Failed to handle tile request: {}", e);
                Ok(Response::builder()
                    .status(tauri::http::StatusCode::INTERNAL_SERVER_ERROR)
                    .body(vec![])?)
            }
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
    thumbnail: bool,
) -> Result<TileResponse, Error> {
    let (registry, image) =
        image::prepare_image(&db, &plugin_manager, registry_id, image_id).await?;
    Ok(image::fetch_image_tile(db, plugin_manager, registry, image, thumbnail).await?)
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
