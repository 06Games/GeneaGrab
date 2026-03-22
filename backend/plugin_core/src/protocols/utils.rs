use anyhow::Error;
use extism_pdk::{http, info, trace, HttpRequest};
use regex::Match;

pub type Fetcher = fn(&str) -> Result<String, Error>;

/**
Fetches the content of a URL as a string
*/
pub fn fetch_string(url: &str) -> Result<String, Error> {
    let req = HttpRequest::new(url);
    let res = http::request::<()>(&req, None)?;
    info!("Sent request to {}, got status {}", url, res.status_code());
    trace!("Response body: {:?}", res.body());
    String::from_utf8(res.body()).map_err(|e| Error::msg(format!("Invalid UTF-8: {}", e)))
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
