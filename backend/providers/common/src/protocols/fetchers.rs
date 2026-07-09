use std::collections::HashMap;
use std::fmt::Display;
use std::sync::LazyLock;
use tokio::sync::RwLock;
use url::Url;
use scraper::{Html, Selector};
use serde::{Deserialize, Serialize};

use crate::{
    errors::ProviderError,
    data::{FetchMethod, Request},
    traits::Fetcher,
};

static FLARESOLVERR_CACHE: LazyLock<RwLock<HashMap<String, Clearance>>> =
    LazyLock::new(|| RwLock::new(HashMap::new()));

/// Proxies requests through FlareSolverr to bypass Cloudflare protections.
pub struct FlareSolverrFetcher<'a, F: Fetcher + ?Sized> {
    flaresolverr_url: String,
    fetcher: &'a F,
}

impl<'a, F: Fetcher + ?Sized> FlareSolverrFetcher<'a, F> {
    #[must_use]
    pub fn new(flaresolverr_url: &str, fetcher: &'a F) -> Self {
        Self {
            flaresolverr_url: flaresolverr_url.trim_end_matches('/').to_string(),
            fetcher,
        }
    }

    /// Performs a fetch through FlareSolverr.
    pub async fn internal_fetch(&self, req: Request) -> Result<FlareSolverrSolution, ProviderError> {
        let url = Url::parse(&req.url)
            .map_err(|e| ProviderError::InvalidField(format!("Invalid URL: {e}")))?;
        let domain = url
            .domain()
            .ok_or_else(|| ProviderError::InvalidField("URL has no domain".into()))?;
        let session_id = format!("geneagrab-{domain}");

        let (cmd, post_data) = match req.method {
            FetchMethod::GET => ("request.get", None),
            FetchMethod::POST => ("request.post", req.body),
            _ => return Err(ProviderError::InvalidField("Unsupported HTTP method".into())),
        };

        let payload = FlareSolverrPayload {
            cmd,
            url: req.url,
            session: session_id,
            max_timeout: 60000,
            post_data,
        };

        let body = serde_json::to_string(&payload)
            .map_err(|_| ProviderError::InvalidField("Failed to serialize payload".into()))?;

        let req = Request {
            url: format!("{}/v1", self.flaresolverr_url),
            headers: vec![("Content-Type".to_string(), "application/json".to_string())],
            method: FetchMethod::POST,
            body: Some(body),
        };

        let res = self.fetcher.fetch(req).await?;

        let parsed_res: FlareSolverrResponse = serde_json::from_str(&res).map_err(|e| {
            ProviderError::ParsingError(format!("Failed to parse FlareSolverr response: {e}"))
        })?;

        if parsed_res.status != "ok" {
            return Err(ProviderError::NetworkError(format!(
                "FlareSolverr error: {}",
                parsed_res.message
            )));
        }

        let solution = parsed_res.solution.ok_or_else(|| {
            ProviderError::ParsingError("No solution in FlareSolverr response".into())
        })?;

        Ok(solution)
    }

    /// Performs the original request with the obtained cookies and user agent from FlareSolverr.
    pub async fn safe_fetch(
        &self,
        clearance: Clearance,
        req: Request,
    ) -> Result<Vec<u8>, ProviderError> {
        let mut req = req;
        req.headers.extend(clearance);
        self.fetcher.fetch_raw(req).await
    }

    pub async fn use_solution(
        &self,
        req: Request,
        solution: FlareSolverrSolution,
    ) -> Result<Vec<u8>, ProviderError> {
        if needs_safe_request(&req.url) {
            self.safe_fetch(solution.try_into()?, req).await
        } else {
            Ok(clean_response(solution))
        }
    }
}

#[async_trait::async_trait]
impl<'a, F: Fetcher + ?Sized> Fetcher for FlareSolverrFetcher<'a, F> {
    async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError> {
        let solution = self.internal_fetch(req.clone()).await?;
        self.use_solution(req, solution).await
    }
}

pub struct CachedFlareSolverrFetcher<'a, F: Fetcher + ?Sized> {
    flaresolverr_fetcher: FlareSolverrFetcher<'a, F>,
}

impl<'a, F: Fetcher + ?Sized> CachedFlareSolverrFetcher<'a, F> {
    #[must_use]
    pub fn new(flaresolverr_url: &str, fetcher: &'a F) -> Self {
        Self {
            flaresolverr_fetcher: FlareSolverrFetcher::new(flaresolverr_url, fetcher),
        }
    }
}

