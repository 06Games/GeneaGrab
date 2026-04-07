use extism_pdk::{http, info, trace, HttpRequest};

use crate::{com_structs::PluginError, data::http::Request, protocols::fetchers::Fetcher};

/**
A simple fetcher that uses the built-in HTTP client.
*/
pub struct SimpleFetcher;

impl Fetcher for SimpleFetcher {
    fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, PluginError> {
        let mut http_req = HttpRequest::new(&req.url).with_method(req.method.to_string());
        for (key, value) in req.headers {
            http_req = http_req.with_header(&key, &value);
        }
        let http_req = http_req;

        let res = http::request(&http_req, req.body)
            .map_err(|e| PluginError::NetworkError(e.to_string()))?;
        info!(
            "Sent request to {}, got status {}",
            req.url,
            res.status_code()
        );
        trace!("Response body: {:?}", res.body());

        if !is_success_status(res.status_code()) {
            return Err(PluginError::NetworkError(format!(
                "Request failed with status {}: {}",
                res.status_code(),
                String::from_utf8_lossy(&res.body())
            )));
        }

        Ok(res.body().to_vec())
    }
}

fn is_success_status(status: u16) -> bool {
    (200..300).contains(&status)
}
