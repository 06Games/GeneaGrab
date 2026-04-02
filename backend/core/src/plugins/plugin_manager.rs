use extism::convert::Json;
use extism::{host_fn, Manifest, Plugin, PluginBuilder, UserData, Wasm, PTR};
use geneagrab_plugin_core::com_structs::HostPluginBase;
use geneagrab_plugin_core::data::http::Request;
use geneagrab_plugin_core::data::PluginMetadata;
use log::{info, trace};
use serde::ser::StdError;
use std::collections::HashMap;
use std::str::FromStr;
use std::sync::{Arc, Mutex, MutexGuard, RwLock};
use tokio::runtime::Handle;
use wreq::header::{self, HeaderMap, HeaderName, HeaderValue};
use wreq::{Client, Method};
use wreq_util::Emulation;

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

async fn http_request_impl(
    req: Request,
    client: MutexGuard<'_, Client>,
) -> Result<Vec<u8>, Box<dyn StdError>> {
    let headers = req
        .headers
        .iter()
        .map(|(k, v)| {
            (
                HeaderName::from_str(k.as_str()).unwrap(),
                HeaderValue::from_str(v.as_str()).unwrap(),
            )
        })
        .collect();
    let mut req_builder = client
        .request(Method::from_str(&req.method.to_string())?, &req.url)
        .headers(headers);
    if let Some(body) = req.body {
        req_builder = req_builder.body(body);
    }
    let resp = req_builder.send().await?;

    info!("Sent request to {}, got status {}", req.url, resp.status());
    let body = resp.bytes().await?;
    trace!("Response body: {:?}", body);

    Ok(body.to_vec())
}

host_fn!(http_request (user_data: Client;req: Json<Request>) -> Vec<u8> {
    let request_data = req.0;
    let client = user_data.get()?;
    let client = client.lock().unwrap();
    tokio::task::block_in_place(|| {
        Handle::current().block_on(async {
            http_request_impl(request_data, client).await
        })
    })
    .map_err(|e| extism::Error::msg(e.to_string()))
});

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
        let http_client = Client::builder()
            .emulation(Emulation::Firefox135)
            .build()
            .map_err(|e| CoreError::Other(format!("Failed to create HTTP client: {}", e)))?;

        let mut plugin = PluginBuilder::new(&manifest)
            .with_http_response_headers(true)
            .with_wasi(true)
            .with_function(
                "http_request",
                [PTR],
                [PTR],
                UserData::new(http_client),
                http_request,
            )
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
