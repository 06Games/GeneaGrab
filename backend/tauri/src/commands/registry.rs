use crate::state::{AppState, CommandError};
use geneagrab_core::comm_models::{CursorPayload, CursorResponse, RegistryFilters, RegistryMeta};
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
    provider_id: String,
    state: State<'_, AppState>,
) -> Result<RegistryMeta, CommandError> {
    let res = geneagrab_core::services::registry::add_registry(
        &state.db,
        url,
        provider_id,
    )
    .await?;
    Ok(res)
}

#[derive(serde::Serialize)]
pub struct ProviderOptionResponse {
    pub id: String,
    pub name: String,
    pub registry_id: String,
    pub image_number: Option<u32>,
}

#[tauri::command]
pub async fn get_providers_for_url(
    url: String,
) -> Result<Vec<ProviderOptionResponse>, CommandError> {
    let res = geneagrab_core::services::registry::get_providers_for_url(&url)
        .await?;

    Ok(res
        .into_iter()
        .map(|(meta, identified)| ProviderOptionResponse {
            id: meta.id.to_string(),
            name: meta.name.to_string(),
            registry_id: identified.registry_id,
            image_number: identified.image_number,
        })
        .collect())
}

#[tauri::command]
pub async fn delete_registry(
    id: u32,
    state: State<'_, AppState>,
) -> Result<(), CommandError> {
    geneagrab_core::services::registry::delete_registry(&state.db, id).await?;
    Ok(())
}

