use sea_orm::Database;
use tauri::Manager;

pub mod commands; 
pub mod state;   
mod tiles; 

use state::AppState;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .setup(|app| {
            // Setup logging
            if cfg!(debug_assertions) {
                app.handle().plugin(
                    tauri_plugin_log::Builder::default()
                        .level(log::LevelFilter::Info)
                        .build(),
                )?;
            }

            let db_url = "sqlite:///home/evan/.local/share/GeneaGrab/geneagrab.db?mode=rwc"; // TODO: Use dynamic app data dir
            // Wait for the database connection to be established before running the app
            let db = tauri::async_runtime::block_on(async {
                Database::connect(db_url).await.expect("Failed to connect to database")
            });
            app.manage(AppState { db }); // Inject the DbConn into managed state

            Ok(())
        })
        .register_uri_scheme_protocol("tiles", |ctx, request| {
            let app_handle = ctx.app_handle();
            let state = app_handle.state::<state::AppState>();
            tiles::handle_tile_request(request, state)
        })

        // Register all IPC commands
        .invoke_handler(tauri::generate_handler![
            commands::registry::get_registry_meta,
            commands::image::get_image_meta,
            commands::image::save_image_meta,
            commands::event::get_event_rows,
            commands::event::get_event_detail,
            commands::event::save_act
        ])
        
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
