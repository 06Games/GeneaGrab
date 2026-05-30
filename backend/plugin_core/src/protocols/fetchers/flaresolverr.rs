#![allow(clippy::doc_markdown)]
use std::{fmt::Display, string::ToString};

use extism_pdk::{config, var, Json};
use scraper::{Html, Selector};
use serde::{Deserialize, Serialize};
use url::Url;

use crate::{
    com_structs::PluginError,
    data::http::{FetchMethod, Request},
    protocols::fetchers::{Fetcher, HostFetcher, SimpleFetcher},
};

/// Proxies requests through FlareSolverr to bypass Cloudflare protections.
pub struct FlareSolverrFetcher {
    flaresolverr_url: String,
}

impl FlareSolverrFetcher {
    #[must_use]
    pub fn new(flaresolverr_url: &str) -> Self {
        Self {
            flaresolverr_url: flaresolverr_url.trim_end_matches('/').to_string(),
        }
    }

    /// # Errors
    ///
    /// If the config value couldn't be retrieved
    pub fn from_config() -> Result<Self, PluginError> {
        let flaresolverr_url = config::get("flaresolverr_url")
            .map_err(|e| PluginError::LibraryError(format!("Failed to get config: {e}")))?
            .unwrap_or_else(|| "http://localhost:8191".to_string());
        Ok(Self::new(&flaresolverr_url))
    }
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct FlareSolverrPayload {
    cmd: &'static str,
    url: String,
    session: String,
    max_timeout: u32,
    #[serde(skip_serializing_if = "Option::is_none")]
    post_data: Option<String>,
}

#[derive(Deserialize)]
struct FlareSolverrResponse {
    status: String,
    message: String,
    solution: Option<FlareSolverrSolution>,
}

#[derive(Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
struct FlareSolverrSolution {
    url: String,
    response: String,
    user_agent: String,
    cookies: Vec<FlareSolverrCookie>,
}

#[derive(Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
struct FlareSolverrCookie {
    pub name: String,
    pub value: String,
}

impl Display for FlareSolverrCookie {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}={}", self.name, self.value)
    }
}

#[derive(Serialize, Deserialize)]
struct Clearance {
    user_agent: String,
    cookies: Vec<FlareSolverrCookie>,
    referer: String,
}

impl TryFrom<FlareSolverrSolution> for Clearance {
    type Error = PluginError;

    fn try_from(value: FlareSolverrSolution) -> Result<Self, Self::Error> {
        Ok(Self {
            user_agent: value.user_agent,
            cookies: value.cookies,
            referer: build_referer(&value.url)?,
        })
    }
}

impl IntoIterator for Clearance {
    type Item = (String, String);
    type IntoIter = std::vec::IntoIter<Self::Item>;

    fn into_iter(self) -> Self::IntoIter {
        vec![
            ("User-Agent".to_string(), self.user_agent),
            (
                "Cookie".to_string(),
                build_cookie_header_string(&self.cookies),
            ),
            ("Referer".to_string(), self.referer),
        ]
        .into_iter()
    }
}

impl FlareSolverrFetcher {
    /// Performs a fetch through FlareSolverr.
    fn internal_fetch(&self, req: Request) -> Result<FlareSolverrSolution, PluginError> {
        let url = Url::parse(&req.url)
            .map_err(|e| PluginError::InvalidField(format!("Invalid URL: {e}")))?;
        let domain = url
            .domain()
            .ok_or_else(|| PluginError::InvalidField("URL has no domain".into()))?;
        let session_id = format!("geneagrab-{domain}");

        let (cmd, post_data) = match req.method {
            FetchMethod::GET => ("request.get", None),
            FetchMethod::POST => ("request.post", req.body),
            _ => return Err(PluginError::InvalidField("Unsupported HTTP method".into())),
        };

        let payload = FlareSolverrPayload {
            cmd,
            url: req.url,
            session: session_id,
            max_timeout: 60000,
            post_data,
        };

        let body = serde_json::to_string(&payload)
            .map_err(|_| PluginError::InvalidField("Failed to serialize payload".into()))?;

        let req = Request {
            url: format!("{}/v1", self.flaresolverr_url.clone()),
            headers: vec![("Content-Type".to_string(), "application/json".to_string())],
            method: FetchMethod::POST,
            body: Some(body),
        };

        let res = SimpleFetcher {}.fetch(req)?;

        let parsed_res: FlareSolverrResponse = serde_json::from_str(&res).map_err(|e| {
            PluginError::ParsingError(format!("Failed to parse FlareSolverr response: {e}"))
        })?;

        if parsed_res.status != "ok" {
            return Err(PluginError::NetworkError(format!(
                "FlareSolverr error: {}",
                parsed_res.message
            )));
        }

        let solution = parsed_res.solution.ok_or_else(|| {
            PluginError::ParsingError("No solution in FlareSolverr response".into())
        })?;

        Ok(solution)
    }

