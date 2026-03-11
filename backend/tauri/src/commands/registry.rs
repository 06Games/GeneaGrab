use tauri::State;
use geneagrab_core::comm_models::{CursorPayload, CursorResponse, RegistryFilters, RegistryMeta};
use crate::state::{AppState, CommandError};

#[tauri::command]
pub async fn get_all_registries(
    payload: CursorPayload<RegistryFilters>,
    state: State<'_, AppState>,
) -> Result<CursorResponse<RegistryMeta>, CommandError> {
    let res = geneagrab_core::services::registry::get_all_registries(&state.db, payload).await?;
    Ok(res)
}

#[tauri::command]
pub async fn get_registry(
    id: u32,
    state: State<'_, AppState>,
) -> Result<RegistryMeta, CommandError> {
    let res = geneagrab_core::services::registry::get_registry(&state.db, id).await?;
    Ok(res)
}

#[tauri::command]
pub async fn add_registry(
    url: String,
    state: State<'_, AppState>,
) -> Result<RegistryMeta, CommandError> {
    let res = geneagrab_core::services::registry::add_registry(&state.db, url).await?;
    Ok(res)
}
