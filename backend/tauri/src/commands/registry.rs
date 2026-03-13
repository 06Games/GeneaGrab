use crate::state::{AppState, CommandError};
use geneagrab_core::comm_models::{CursorPayload, CursorResponse, RegistryFilters, RegistryMeta};
use geneagrab_plugin_core::data::PluginMetadata;
use tauri::State;

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
    plugin_id: String,
    state: State<'_, AppState>,
) -> Result<RegistryMeta, CommandError> {
    let res = geneagrab_core::services::registry::add_registry(
        &state.db,
        &state.plugin_manager,
        url,
        plugin_id,
    )
    .await?;
    Ok(res)
}

#[tauri::command]
pub async fn get_plugins_for_url(
    _url: String,
    state: State<'_, AppState>,
) -> Result<Vec<PluginMetadata>, CommandError> {
    let manager = state.plugin_manager.lock().unwrap();
    // TODO: Implement plugin discovery logic based on the URL
    Ok(manager.list_plugins())
}
