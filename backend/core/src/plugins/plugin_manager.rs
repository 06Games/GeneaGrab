use extism::{Manifest, Plugin, PluginBuilder, Wasm};
use geneagrab_plugin_core::com_structs::HostPluginBase;
use geneagrab_plugin_core::data::PluginMetadata;
use std::collections::HashMap;
use std::sync::{Arc, Mutex, RwLock};

use crate::errors::CoreError;

struct PluginData {
    plugin: Mutex<Plugin>,
    metadata: PluginMetadata,
}

#[derive(Clone)]
pub struct PluginManager {
    registry: Arc<RwLock<HashMap<String, PluginData>>>,
}

impl Default for PluginManager {
    fn default() -> Self {
        Self {
            registry: Arc::new(RwLock::new(HashMap::new())),
        }
    }
}

impl PluginManager {
    pub fn register_plugin(
        &self,
        id: &str,
        plugin_config: HashMap<String, String>,
        wasm_bytes: Vec<u8>,
    ) -> Result<(), CoreError> {
        let manifest = Manifest::new([Wasm::data(wasm_bytes)])
            .with_allowed_host("*")
            .with_config(plugin_config.iter());

        let mut plugin = PluginBuilder::new(&manifest)
            .with_http_response_headers(true)
            .with_wasi(true)
            .build()
            .map_err(|e| CoreError::Other(format!("Failed to initialize plugin: {}", e)))?;

        if !HostPluginBase::is_supported(&plugin) {
            return Err(CoreError::Other(format!(
                "Plugin '{}' is missing required Base functions!",
                id
            )));
        }

        let meta = plugin.metadata(()).map_err(|e| {
            CoreError::Other(format!("Failed to get plugin metadata for '{}': {}", id, e))
        })?;

        let mut map = self.registry.write().map_err(|e| {
            CoreError::LockError(format!("Failed to write to plugin registry: {}", e))
        })?;
        map.insert(
            id.to_string(),
            PluginData {
                plugin: Mutex::new(plugin),
                metadata: meta.clone(),
            },
        );
        Ok(())
    }

    pub fn list_plugins(&self) -> Result<Vec<PluginMetadata>, CoreError> {
        let map = self
            .registry
            .read()
            .map_err(|e| CoreError::LockError(format!("Failed to read plugin registry: {}", e)))?;
        Ok(map.values().map(|data| data.metadata.clone()).collect())
    }

    pub async fn execute<F, R>(&self, plugin_id: &str, action: F) -> Result<R, CoreError>
    where
        F: FnOnce(&mut extism::Plugin) -> Result<R, extism::Error>,
    {
        let map = self
            .registry
            .read()
            .map_err(|e| CoreError::LockError(format!("Failed to read plugin registry: {}", e)))?;
        let plugin_mutex = &map
            .get(plugin_id)
            .ok_or_else(|| CoreError::NotFound(format!("Plugin {} not found", plugin_id)))?
            .plugin;

        let mut plugin = plugin_mutex
            .lock()
            .map_err(|e| CoreError::LockError(format!("Failed to lock plugin: {}", e)))?;

        action(&mut plugin).map_err(|e| CoreError::PluginError(e.to_string()))
    }
}