    /// Returns the response body given by Flaresolverr with some cleaning to try to recover the original response.
    fn clean_response(solution: FlareSolverrSolution) -> std::vec::Vec<u8> {
        let response = remove_xml_viewer(&solution.response)
            .or_else(|| remove_json_viewer(&solution.response))
            .unwrap_or(solution.response);
        response.into_bytes()
    }

    /// Performs the original request with the obtained cookies and user agent from FlareSolverr.
    /// Safer than `dirty_fetch` when the response isn't HTML, but uses two requests.
    fn safe_fetch(clearance: Clearance, req: Request) -> Result<Vec<u8>, PluginError> {
        let mut req = req;
        req.headers.extend(clearance);
        HostFetcher {}.fetch_raw(req)
    }
}

impl Fetcher for FlareSolverrFetcher {
    fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, PluginError> {
        let host = build_referer(&req.url)?;
        let cache_key = format!("flaresolverr_{host}");

        if needs_safe_request(&req.url) {
            if let Ok(Some(cached_solution)) = var::get::<Json<Clearance>>(&cache_key) {
                // Try to fetch using the cached session
                if let Ok(data) = FlareSolverrFetcher::safe_fetch(cached_solution.0, req.clone()) {
                    return Ok(data);
                }
            }
        }

        let solution = self.internal_fetch(req.clone())?;
        var::set(&cache_key, Json::<Clearance>(solution.clone().try_into()?))
            .map_err(|e| PluginError::LibraryError(format!("Couldn't store cache: {e}")))?;

        // Could be improved... but it's good enough for now
        if needs_safe_request(&req.url) {
            FlareSolverrFetcher::safe_fetch(solution.try_into()?, req)
        } else {
            Ok(FlareSolverrFetcher::clean_response(solution))
        }
    }
}

/// Determine if a request needs a second pass without FlareSolverr (because of the browser modifying the response)
fn needs_safe_request(url: &str) -> bool {
    std::path::Path::new(url).extension().is_some_and(|ext| {
        ext.eq_ignore_ascii_case("jpg")
            || ext.eq_ignore_ascii_case("png")
            || ext.eq_ignore_ascii_case("jpeg")
    })
}

/// Build the Referer header for a request
fn build_referer(url: &str) -> Result<String, PluginError> {
    let url =
        Url::parse(url).map_err(|e| PluginError::ParsingError(format!("Invalid url: {e}")))?;
    Ok(format!(
        "{}://{}",
        url.scheme(),
        url.host_str()
            .ok_or(PluginError::ParsingError(String::from(
                "Can't generate referer with empty host"
            )))?
    ))
}

/// Converts a list of cookies into a single Cookie header string.
fn build_cookie_header_string(cookies: &[FlareSolverrCookie]) -> String {
    cookies
        .iter()
        .map(ToString::to_string)
        .collect::<Vec<String>>()
        .join("; ")
}

/// FlareSolverr wraps XML responses in viewer, this function removes that wrapper if it exists.
fn remove_xml_viewer(response: &str) -> Option<String> {
    let viewer_tag = "<div id=\"webkit-xml-viewer-source-xml\">";
    let start_idx = response.find(viewer_tag)?;
    let content_start = start_idx + viewer_tag.len();
    let after_start = &response[content_start..];
    let end_idx = after_start.find("</div>")?;
    Some(after_start[..end_idx].to_string())
}

// FlareSolverr wraps JSON responses in viewer, this function removes that wrapper if it exists.
fn remove_json_viewer(response: &str) -> Option<String> {
    let document = Html::parse_document(response);
    let selector = Selector::parse("pre").ok()?;
    let pre_element = document.select(&selector).next()?;
    let json_text = pre_element.text().collect::<String>();
    Some(json_text)
}
