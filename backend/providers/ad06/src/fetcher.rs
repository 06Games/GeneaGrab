use base64::{engine::general_purpose, Engine};
use scraper::{Html, Selector};

use geneagrab_providers::{
    data::{FetchMethod, Request},
    errors::ProviderError,
    protocols::fetchers::{build_referer, CachedFlareSolverrFetcher},
    traits::Fetcher,
};

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

    fn get_captcha(res: &[u8]) -> Result<Option<Vec<u8>>, ProviderError> {
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
}

#[async_trait::async_trait]
impl<F: Fetcher + ?Sized> Fetcher for AdamFetcher<'_, F> {
    async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError> {
        let first_attempt = self.fetcher.fetch_raw(req.clone()).await;

        if Self::is_blocked_by_captcha(&first_attempt) {
            static ROOT_REQUEST_LOCK: tokio::sync::Mutex<Option<std::time::Instant>> =
                tokio::sync::Mutex::const_new(None);

            let mut last_request = ROOT_REQUEST_LOCK.lock().await;
            let need_request = match *last_request {
                Some(instant) => instant.elapsed() > std::time::Duration::from_secs(10),
                None => true,
            };

            if need_request {
                tracing::warn!("Detected captcha challenge or connection reset. Requesting host root to trigger captcha...");
                let host_url = build_referer(&req.url)?;
                let root_req = Request {
                    url: host_url,
                    method: FetchMethod::GET,
                    headers: vec![],
                    body: None,
                };

                match self.fetcher.fetch_raw(root_req).await {
                    Ok(root_response) => {
                        *last_request = Some(std::time::Instant::now());
                        // Extract the captcha image if present on the root response
                        if let Ok(Some(_captcha_bytes)) = Self::get_captcha(&root_response) {
                            tracing::info!(
                                "Captcha challenge image successfully extracted from root response"
                            );
                            // TODO: Solve the captcha
                        } else {
                            tracing::error!("Found no captcha inside the response");
                        }
                    }
                    Err(e) => {
                        tracing::error!("Failed to request host root for captcha trigger: {:?}", e);
                    }
                }
            }

            // Retry the original request
            let second_attempt = self.fetcher.fetch_raw(req).await;
            if Self::is_blocked_by_captcha(&second_attempt) {
                if let Ok(data) = &second_attempt {
                    // Propagate the parsing error if the captcha page is broken/missing image
                    Self::get_captcha(data)?;
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
