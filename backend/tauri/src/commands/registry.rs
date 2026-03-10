use tauri::State;
use geneagrab_core::comm_models::RegistryMeta;
use crate::state::{AppState, CommandError};

#[tauri::command]
pub async fn get_registry_meta(
    id: u32,
    state: State<'_, AppState>,
) -> Result<RegistryMeta, CommandError> {
    let res = geneagrab_core::services::registry::fetch_registry_meta(&state.db, id).await?;
    Ok(res)
}
