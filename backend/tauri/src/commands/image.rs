use std::fs;

use crate::{
    events::DownloadProgressPayload,
    state::{AppState, CommandError},
};
use geneagrab_core::{
    comm_models::{ImageMeta, UserImageMeta},
    services::image,
};
use tauri::{AppHandle, Manager, State};
use tauri_plugin_opener::OpenerExt;

#[tauri::command]
pub async fn get_image_meta(
    registry_id: u32,
    image_id: u32,
    state: State<'_, AppState>,
) -> Result<ImageMeta, CommandError> {
    let res =
        geneagrab_core::services::image::fetch_image_meta(&state.db, registry_id, image_id).await?;
    Ok(res)
}

#[tauri::command]
pub async fn save_image_meta(
    registry_id: u32,
    image_id: u32,
    meta: UserImageMeta,
    state: State<'_, AppState>,
) -> Result<(), CommandError> {
    geneagrab_core::services::image::save_image_meta(&state.db, registry_id, image_id, meta)
        .await?;
    Ok(())
}

#[tauri::command]
pub async fn download_image(
    registry_id: u32,
    image_id: u32,
    state: State<'_, AppState>,
    app_handle: AppHandle,
) -> Result<(), CommandError> {
    let cb = DownloadProgressPayload::download_progress_callback(
        app_handle.clone(),
        registry_id,
        image_id,
    );
    let (registry, image) =
        image::prepare_image(&state.db, registry_id, image_id).await?;
    let reference = registry
        .archive_reference
        .clone()
        .unwrap_or_else(|| registry.registry_id.clone());
    let safe_reference = reference
        .chars()
        .map(|c| match c {
            '/' | '\\' | ':' | '*' | '?' | '"' | '<' | '>' | '|' => '_',
            _ => c,
        })
        .collect::<String>();
    let filename = format!("geneagrab--{safe_reference}--{image_id}.jpg");
    let image =
        image::download_image(&state.db, registry, image, cb).await?;

    let download_dir = app_handle.path().download_dir()?;
    if !download_dir.exists() {
        fs::create_dir_all(&download_dir)?;
    }
    let file_path = download_dir.join(filename);
    fs::write(&file_path, image.data)?;
    app_handle
        .opener()
        .reveal_item_in_dir(file_path)
        .map_err(|e| CommandError(e.to_string()))?;
    Ok(())
}
