use extism::{Manifest, Plugin, Wasm};
use geneagrab_plugin_core::com_structs::HostPluginBase;
use geneagrab_plugin_core::data::PluginMetadata;
use std::collections::HashMap;
use std::sync::{Mutex, RwLock};

use crate::errors::CoreError;

pub struct PluginManager {
    registry: RwLock<HashMap<String, Mutex<Plugin>>>,
}

impl Default for PluginManager {
    fn default() -> Self {
        Self::new()
    }
}

impl PluginManager {
    pub fn new() -> Self {
        Self {
            registry: RwLock::new(HashMap::new()),
        }
    }

    pub fn register_plugin(&mut self, id: String, wasm_bytes: Vec<u8>) -> Result<(), CoreError> {
        let manifest = Manifest::new([Wasm::data(wasm_bytes)]).with_allowed_host("*");
        let plugin = Plugin::new(&manifest, [], true)
            .map_err(|e| CoreError::Other(format!("Failed to initialize plugin: {}", e)))?;

        if !HostPluginBase::is_supported(&plugin) {
            return Err(CoreError::Other(format!(
                "Plugin '{}' is missing required Base functions!",
                id
            )));
        }

        let mut map = self.registry.write().map_err(|e| {
            CoreError::LockError(format!("Failed to write to plugin registry: {}", e))
        })?;
        map.insert(id, Mutex::new(plugin));
        Ok(())
    }

    pub fn list_plugins(&self) -> Result<Vec<PluginMetadata>, CoreError> {
        let map = self
            .registry
            .read()
            .map_err(|e| CoreError::LockError(format!("Failed to read plugin registry: {}", e)))?;
        Ok(map
            .keys()
            .map(|k| PluginMetadata {
                id: k.clone(),
                name: format!("{} Extractor", k),
            })
            .collect())
    }

    pub fn execute<F, R>(&self, plugin_id: &str, action: F) -> Result<R, CoreError>
    where
        F: FnOnce(&mut extism::Plugin) -> Result<R, extism::Error>,
    {
        let map = self
            .registry
            .read()
            .map_err(|e| CoreError::LockError(format!("Failed to read plugin registry: {}", e)))?;
        let plugin_mutex = map
            .get(plugin_id)
            .ok_or_else(|| CoreError::NotFound(format!("Plugin {} not found", plugin_id)))?;

        let mut plugin = plugin_mutex
            .lock()
            .map_err(|e| CoreError::LockError(format!("Failed to lock plugin: {}", e)))?;

        action(&mut plugin).map_err(|e| CoreError::PluginError(e.to_string()))
    }
}
