use tauri::State;
use geneagrab_core::comm_models::{ImageMeta, UserImageMeta};
use crate::state::{AppState, CommandError};

#[tauri::command]
pub async fn get_image_meta(
    registry_id: String,
    image_id: u32,
    state: State<'_, AppState>,
) -> Result<ImageMeta, CommandError> {
    let res = geneagrab_core::services::image::fetch_image_meta(&state.db, registry_id, image_id).await?;
    Ok(res)
}

#[tauri::command]
pub async fn save_image_meta(
    registry_id: String,
    image_id: u32,
    meta: UserImageMeta,
    state: State<'_, AppState>,
) -> Result<(), CommandError> {
    geneagrab_core::services::image::save_image_meta(&state.db, registry_id, image_id, meta).await?;
    Ok(())
}