#[async_trait::async_trait]
impl<'a, F: Fetcher + ?Sized> Fetcher for CachedFlareSolverrFetcher<'a, F> {
    async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError> {
        let host = build_referer(&req.url)?;
        let cache_key = format!("flaresolverr_{host}");

        if needs_safe_request(&req.url) {
            let read_guard = FLARESOLVERR_CACHE.read().await;
            if let Some(cached_solution) = read_guard.get(&cache_key) {
                match self
                    .flaresolverr_fetcher
                    .safe_fetch(cached_solution.clone(), req.clone())
                    .await
                {
                    Ok(data) => return Ok(data),
                    Err(_) => {
                        drop(read_guard);
                        let mut write_guard = FLARESOLVERR_CACHE.write().await;
                        write_guard.remove(&cache_key);
                    }
                }
            }
        }

        // Cache miss or expired cookies: serialize FlareSolverr calls
        static RESOLVE_LOCK: tokio::sync::Mutex<()> = tokio::sync::Mutex::const_new(());
        let _guard = RESOLVE_LOCK.lock().await;

        // Double check cache under lock (another thread might have resolved it in the meantime)
        if needs_safe_request(&req.url) {
            let read_guard = FLARESOLVERR_CACHE.read().await;
            if let Some(cached_solution) = read_guard.get(&cache_key) {
                let solution_clone = cached_solution.clone();
                drop(read_guard);
                drop(_guard); // drop lock before making network request
                if let Ok(data) = self
                    .flaresolverr_fetcher
                    .safe_fetch(solution_clone, req.clone())
                    .await
                {
                    return Ok(data);
                }
                // If it fails again, re-acquire the lock to perform a fresh fetch
                let _re_guard = RESOLVE_LOCK.lock().await;
                let solution = self.flaresolverr_fetcher.internal_fetch(req.clone()).await?;
                let clearance = Clearance::try_from(solution.clone())?;
                {
                    let mut write_guard = FLARESOLVERR_CACHE.write().await;
                    write_guard.insert(cache_key, clearance);
                }
                return self.flaresolverr_fetcher.use_solution(req, solution).await;
            }
        }

        let solution = self.flaresolverr_fetcher.internal_fetch(req.clone()).await?;
        let clearance = Clearance::try_from(solution.clone())?;

        {
            let mut write_guard = FLARESOLVERR_CACHE.write().await;
            write_guard.insert(cache_key, clearance);
        }

        drop(_guard);

        self.flaresolverr_fetcher.use_solution(req, solution).await
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
pub struct FlareSolverrSolution {
    url: String,
    response: String,
    user_agent: String,
    cookies: Vec<FlareSolverrCookie>,
}

#[derive(Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct FlareSolverrCookie {
    pub name: String,
    pub value: String,
}

impl Display for FlareSolverrCookie {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}={}", self.name, self.value)
    }
}

#[derive(Serialize, Deserialize, Clone)]
pub struct Clearance {
    user_agent: String,
    cookies: Vec<FlareSolverrCookie>,
    referer: String,
}

impl TryFrom<FlareSolverrSolution> for Clearance {
    type Error = ProviderError;

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
        .collect::<Vec<_>>()
        .into_iter()
    }
}

/// Determine if a request needs a second pass without FlareSolverr (because of the browser modifying the response)
pub fn needs_safe_request(url: &str) -> bool {
    std::path::Path::new(url).extension().is_some_and(|ext| {
        ext.eq_ignore_ascii_case("jpg")
            || ext.eq_ignore_ascii_case("png")
            || ext.eq_ignore_ascii_case("jpeg")
    })
}

/// Build the Referer header for a request
pub fn build_referer(url: &str) -> Result<String, ProviderError> {
    let url =
        Url::parse(url).map_err(|e| ProviderError::ParsingError(format!("Invalid url: {e}")))?;
    Ok(format!(
        "{}://{}",
        url.scheme(),
        url.host_str()
            .ok_or_else(|| ProviderError::ParsingError(String::from(
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

/// Returns the response body given by FlareSolverr with some cleaning to try to recover the original response.
fn clean_response(solution: FlareSolverrSolution) -> std::vec::Vec<u8> {
    let response = remove_xml_viewer(&solution.response)
        .or_else(|| remove_json_viewer(&solution.response))
        .or_else(|| fake_html(&solution.response))
        .unwrap_or(solution.response);
    response.into_bytes()
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

/// FlareSolverr wraps JSON responses in viewer, this function removes that wrapper if it exists.
fn remove_json_viewer(response: &str) -> Option<String> {
    let document = Html::parse_document(response);
    let selector = Selector::parse("pre").ok()?;
    let pre_element = document.select(&selector).next()?;
    let json_text = pre_element.text().collect::<String>();
    Some(json_text)
}

/// FlareSolverr wraps responses in HTML tags if the server wrongly declared the content type as text/html.
/// This function retrieves the actual response from the HTML body.
fn fake_html(response: &str) -> Option<String> {
    let trimmed = response.trim();
    let prefix = "<html><head></head><body>";
    let suffix = "</body></html>";

    if trimmed.starts_with(prefix) && trimmed.ends_with(suffix) {
        let start = prefix.len();
        let end = trimmed.len() - suffix.len();
        Some(trimmed[start..end].to_string())
    } else {
        None
    }
}
