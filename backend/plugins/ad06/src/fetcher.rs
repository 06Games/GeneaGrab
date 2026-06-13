use base64::{engine::general_purpose, Engine};
use extism_pdk::warn;
use geneagrab_plugin_core::{
    com_structs::PluginError,
    data::http::Request,
    protocols::fetchers::{CachedFlareSolverrFetcher, Fetcher, HostFetcher},
};
use scraper::{Html, Selector};

#[derive(Default)]
pub struct AdamFetcher {
    fetcher: CachedFlareSolverrFetcher<HostFetcher>,
}

impl Fetcher for AdamFetcher {
    fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, PluginError> {
        let res = self.fetcher.fetch_raw(req)?;
        match AdamFetcher::has_captcha(&res) {
            Ok(None) => Ok(res),
            Ok(Some(_captcha)) => {
                warn!("Captcha detected");
                // TODO: Solve the captcha
                Err(PluginError::NetworkError(String::from(
                    "Resource is behind a captcha",
                )))
            }
            Err(e) => Err(e),
        }
    }
}

impl AdamFetcher {
    fn has_captcha(res: &[u8]) -> Result<Option<Vec<u8>>, PluginError> {
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
                            PluginError::ParsingError(format!("Error decoding captcha image: {e}"))
                        })?;
                Ok(Some(image_bytes))
            } else {
                Err(PluginError::ParsingError(String::from(
                    "A captcha is present but the image to resolve couldn't be found",
                )))
            }
        } else {
            Ok(None)
        }
    }
}
