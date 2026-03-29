use extism_pdk::config;
use serde::{Deserialize, Serialize};
use url::Url;

use crate::{
    com_structs::PluginError,
    protocols::fetchers::{FetchMethod, Fetcher, Request, SimpleFetcher},
};

/**
Proxies requests through FlareSolverr to bypass Cloudflare protections.
*/
pub struct FlareSolverrFetcher {
    flaresolverr_url: String,
}

impl FlareSolverrFetcher {
    pub fn new(flaresolverr_url: String) -> Self {
        Self {
            flaresolverr_url: flaresolverr_url.trim_end_matches('/').to_string(),
        }
    }

    pub fn from_config() -> Result<Self, PluginError> {
        let flaresolverr_url = config::get("flaresolverr_url")
            .map_err(|e| PluginError::LibraryError(format!("Failed to get config: {}", e)))?
            .unwrap_or_default();
        Ok(Self::new(flaresolverr_url))
    }
}

impl Default for FlareSolverrFetcher {
    fn default() -> Self {
        Self {
            flaresolverr_url: "http://localhost:8191".to_string(),
        }
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

#[derive(Deserialize)]
struct FlareSolverrSolution {
    response: String,
}

impl Fetcher for FlareSolverrFetcher {
    fn fetch(&self, req: Request) -> Result<String, PluginError> {
        let url = Url::parse(&req.url)
            .map_err(|e| PluginError::InvalidField(format!("Invalid URL: {}", e)))?;
        let domain = url
            .domain()
            .ok_or_else(|| PluginError::InvalidField("URL has no domain".into()))?;
        let session_id = format!("geneagrab-{}", domain);

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
            PluginError::ParsingError(format!("Failed to parse FlareSolverr response: {}", e))
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

        Ok(remove_xml_viewer(solution.response))
    }
}

/**
FlareSolverr wraps XML responses in viewer, this function removes that wrapper if it exists.
*/
fn remove_xml_viewer(response: String) -> String {
    let viewer_tag = "<div id=\"webkit-xml-viewer-source-xml\">";
    if let Some(start_idx) = response.find(viewer_tag) {
        let content_start = start_idx + viewer_tag.len();
        let after_start = &response[content_start..];

        if let Some(end_idx) = after_start.find("</div>") {
            return after_start[..end_idx].to_string();
        }
    }
    response
}
