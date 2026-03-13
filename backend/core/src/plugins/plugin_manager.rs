use extism::{Manifest, Plugin, Wasm};
use geneagrab_plugin_core::com_structs::{ExtractRequest, ExtractResponse};
use geneagrab_plugin_core::data::PluginMetadata;
use std::collections::HashMap;
use std::sync::Mutex;

use crate::errors::CoreError;

pub struct PluginManager {
    registry: HashMap<String, Mutex<Plugin>>,
}

impl Default for PluginManager {
    fn default() -> Self {
        Self::new()
    }
}

impl PluginManager {
    pub fn new() -> Self {
        Self {
            registry: HashMap::new(),
        }
    }

    pub fn register_plugin(&mut self, id: String, wasm_bytes: Vec<u8>) {
        let manifest = Manifest::new([Wasm::data(wasm_bytes)]).with_allowed_host("*");

        match Plugin::new(&manifest, [], true) {
            Ok(plugin) => {
                self.registry.insert(id, Mutex::new(plugin));
            }
            Err(e) => {
                log::error!("Failed to initialize plugin '{}': {}", id, e);
            }
        }
    }

    pub fn list_plugins(&self) -> Vec<PluginMetadata> {
        self.registry
            .keys()
            .map(|k| PluginMetadata {
                id: k.clone(),
                name: format!("{} Extractor", k),
            })
            .collect()
    }

    pub fn extract(&self, plugin_id: &str, url: &str) -> Result<ExtractResponse, CoreError> {
        let plugin_mutex = self
            .registry
            .get(plugin_id)
            .ok_or_else(|| CoreError::NotFound(format!("Plugin {} not found", plugin_id)))?;

        let mut plugin = plugin_mutex
            .lock()
            .map_err(|_| CoreError::Other("Plugin lock poisoned".into()))?;

        let req = ExtractRequest {
            url: url.to_string(),
        };
        let req_json = serde_json::to_string(&req)
            .map_err(|e| CoreError::Other(format!("Failed to serialize request: {}", e)))?;

        let res = plugin
            .call::<&str, &str>("extract_registry", &req_json)
            .map_err(|e| CoreError::Other(format!("Plugin execution failed: {}", e)))?;

        let parsed: ExtractResponse = serde_json::from_str(res)
            .map_err(|e| CoreError::Other(format!("Invalid response from plugin: {}", e)))?;

        Ok(parsed)
    }
}
