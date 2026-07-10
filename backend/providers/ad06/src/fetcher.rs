use base64::{engine::general_purpose, Engine};
use scraper::{Html, Selector};
use url::form_urlencoded;

use geneagrab_providers::{
    data::{FetchMethod, Request},
    errors::ProviderError,
    protocols::fetchers::CachedFlareSolverrFetcher,
    traits::Fetcher,
};

static OCR_ENGINE: tokio::sync::Mutex<Option<ddddocr::DdddOcr>> =
    tokio::sync::Mutex::const_new(None);

struct CaptchaState {
    last_request: Option<std::time::Instant>,
}

static CAPTCHA_STATE: tokio::sync::Mutex<CaptchaState> =
    tokio::sync::Mutex::const_new(CaptchaState { last_request: None });

pub struct AdamFetcher<'a, F: Fetcher + ?Sized> {
    fetcher: CachedFlareSolverrFetcher<'a, F>,
}

impl<'a, F: Fetcher + ?Sized> AdamFetcher<'a, F> {
    #[must_use]
    pub fn new(flaresolverr_url: &str, fetcher: &'a F) -> Self {
        Self {
            fetcher: CachedFlareSolverrFetcher::new(flaresolverr_url, fetcher),
        }
    }

    fn extract_captcha_image(res: &[u8]) -> Result<Option<Vec<u8>>, ProviderError> {
        let html = String::from_utf8_lossy(res);
        if html.contains("captcha_audio") {
            let document = Html::parse_document(&html);
            let img_selector = Selector::parse("img[src^='data:image/png;base64,']").unwrap();
            if let Some(img_element) = document.select(&img_selector).next() {
                let src_attr = img_element.value().attr("src").unwrap();

                // Split out the prefix "data:image/png;base64,"
                let base64_payload = src_attr.trim_start_matches("data:image/png;base64,");

                // Decode the Base64 payload into raw image bytes
                let image_bytes =
                    general_purpose::STANDARD
                        .decode(base64_payload)
                        .map_err(|e| {
                            ProviderError::ParsingError(format!(
                                "Error decoding captcha image: {e}"
                            ))
                        })?;
                Ok(Some(image_bytes))
            } else {
                Err(ProviderError::ParsingError(String::from(
                    "A captcha is present but the image to resolve couldn't be found",
                )))
            }
        } else {
            Ok(None)
        }
    }

    fn is_blocked_by_captcha(res: &Result<Vec<u8>, ProviderError>) -> bool {
        match res {
            Ok(data) => {
                let html = String::from_utf8_lossy(data);
                html.contains("captcha_audio")
            }
            Err(ProviderError::NetworkError(msg)) => {
                msg.contains("ERR_CONNECTION_RESET")
                    || msg.to_ascii_lowercase().contains("connection reset")
            }
            _ => false,
        }
    }

    fn get_onnx_path() -> Result<std::path::PathBuf, ProviderError> {
        const MODEL_BYTES: &[u8] = include_bytes!("../ddddocr_common.onnx");
        let temp_path = std::env::temp_dir().join("ddddocr_common.onnx");
        if !temp_path.exists() {
            tracing::info!(
                "Writing embedded OCR model to temporary file: {:?}",
                temp_path
            );
            std::fs::write(&temp_path, MODEL_BYTES).map_err(|e| {
                ProviderError::LibraryError(format!("Failed to write embedded OCR model: {e}"))
            })?;
        }
        Ok(temp_path)
    }

    fn truncate_to_3rd_path_level(url_str: &str) -> Result<String, ProviderError> {
        let mut url = url::Url::parse(url_str)
            .map_err(|e| ProviderError::ParsingError(format!("Invalid URL: {e}")))?;

        if let Some(segments) = url.path_segments() {
            let truncated: Vec<&str> = segments.take(3).collect();
            let new_path = truncated.join("/");
            url.set_path(&new_path);
        }

        url.set_query(None);
        url.set_fragment(None);

        Ok(url.to_string())
    }

    fn prepare_captcha_submit(
        html_bytes: &[u8],
        base_url: &str,
        code: &str,
    ) -> Result<Option<Request>, ProviderError> {
        let html = String::from_utf8_lossy(html_bytes);
        let document = Html::parse_document(&html);

        let form_selector = Selector::parse("form").unwrap();
        let input_selector = Selector::parse("input").unwrap();

        for form in document.select(&form_selector) {
            let mut has_captcha_input = false;
            let mut form_fields = Vec::new();

            for input in form.select(&input_selector) {
                let name = input.value().attr("name").unwrap_or("");
                let value = input.value().attr("value").unwrap_or("");
                let input_type = input.value().attr("type").unwrap_or("");

                if name == "captcha_code" {
                    has_captcha_input = true;
                    form_fields.push((name.to_string(), code.to_string()));
                } else if input_type != "submit" && !name.is_empty() {
                    form_fields.push((name.to_string(), value.to_string()));
                }
            }

            if has_captcha_input {
                let action = form.value().attr("action").unwrap_or("");
                let submit_url = if action.is_empty() {
                    base_url.to_string()
                } else {
                    let base = url::Url::parse(base_url)
                        .map_err(|e| ProviderError::InvalidField(e.to_string()))?;
                    base.join(action)
                        .map_err(|e| ProviderError::InvalidField(e.to_string()))?
                        .to_string()
                };

                let body = form_urlencoded::Serializer::new(String::new())
                    .extend_pairs(form_fields)
                    .finish();

                return Ok(Some(Request {
                    url: submit_url,
                    method: FetchMethod::POST,
                    headers: vec![(
                        "Content-Type".to_string(),
                        "application/x-www-form-urlencoded".to_string(),
                    )],
                    body: Some(body),
                }));
            }
        }

        Ok(None)
    }
}

