#![warn(clippy::pedantic)]

use migration::{Migrator, MigratorTrait};
use sea_orm::{ConnectOptions, Database};
use tauri::Manager;

pub mod commands;
pub mod events;
pub mod schemes;
pub mod state;
pub mod challenges;

use state::AppState;
use tauri_plugin_tracing::LevelFilter;

use crate::schemes::{handler::scheme_handler, tiles};

#[allow(clippy::missing_panics_doc)]
#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let mut builder = tauri::Builder::default();

    #[cfg(desktop)]
    {
        builder = builder.plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            let _ = app
                .get_webview_window("main")
                .expect("no main window")
                .set_focus();
        }));
    }

    builder
        .plugin(tauri_plugin_deep_link::init())
        .plugin(tauri_plugin_opener::init())
        .setup(|app| {
            // Setup logging
            if cfg!(debug_assertions) {
                app.handle().plugin(
                    tauri_plugin_tracing::Builder::new()
                        .with_max_level(LevelFilter::INFO)
                        .with_default_subscriber()
                        .build(),
                )?;
            }

            // Ensure custom schemes are registered
            #[cfg(any(target_os = "linux", all(debug_assertions, windows)))]
            {
                use tauri_plugin_deep_link::DeepLinkExt;
                app.deep_link().register_all()?;
            }

            let app_data_dir = app
                .path()
                .app_data_dir()
                .expect("Failed to get app data dir");
            std::fs::create_dir_all(&app_data_dir).expect("Failed to create app data directory");

            // Construct DB URL
            let db_path = app_data_dir.join("geneagrab.db");
            tracing::info!("Using database at: {}", db_path.to_string_lossy());
            let db_url = format!("sqlite://{}?mode=rwc", db_path.to_string_lossy());

            // Connect to the DB and run migrations
            let db = tauri::async_runtime::block_on(async {
                let mut opt = ConnectOptions::new(db_url);
                opt.sqlx_logging(false);
                let conn = Database::connect(opt)
                    .await
                    .expect("Failed to connect to database");

                // Run all pending migrations automatically on startup
                Migrator::up(&conn, None)
                    .await
                    .expect("Failed to run migrations");

                conn
            });

            app.manage(AppState { db });

            // Initialize the Cloudflare challenge solver callback using Tauri Webviews
            challenges::set_app_handle(app.handle().clone());
            let _ = geneagrab_providers::protocols::fetchers::CHALLENGE_SOLVER.set(challenges::solve_challenge_callback);
            let _ = geneagrab_providers::protocols::fetchers::CAPTCHA_PROMPTER.set(challenges::solve_captcha_callback);

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
            commands::registry::delete_registry,
            commands::registry::get_providers_for_url,
            commands::image::get_image_meta,
            commands::image::save_image_meta,
            commands::image::download_image,
            commands::event::get_event_rows,
            commands::event::get_event_detail,
            commands::event::save_act,
            challenges::register_user_agent,
            challenges::submit_captcha_code
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
