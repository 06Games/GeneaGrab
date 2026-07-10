use crate::state::{AppState, CommandError};
use geneagrab_core::comm_models::{CursorPayload, CursorResponse, RegistryFilters, RegistryMeta};
use geneagrab_providers::data::PluginMetadata;
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
    let res = geneagrab_core::services::registry::get_registry_meta(&state.db, id).await?;
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
        url,
        plugin_id,
    )
    .await?;
    Ok(res)
}

#[tauri::command]
pub async fn get_plugins_for_url(
    url: String,
) -> Result<Vec<PluginMetadata>, CommandError> {
    let res = geneagrab_core::services::registry::get_plugins_for_url(&url)
        .await?;

    Ok(res.into_iter().map(|(meta, _)| meta).collect()) // TODO: Keep the extracted info
}
