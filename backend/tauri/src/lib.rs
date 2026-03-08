use geneagrab_core::{
    errors::CoreError,
    models::{EventDetail, EventRow, ImageMeta, RegistryMeta, UserImageMeta},
};
use serde::Serialize;

#[derive(Serialize)]
pub struct CommandError(String);

impl From<CoreError> for CommandError {
    fn from(err: CoreError) -> Self {
        CommandError(err.to_string())
    }
}

#[tauri::command]
async fn get_registry_meta(id: String) -> Result<RegistryMeta, CommandError> {
    let res = geneagrab_core::services::fetch_registry_meta(id).await?;
    Ok(res)
}

#[tauri::command]
async fn get_image_meta(registry_id: String, image_id: u32) -> Result<ImageMeta, CommandError> {
    let res = geneagrab_core::services::fetch_image_meta(registry_id, image_id).await?;
    Ok(res)
}

#[tauri::command]
async fn save_image_meta(
    registry_id: String,
    image_id: u32,
    meta: UserImageMeta,
) -> Result<(), CommandError> {
    geneagrab_core::services::save_image_meta(registry_id, image_id, meta).await?;
    Ok(())
}

#[tauri::command]
async fn get_event_rows(registry_id: String) -> Result<Vec<EventRow>, CommandError> {
    let res = geneagrab_core::services::fetch_event_rows(registry_id).await?;
    Ok(res)
}

#[tauri::command]
async fn get_event_detail(event_id: u32) -> Result<Option<EventDetail>, CommandError> {
    let res = geneagrab_core::services::fetch_event(event_id).await?;
    Ok(Some(res))
}

#[tauri::command]
async fn save_act(event: EventDetail) -> Result<(), CommandError> {
    geneagrab_core::services::save_act(event).await?;
    Ok(())
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .setup(|app| {
            if cfg!(debug_assertions) {
                app.handle().plugin(
                    tauri_plugin_log::Builder::default()
                        .level(log::LevelFilter::Info)
                        .build(),
                )?;
            }
            Ok(())
        })
        // Register all IPC commands
        .invoke_handler(tauri::generate_handler![
            get_registry_meta,
            get_image_meta,
            save_image_meta,
            get_event_rows,
            get_event_detail,
            save_act
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
