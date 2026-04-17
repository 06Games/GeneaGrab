mod download_progress;

pub use download_progress::DownloadProgressPayload;

use serde::Serialize;
use tauri::{Emitter, Runtime};

pub trait TauriEvent: Serialize {
    const EVENT_NAME: &'static str;

    /// Emits the event via `tauri::AppHandle`.
    ///
    /// # Errors
    ///
    /// Returns an error if the data cannot be serialized or the IPC call fails.
    fn emit<R: Runtime>(&self, app_handle: &impl Emitter<R>) -> Result<(), tauri::Error> {
        app_handle.emit(Self::EVENT_NAME, self)
    }
}
