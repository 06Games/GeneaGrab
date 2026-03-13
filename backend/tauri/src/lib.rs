use std::sync::Arc;

use geneagrab_core::plugins::PluginManager;
use migration::{Migrator, MigratorTrait};
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

            let app_data_dir = app
                .path()
                .app_data_dir()
                .expect("Failed to get app data dir");
            std::fs::create_dir_all(&app_data_dir).expect("Failed to create app data directory");

            // Construct DB URL
            let db_path = app_data_dir.join("geneagrab.db");
            log::info!("Using database at: {}", db_path.to_string_lossy());
            let db_url = format!("sqlite://{}?mode=rwc", db_path.to_string_lossy());

            // Connect to the DB and run migrations
            let db = tauri::async_runtime::block_on(async {
                let conn = Database::connect(&db_url)
                    .await
                    .expect("Failed to connect to database");

                // Run all pending migrations automatically on startup
                Migrator::up(&conn, None)
                    .await
                    .expect("Failed to run migrations");

                conn
            });

            let mut plugin_manager = PluginManager::new();
            let plugins_dir = app_data_dir.join("plugins");

            if plugins_dir.exists() {
                if let Ok(entries) = std::fs::read_dir(&plugins_dir) {
                    for entry in entries.flatten() {
                        let path = entry.path();
                        if path.extension().and_then(|e| e.to_str()) == Some("wasm") {
                            let plugin_id = path.file_stem().unwrap().to_string_lossy().to_string();
                            if let Ok(wasm_bytes) = std::fs::read(&path) {
                                match plugin_manager.register_plugin(plugin_id, wasm_bytes) {
                                    Ok(()) => {
                                        log::info!("Registered plugin from {}", path.display())
                                    }
                                    Err(plugin_error) => log::error!(
                                        "Failed to register plugin from {}: {}",
                                        path.display(),
                                        plugin_error
                                    ),
                                }
                            }
                        }
                    }
                }
            } else {
                let _ = std::fs::create_dir_all(&plugins_dir);
            }

            app.manage(AppState {
                db,
                plugin_manager: Arc::new(plugin_manager),
            });

            Ok(())
        })
        .register_uri_scheme_protocol("tiles", |ctx, request| {
            let app_handle = ctx.app_handle();
            let state = app_handle.state::<state::AppState>();
            tiles::handle_tile_request(request, state)
        })
        // Register all IPC commands
        .invoke_handler(tauri::generate_handler![
            commands::registry::get_all_registries,
            commands::registry::get_registry,
            commands::registry::add_registry,
            commands::registry::get_plugins_for_url,
            commands::image::get_image_meta,
            commands::image::save_image_meta,
            commands::event::get_event_rows,
            commands::event::get_event_detail,
            commands::event::save_act
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
