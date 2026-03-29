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
    fn fetch(&self, req: Request) -> Result<String, PluginError> {
        let res = unsafe { http_request(Json::from(req)) }
            .map_err(|e| PluginError::NetworkError(e.to_string()))?;

        String::from_utf8(res)
            .map_err(|e| PluginError::NetworkError(format!("Invalid UTF-8: {}", e)))
    }
}
