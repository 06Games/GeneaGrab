use crate::events::TauriEvent;
use serde::Serialize;
use tauri::{Emitter, Runtime};

#[derive(Serialize, Clone)]
pub struct DownloadProgressPayload {
    pub registry_id: u32,
    pub image_id: u32,
    pub current: u32,
    pub total: u32,
}

impl TauriEvent for DownloadProgressPayload {
    const EVENT_NAME: &'static str = "download-progress";
}

impl DownloadProgressPayload {
    pub fn download_progress_callback<R: Runtime>(
        app_handle: impl Emitter<R>,
        registry_id: u32,
        image_id: u32,
    ) -> impl Fn(u32, u32) {
        move |current: u32, total: u32| {
            DownloadProgressPayload {
                registry_id,
                image_id,
                current,
                total,
            }
            .emit(&app_handle)
            .unwrap_or_else(|e| log::error!("Failed to emit progress: {e}"));
        }
    }
}
