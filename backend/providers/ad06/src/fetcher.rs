use base64::{engine::general_purpose, Engine};
use scraper::{Html, Selector};

use geneagrab_providers::{
    errors::ProviderError,
    data::Request,
    traits::Fetcher,
    protocols::fetchers::CachedFlareSolverrFetcher,
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

    fn has_captcha(res: &[u8]) -> Result<Option<Vec<u8>>, ProviderError> {
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
                            ProviderError::ParsingError(format!("Error decoding captcha image: {e}"))
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
}

#[async_trait::async_trait]
impl<'a, F: Fetcher + ?Sized> Fetcher for AdamFetcher<'a, F> {
    async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError> {
        let res = match self.fetcher.fetch_raw(req).await {
            Ok(data) => data,
            Err(ProviderError::NetworkError(msg))
                if msg.contains("ERR_CONNECTION_RESET")
                    || msg.to_ascii_lowercase().contains("connection reset") =>
            {
                return Err(ProviderError::NetworkError(String::from(
                    "Resource is behind a captcha",
                )));
            }
            Err(e) => return Err(e),
        };
        match Self::has_captcha(&res) {
            Ok(None) => Ok(res),
            Ok(Some(_captcha)) => {
                tracing::warn!("Captcha detected");
                // TODO: Solve the captcha
                Err(ProviderError::NetworkError(String::from(
                    "Resource is behind a captcha",
                )))
            }
            Err(e) => Err(e),
        }
    }
}