#[async_trait::async_trait]
impl<F: Fetcher + ?Sized> Fetcher for AdamFetcher<'_, F> {
    async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError> {
        let first_attempt = self.fetcher.fetch_raw(req.clone()).await;

        if Self::is_blocked_by_captcha(&first_attempt) {
            let failure_time = std::time::Instant::now();
            let mut state = CAPTCHA_STATE.lock().await;

            let need_request = match state.last_request {
                Some(instant) => instant.elapsed() > std::time::Duration::from_mins(1),
                None => true,
            };

            if need_request {
                let challenge_url = Self::truncate_to_3rd_path_level(&req.url)?;
                tracing::info!("Detected captcha challenge or connection reset. Requesting 3rd level URL to trigger captcha: {}...", challenge_url);
                let root_req = Request {
                    url: challenge_url,
                    method: FetchMethod::GET,
                    headers: vec![],
                    body: None,
                };

                let root_response = self.fetcher.fetch_raw(root_req).await;

                if let Ok(root_res_data) = root_response {
                    if let Ok(Some(captcha_bytes)) = Self::extract_captcha_image(&root_res_data) {
                        tracing::warn!(
                            "Captcha challenge image successfully extracted from root response"
                        );

                        let mut ocr_guard = OCR_ENGINE.lock().await;
                        if ocr_guard.is_none() {
                            let path = Self::get_onnx_path()?;
                            tracing::info!("Loading OCR model from {:?}", path);
                            *ocr_guard = Some(
                                ddddocr::DdddOcr::new(path.to_str().unwrap()).map_err(|e| {
                                    ProviderError::LibraryError(format!(
                                        "Failed to load ddddocr model: {e:?}"
                                    ))
                                })?,
                            );
                        }

                        if let Some(ocr) = ocr_guard.as_mut() {
                            match ocr.classification(&captcha_bytes).await {
                                Ok(code) => {
                                    let trimmed_code = code.trim();
                                    tracing::warn!("Solved captcha: '{}'", trimmed_code);
                                    if let Some(submit_req) = Self::prepare_captcha_submit(
                                        &root_res_data,
                                        &req.url,
                                        trimmed_code,
                                    )? {
                                        tracing::info!(
                                            "Submitting solved captcha to {}...",
                                            submit_req.url
                                        );
                                        match self.fetcher.fetch_raw(submit_req).await {
                                            Ok(_) => {
                                                tracing::info!("Captcha submitted successfully");
                                            }
                                            Err(e) => {
                                                tracing::error!("Failed to submit captcha: {e:?}");
                                            }
                                        }
                                    }
                                }
                                Err(e) => {
                                    tracing::error!("Failed to solve captcha with OCR: {:?}", e);
                                }
                            }
                        }
                    }
                }

                state.last_request = Some(std::time::Instant::now());
            }

            drop(state);

            // Bypassing the 5-second FlareSolverr cache cooldown:
            // If the first attempt failed, the core fetcher recorded the failure in a 5-second error cache.
            // If we retry before 5 seconds have elapsed, the core fetcher immediately returns the cached error.
            // We sleep for the remaining duration of the 5 seconds since the failure before retrying.
            let elapsed = failure_time.elapsed();
            if elapsed < std::time::Duration::from_secs(5) {
                let sleep_duration = std::time::Duration::from_secs(5)
                    .checked_sub(elapsed)
                    .unwrap_or_default();
                tracing::info!(
                    "Sleeping for {:?} to bypass FlareSolverr cooldown cache...",
                    sleep_duration
                );
                tokio::time::sleep(sleep_duration).await;
            }

            // Retry the original request
            let second_attempt = self.fetcher.fetch_raw(req).await;
            if Self::is_blocked_by_captcha(&second_attempt) {
                if let Ok(data) = &second_attempt {
                    Self::extract_captcha_image(data)?;
                }
                return Err(ProviderError::NetworkError(String::from(
                    "Resource is behind a captcha",
                )));
            }
            return second_attempt;
        }

        first_attempt
    }
}
