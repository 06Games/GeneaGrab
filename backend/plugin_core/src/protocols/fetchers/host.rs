use extism_pdk::{host_fn, Json};

use crate::{com_structs::PluginError, data::http::Request, protocols::fetchers::Fetcher};

#[host_fn]
extern "ExtismHost" {
    fn http_request(req: Json<Request>) -> Vec<u8>;
}

/**
A fetcher that proxies requests through the Rust host environment.
*/
pub struct HostFetcher;

impl Fetcher for HostFetcher {
    fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, PluginError> {
        let res = unsafe { http_request(Json::from(req)) }
            .map_err(|e| PluginError::NetworkError(e.to_string()))?;

        Ok(res)
    }
}
