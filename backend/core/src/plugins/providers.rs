use std::sync::Arc;
use sea_orm::DbConn;
use wreq::Client;
use wreq_util::Emulation;
use geneagrab_providers::traits::{ArchiveProvider, Fetcher};
use geneagrab_plugin_ad06::Ad06Provider;
use geneagrab_plugin_geneanet::GeneanetProvider;
use crate::errors::CoreError;
use crate::services::plugin::get_plugin_config;

pub struct CoreFetcher {
    pub client: Client,
}

#[async_trait::async_trait]
impl Fetcher for CoreFetcher {
    async fn fetch_raw(&self, req: geneagrab_providers::data::Request) -> Result<Vec<u8>, geneagrab_providers::errors::ProviderError> {
        use std::str::FromStr;
        use wreq::header::{HeaderName, HeaderValue};
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
        let mut req_builder = self.client
            .request(wreq::Method::from_str(&req.method.to_string()).unwrap(), &req.url)
            .headers(headers);
        if let Some(body) = req.body {
            req_builder = req_builder.body(body);
        }
        let resp = req_builder.send().await.map_err(|e| geneagrab_providers::errors::ProviderError::NetworkError(e.to_string()))?;

        tracing::info!("Sent request to {}, got status {}", req.url, resp.status());
        let body = resp.bytes().await.map_err(|e| geneagrab_providers::errors::ProviderError::NetworkError(e.to_string()))?;
        Ok(body.to_vec())
    }
}

pub fn get_providers(flaresolverr_url: &str, fetcher: Arc<dyn Fetcher>) -> Vec<Arc<dyn ArchiveProvider>> {
    vec![
        Arc::new(Ad06Provider::new(flaresolverr_url, fetcher.clone())),
        Arc::new(GeneanetProvider::new(flaresolverr_url, fetcher)),
    ]
}

pub fn get_provider_without_config(plugin_id: &str) -> Result<Arc<dyn ArchiveProvider>, CoreError> {
    let fetcher = get_fetcher()?;
    get_providers("", fetcher)
        .into_iter()
        .find(|p| p.metadata().id == plugin_id)
        .ok_or_else(|| CoreError::NotFound(format!("Plugin {plugin_id} not found")))
}

pub async fn get_provider(
    db: &DbConn,
    plugin_id: &str,
) -> Result<Arc<dyn ArchiveProvider>, CoreError> {
    let config = get_plugin_config(db, plugin_id).await?;
    let flaresolverr_url = config
        .get("flaresolverr_url")
        .cloned()
        .unwrap_or_else(|| "http://localhost:8191".to_string());

    let fetcher = get_fetcher()?;
    get_providers(&flaresolverr_url, fetcher)
        .into_iter()
        .find(|p| p.metadata().id == plugin_id)
        .ok_or_else(|| CoreError::NotFound(format!("Plugin {plugin_id} not found")))
}

pub fn get_fetcher() -> Result<Arc<dyn Fetcher>, CoreError> {
    let http_client = Client::builder()
        .emulation(Emulation::Firefox135)
        .build()
        .map_err(|e| CoreError::Other(format!("Failed to create HTTP client: {e}")))?;

    Ok(Arc::new(CoreFetcher { client: http_client }))
}
