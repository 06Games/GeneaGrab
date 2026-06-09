use std::{collections::HashMap, path::PathBuf};

use sea_orm::{ColumnTrait, DbConn, EntityTrait, QueryFilter, Select};

use crate::{db_entries::plugin_settings, errors::CoreError, plugins::PluginManager};

pub async fn get_plugin_config(
    db: &DbConn,
    plugin_id: &str,
) -> Result<HashMap<String, String>, CoreError> {
    let query: Select<plugin_settings::Entity> =
        plugin_settings::Entity::find().filter(plugin_settings::Column::PluginId.eq(plugin_id));
    Ok(query
        .all(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {e}")))?
        .into_iter()
        .map(|settings| (settings.key, settings.value))
        .collect::<HashMap<String, String>>())
}

pub async fn scan_plugins_dir(plugins_dir: &PathBuf) -> Result<Vec<PathBuf>, CoreError> {
    if !plugins_dir.exists() {
        std::fs::create_dir_all(plugins_dir).map_err(|e| {
            CoreError::Other(format!(
                "Failed to create plugins directory at {}: {}",
                plugins_dir.display(),
                e
            ))
        })?;
    }

    Ok(std::fs::read_dir(plugins_dir)
        .map_err(|e| CoreError::Other(format!("Failed to read plugins directory: {e}")))?
        .flatten()
        .map(|entry| entry.path())
        .filter(|path| path.extension().and_then(|e| e.to_str()) == Some("wasm"))
        .collect::<Vec<_>>())
}

pub async fn register_plugin(
    db: &DbConn,
    plugin_manager: PluginManager,
    plugin_path: PathBuf,
) -> Result<(), CoreError> {
    let plugin_id = plugin_path
        .file_stem()
        .unwrap()
        .to_string_lossy()
        .to_string();

    let wasm_bytes = std::fs::read(&plugin_path).map_err(|e| {
        CoreError::Other(format!(
            "Failed to read plugin file {}: {}",
            plugin_path.display(),
            e
        ))
    })?;

    let plugin_config = get_plugin_config(db, &plugin_id).await?;

    tokio::task::spawn_blocking(move || {
        plugin_manager.register_plugin(&plugin_id, &plugin_config, wasm_bytes)
    })
    .await
    .map_err(|e| {
        CoreError::Other(format!(
            "Failed to join compilation thread for plugin {}: {}",
            plugin_path.display(),
            e
        ))
    })?
    .map_err(|e| {
        CoreError::Other(format!(
            "Failed to compile plugin {}: {}",
            plugin_path.display(),
            e
        ))
    })?;

    Ok(())
}
