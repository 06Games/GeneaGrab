use extism_pdk::{http, info, trace, HttpRequest};
use regex::Match;

use crate::com_structs::PluginError;

pub trait Fetch {
    fn fetch(&self, url: &str) -> Result<String, PluginError>;
}

impl<F> Fetch for F
where
    F: Fn(&str) -> Result<String, PluginError>,
{
    fn fetch(&self, url: &str) -> Result<String, PluginError> {
        (self)(url)
    }
}

/**
Fetches the content of a URL as a string
*/
pub fn fetch_string(url: &str) -> Result<String, PluginError> {
    let req = HttpRequest::new(url);
    let res =
        http::request::<()>(&req, None).map_err(|e| PluginError::NetworkError(e.to_string()))?;
    info!("Sent request to {}, got status {}", url, res.status_code());
    trace!("Response body: {:?}", res.body());
    String::from_utf8(res.body())
        .map_err(|e| PluginError::NetworkError(format!("Invalid UTF-8: {}", e)))
}

/**
Returns the trimmed string if the regex match is valid and non-empty, otherwise returns None.
*/
pub fn validate_regex_match(reg_match: Option<Match>) -> Option<String> {
    reg_match
        .map(|m| m.as_str().trim().to_string())
        .map(|s| s.is_empty().then(|| None).unwrap_or(Some(s)))
        .flatten()
}
