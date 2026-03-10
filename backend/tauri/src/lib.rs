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
            
            let app_data_dir = app.path().app_data_dir().expect("Failed to get app data dir");
            std::fs::create_dir_all(&app_data_dir).expect("Failed to create app data directory");
            
            // Construct DB URL
            let db_path = app_data_dir.join("geneagrab.db");
            log::info!("Using database at: {}", db_path.to_string_lossy());
            let db_url = format!("sqlite://{}?mode=rwc", db_path.to_string_lossy());

            // Connect to the DB and run migrations
            let db = tauri::async_runtime::block_on(async {
                let conn = Database::connect(&db_url).await.expect("Failed to connect to database");
                
                // Run all pending migrations automatically on startup
                Migrator::up(&conn, None).await.expect("Failed to run migrations");
                
                conn
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
