use geneagrab_plugin_core::{
    com_structs::PluginError,
    data::http::Request,
    protocols::fetchers::{CachedFlareSolverrFetcher, Fetcher, HostFetcher},
};

#[derive(Default)]
pub struct AdamFetcher {
    fetcher: CachedFlareSolverrFetcher<HostFetcher>,
}

impl Fetcher for AdamFetcher {
    fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, PluginError> {
        let res = self.fetcher.fetch_raw(req)?;
        if AdamFetcher::has_captcha(&res) {
            Err(PluginError::NetworkError(String::from(
                "Resource is behind a captcha",
            )))
        } else {
            Ok(res)
        }
    }
}

impl AdamFetcher {
    fn has_captcha(res: &[u8]) -> bool {
        String::from_utf8_lossy(res).contains("captcha_audio")
    }
}
