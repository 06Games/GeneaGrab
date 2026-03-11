use tauri::State;
use geneagrab_core::comm_models::{EventDetail, EventRow};
use crate::state::{AppState, CommandError};

#[tauri::command]
pub async fn get_event_rows(
    registry_id: u32,
    state: State<'_, AppState>,
) -> Result<Vec<EventRow>, CommandError> {
    let res = geneagrab_core::services::event::fetch_event_rows(&state.db, registry_id).await?;
    Ok(res)
}

#[tauri::command]
pub async fn get_event_detail(
    event_id: u32,
    state: State<'_, AppState>,
) -> Result<Option<EventDetail>, CommandError> {
    let res = geneagrab_core::services::event::fetch_event(&state.db, event_id).await?;
    Ok(Some(res))
}

#[tauri::command]
pub async fn save_act(
    event: EventDetail,
    state: State<'_, AppState>,
) -> Result<(), CommandError> {
    geneagrab_core::services::event::save_act(&state.db, event).await?;
    Ok(())
}
