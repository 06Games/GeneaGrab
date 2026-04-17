#![warn(clippy::pedantic)]

use geneagrab_core::{plugins::PluginManager, services::plugin};
use migration::{Migrator, MigratorTrait};
use sea_orm::Database;
use tauri::{Manager, State};

pub mod commands;
pub mod events;
pub mod schemes;
pub mod state;

use state::AppState;

use crate::schemes::{handler::scheme_handler, tiles};

#[allow(clippy::missing_panics_doc)]
#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .setup(|app| {
            // Setup logging
            if cfg!(debug_assertions) {
                app.handle().plugin(
                    tauri_plugin_log::Builder::default()
                        .targets([
                            tauri_plugin_log::Target::new(tauri_plugin_log::TargetKind::Stdout),
                            tauri_plugin_log::Target::new(tauri_plugin_log::TargetKind::Webview),
                        ])
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

            app.manage(AppState {
                db,
                plugin_manager: PluginManager::default(),
            });

            let app_handle = app.handle().clone();
            let plugins_dir = app_data_dir.join("plugins").clone();
            tauri::async_runtime::spawn(async move {
                let state: State<'_, AppState> = app_handle.state();
                let plugins = plugin::scan_plugins_dir(&plugins_dir)
                    .await
                    .expect("Failed to scan plugins directory");
                for plugin_path in plugins {
                    match plugin::register_plugin(
                        &state.db,
                        state.plugin_manager.clone(),
                        plugin_path.clone(),
                    )
                    .await
                    {
                        Ok(()) => {
                            log::info!("Successfully registered plugin: {}", plugin_path.display());
                        }
                        Err(e) => log::error!(
                            "Failed to register plugin {}: {}",
                            plugin_path.display(),
                            e
                        ),
                    }
                }
            });

            Ok(())
        })
        .register_asynchronous_uri_scheme_protocol(
            "tiles",
            scheme_handler(tiles::handle_tile_request),
        )
        // Register all IPC commands
        .invoke_handler(tauri::generate_handler![
            commands::registry::get_all_registries,
            commands::registry::get_registry,
            commands::registry::add_registry,
            commands::registry::get_plugins_for_url,
            commands::image::get_image_meta,
            commands::image::save_image_meta,
            commands::image::download_image,
            commands::event::get_event_rows,
            commands::event::get_event_detail,
            commands::event::save_act
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");

    // TODO: Handle geneagrab:// links
}
