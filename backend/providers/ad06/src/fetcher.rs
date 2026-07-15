use base64::{engine::general_purpose, Engine};
use scraper::{Html, Selector};
use url::form_urlencoded;

use geneagrab_providers::{
    data::{FetchMethod, Request},
    errors::ProviderError,
    protocols::fetchers::CachedFlareSolverrFetcher,
    traits::Fetcher,
};

static CHARSET: std::sync::OnceLock<Vec<String>> = std::sync::OnceLock::new();

fn get_charset() -> &'static Vec<String> {
    CHARSET.get_or_init(|| serde_json::from_str(include_str!("../charset.json")).unwrap())
}

static OCR_SESSION: tokio::sync::Mutex<Option<ort::session::Session>> =
    tokio::sync::Mutex::const_new(None);

struct CaptchaState {
    last_request: Option<std::time::Instant>,
    is_cleared: bool,
    is_clearing: bool,
}

static CAPTCHA_STATE: tokio::sync::Mutex<CaptchaState> =
    tokio::sync::Mutex::const_new(CaptchaState {
        last_request: None,
        is_cleared: false,
        is_clearing: false,
    });

static CAPTCHA_NOTIFY: tokio::sync::Notify = tokio::sync::Notify::const_new();

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
                    || html.to_ascii_lowercase().contains("forbidden")
                    || html.to_ascii_lowercase().contains("access denied")
                    || html.to_ascii_lowercase().contains("captcha")
            }
            Err(ProviderError::NetworkError(_)) => true,
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

    async fn solve_captcha_and_retry(
        &self,
        req: Request,
        _failure_time: std::time::Instant,
    ) -> Result<Vec<u8>, ProviderError> {
        // Clear the 5-second FlareSolverr error cooldown cache for this host to retry immediately
        geneagrab_providers::protocols::fetchers::clear_error_cooldown(&req.url).await;

        let mut state = CAPTCHA_STATE.lock().await;

        let need_request = match state.last_request {
            Some(instant) => instant.elapsed() > std::time::Duration::from_mins(1),
            None => true,
        };

        if need_request {
            let challenge_url =
                String::from("https://archives06.fr/ark:/79346/ecfe2e9e042c39520667bbe5f35f84a327");
            tracing::info!("Detected captcha challenge or connection reset. Trying to trigger a captcha by navigating to standard page: {}...", challenge_url);
            let root_req = Request {
                url: challenge_url.clone(),
                method: FetchMethod::GET,
                headers: vec![],
                body: None,
            };

            let root_response = self.fetcher.fetch_raw(root_req).await;

            match root_response {
                Ok(root_res_data) => {
                    if let Ok(Some(captcha_bytes)) = Self::extract_captcha_image(&root_res_data) {
                        tracing::warn!(
                            "Captcha challenge image successfully extracted from root response"
                        );

                        let mut session_guard = OCR_SESSION.lock().await;
                        if session_guard.is_none() {
                            let path = Self::get_onnx_path()?;
                            tracing::info!("Loading OCR session from {:?}", path);
                            let session = ort::session::Session::builder()
                                .map_err(|e| {
                                    ProviderError::LibraryError(format!(
                                        "Failed to create SessionBuilder: {e:?}"
                                    ))
                                })?
                                .commit_from_file(path)
                                .map_err(|e| {
                                    ProviderError::LibraryError(format!(
                                        "Failed to load ONNX model: {e:?}"
                                    ))
                                })?;
                            *session_guard = Some(session);
                        }

                        if let Some(session) = session_guard.as_mut() {
                            match Self::classify_image(session, &captcha_bytes) {
                                Ok(solved_code) => {
                                    tracing::warn!("Solved captcha: '{}'", solved_code);
                                    if let Some(submit_req) = Self::prepare_captcha_submit(
                                        &root_res_data,
                                        &req.url,
                                        &solved_code,
                                    )? {
                                        tracing::info!(
                                            "Submitting solved captcha to {}...",
                                            submit_req.url
                                        );
                                        match self.fetcher.fetch_raw(submit_req).await {
                                            Ok(_) => {
                                                tracing::info!("Captcha submitted successfully");

                                                state.last_request =
                                                    Some(std::time::Instant::now());
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
                Err(e) => {
                    tracing::error!("Request to triggering page failed: {e}");
                }
            }
        }

        drop(state);

        // Retry the original request
        let mut second_attempt = self.fetcher.fetch_raw(req.clone()).await;
        if Self::is_blocked_by_captcha(&second_attempt) {
            if let Ok(data) = &second_attempt {
                if let Ok(Some(captcha_bytes)) = Self::extract_captcha_image(data) {
                    if let Some(prompter) = geneagrab_providers::protocols::fetchers::CAPTCHA_PROMPTER.get() {
                        tracing::warn!("OCR failed to solve the captcha. Prompting user for manual verification...");
                        match (prompter)(captcha_bytes).await {
                            Ok(manual_code) => {
                                tracing::info!("User provided manual captcha code: '{}'", manual_code);
                                if let Some(submit_req) = Self::prepare_captcha_submit(data, &req.url, &manual_code)? {
                                    match self.fetcher.fetch_raw(submit_req).await {
                                        Ok(_) => {
                                            tracing::info!("Manual captcha submitted successfully");
                                            second_attempt = self.fetcher.fetch_raw(req).await;
                                        }
                                        Err(e) => {
                                            tracing::error!("Failed to submit manual captcha: {e:?}");
                                        }
                                    }
                                }
                            }
                            Err(e) => {
                                tracing::error!("Failed to prompt user for captcha: {:?}", e);
                            }
                        }
                    }
                }
            }
        }

        if Self::is_blocked_by_captcha(&second_attempt) {
            return Err(ProviderError::NetworkError(String::from(
                "Resource is behind a captcha",
            )));
        }
        second_attempt
    }

    fn classify_image(
        session: &mut ort::session::Session,
        img_bytes: &[u8],
    ) -> Result<String, ProviderError> {
        // 1. Decode image
        let img = image::load_from_memory(img_bytes)
            .map_err(|e| ProviderError::LibraryError(format!("Failed to decode image: {e:?}")))?;

        // 2. Resize to maintain aspect ratio with height = 64
        let new_width = (img.width() as f32 * (64.0 / img.height() as f32)) as u32;
        let resized = img.resize_exact(new_width, 64, image::imageops::FilterType::Lanczos3);

        // 3. Convert to grayscale
        let gray_image = resized.to_luma8();

        // 4. Normalize to [0, 1] matching Python ddddocr: pixel / 255.0
        let height = gray_image.height() as usize;
        let width = gray_image.width() as usize;

        let mut img_data = Vec::with_capacity(height * width);
        for pixel in gray_image.pixels() {
            let normalized = pixel[0] as f32 / 255.0;
            img_data.push(normalized);
        }

        // 5. Create input tensor using (shape, data) tuple
        let shape = vec![1usize, 1, height, width];
        let input_value = ort::value::Value::from_array((shape, img_data)).map_err(|e| {
            ProviderError::LibraryError(format!("Failed to create input tensor: {e:?}"))
        })?;

        // 6. Run inference
        let inputs = std::collections::HashMap::from([("input1".to_string(), input_value)]);
        let outputs = session
            .run(inputs)
            .map_err(|e| ProviderError::LibraryError(format!("Inference run error: {e:?}")))?;
        let output = &outputs[0];

        // 6. Extract tensor outputs
        let (_, output_data) = output
            .try_extract_tensor::<i64>()
            .map_err(|e| ProviderError::LibraryError(format!("Tensor extraction error: {e:?}")))?;

        // 7. Decode using CHARSET, skipping duplicate and 0 values
        let charset = get_charset();
        let mut result = String::new();
        let mut last_item = 0i64;

        for &item in output_data {
            if item == last_item {
                continue;
            }
            last_item = item;

            if let Some(char_str) = charset.get(item as usize) {
                result.push_str(char_str);
            }
        }

        Ok(result)
    }
}

#[async_trait::async_trait]
impl<F: Fetcher + ?Sized> Fetcher for AdamFetcher<'_, F> {
    async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError> {
        loop {
            let mut state = CAPTCHA_STATE.lock().await;
            if state.is_cleared {
                drop(state);

                let res = self.fetcher.fetch_raw(req.clone()).await;
                if Self::is_blocked_by_captcha(&res) {
                    let failure_time = std::time::Instant::now();
                    let mut state = CAPTCHA_STATE.lock().await;
                    state.is_cleared = false;
                    if !state.is_clearing {
                        state.is_clearing = true;
                        drop(state);

                        let resolve_res = self
                            .solve_captcha_and_retry(req.clone(), failure_time)
                            .await;

                        let mut state = CAPTCHA_STATE.lock().await;
                        state.is_clearing = false;
                        if resolve_res.is_ok() && !Self::is_blocked_by_captcha(&resolve_res) {
                            state.is_cleared = true;
                        }
                        CAPTCHA_NOTIFY.notify_waiters();
                        return resolve_res;
                    } else {
                        let notified = CAPTCHA_NOTIFY.notified();
                        drop(state);
                        notified.await;
                        continue;
                    }
                }
                return res;
            }

            if state.is_clearing {
                let notified = CAPTCHA_NOTIFY.notified();
                drop(state);
                notified.await;
                continue;
            }

            state.is_clearing = true;
            drop(state);

            let res = self.fetcher.fetch_raw(req.clone()).await;
            let resolve_res = if Self::is_blocked_by_captcha(&res) {
                let failure_time = std::time::Instant::now();
                self.solve_captcha_and_retry(req.clone(), failure_time)
                    .await
            } else {
                res
            };

            let mut state = CAPTCHA_STATE.lock().await;
            state.is_clearing = false;
            if resolve_res.is_ok() && !Self::is_blocked_by_captcha(&resolve_res) {
                state.is_cleared = true;
            }
            CAPTCHA_NOTIFY.notify_waiters();
            return resolve_res;
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::sync::Arc;
    use std::time::Duration;

    struct TimestampFetcher {
        events: Arc<tokio::sync::Mutex<Vec<(String, std::time::Instant)>>>,
    }

    #[async_trait::async_trait]
    impl Fetcher for TimestampFetcher {
        async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError> {
            let body_str = req.body.as_deref().unwrap_or("");
            let parsed: serde_json::Value = serde_json::from_str(body_str)
                .map_err(|e| ProviderError::ParsingError(e.to_string()))?;
            let target_url = parsed["url"].as_str().unwrap_or("http://example.com");
            let id = target_url.to_string();

            {
                let mut ev = self.events.lock().await;
                ev.push((format!("{id}_start"), std::time::Instant::now()));
            }
            tokio::time::sleep(Duration::from_millis(50)).await;
            {
                let mut ev = self.events.lock().await;
                ev.push((format!("{id}_finish"), std::time::Instant::now()));
            }

            let response_str = format!(
                r#"{{"status":"ok","message":"success","solution":{{"url":{:?},"response":"no captcha","userAgent":"Mozilla/5.0","cookies":[]}}}}"#,
                target_url
            );
            Ok(response_str.into_bytes())
        }
    }

    #[tokio::test]
    async fn test_gated_concurrency() {
        {
            let mut state = CAPTCHA_STATE.lock().await;
            state.is_cleared = false;
            state.is_clearing = false;
            state.last_request = None;
        }

        let events = Arc::new(tokio::sync::Mutex::new(Vec::new()));
        let base_fetcher = TimestampFetcher {
            events: events.clone(),
        };
        let adam_fetcher = AdamFetcher::new("http://localhost:8191", &base_fetcher);

        let req1 = Request {
            url: "http://example.com/1".to_string(),
            method: FetchMethod::GET,
            headers: vec![],
            body: None,
        };
        let req2 = Request {
            url: "http://example.com/2".to_string(),
            method: FetchMethod::GET,
            headers: vec![],
            body: None,
        };
        let req3 = Request {
            url: "http://example.com/3".to_string(),
            method: FetchMethod::GET,
            headers: vec![],
            body: None,
        };
        let req4 = Request {
            url: "http://example.com/4".to_string(),
            method: FetchMethod::GET,
            headers: vec![],
            body: None,
        };
        let req5 = Request {
            url: "http://example.com/5".to_string(),
            method: FetchMethod::GET,
            headers: vec![],
            body: None,
        };

        let res = tokio::join!(
            adam_fetcher.fetch_raw(req1),
            adam_fetcher.fetch_raw(req2),
            adam_fetcher.fetch_raw(req3),
            adam_fetcher.fetch_raw(req4),
            adam_fetcher.fetch_raw(req5),
        );

        assert!(res.0.is_ok(), "res.0 is Err: {:?}", res.0.err().unwrap());
        assert!(res.1.is_ok(), "res.1 is Err: {:?}", res.1.err().unwrap());
        assert!(res.2.is_ok(), "res.2 is Err: {:?}", res.2.err().unwrap());
        assert!(res.3.is_ok(), "res.3 is Err: {:?}", res.3.err().unwrap());
        assert!(res.4.is_ok(), "res.4 is Err: {:?}", res.4.err().unwrap());

        let evs = events.lock().await;
        assert_eq!(evs.len(), 10);

        // Find which request was the first/clearing request.
        let first_start_event = &evs[0];
        let first_id = first_start_event.0.strip_suffix("_start").unwrap();

        // Find the finish event of that first request.
        let first_finish_event = evs
            .iter()
            .find(|(name, _)| name == &format!("{}_finish", first_id))
            .unwrap();
        let first_finish_time = first_finish_event.1;

        // Verify that every other request started AFTER the first request finished.
        for (name, time) in evs.iter() {
            if name.ends_with("_start") && !name.starts_with(first_id) {
                assert!(
                    *time >= first_finish_time,
                    "Request {} started before the first request finished",
                    name
                );
            }
        }

        // Verify that the subsequent requests ran in parallel (their start times overlap).
        let subsequent_starts: Vec<_> = evs
            .iter()
            .filter(|(name, _)| name.ends_with("_start") && !name.starts_with(first_id))
            .collect();
        let subsequent_finishes: Vec<_> = evs
            .iter()
            .filter(|(name, _)| name.ends_with("_finish") && !name.starts_with(first_id))
            .collect();

        let mut overlap_count = 0;
        for (start_name, start_time) in &subsequent_starts {
            let start_id = start_name.strip_suffix("_start").unwrap();
            for (finish_name, finish_time) in &subsequent_finishes {
                let finish_id = finish_name.strip_suffix("_finish").unwrap();
                if start_id != finish_id && *start_time < *finish_time {
                    overlap_count += 1;
                }
            }
        }
        assert!(
            overlap_count > 0,
            "Subsequent requests did not execute in parallel!"
        );
    }

    #[tokio::test]
    async fn test_example_captcha() {
        let path = "tests/data/captcha.gif";
        if std::path::Path::new(path).exists() {
            let image_bytes = std::fs::read(path).unwrap();
            let model_path = "ddddocr_common.onnx";
            let mut session = ort::session::Session::builder()
                .unwrap()
                .commit_from_file(model_path)
                .unwrap();
            let result =
                super::AdamFetcher::<TimestampFetcher>::classify_image(&mut session, &image_bytes)
                    .unwrap();
            println!("TEST RESULT: result: {result}");
            // Verify it matches the correct output: "pe7mdf"
            assert_eq!(result, "pe7mdf");
        }
    }
}
