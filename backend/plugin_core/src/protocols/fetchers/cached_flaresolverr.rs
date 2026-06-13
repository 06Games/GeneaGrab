use extism_pdk::{var, Json};

use crate::{
    com_structs::PluginError,
    data::http::Request,
    protocols::fetchers::{
        flaresolverr::{build_referer, needs_safe_request, Clearance},
        Fetcher, FlareSolverrFetcher, HostFetcher,
    },
};

pub struct CachedFlareSolverrFetcher<F: Fetcher> {
    flaresolverr_fetcher: FlareSolverrFetcher<F>,
}

impl Default for CachedFlareSolverrFetcher<HostFetcher> {
    fn default() -> Self {
        Self {
            flaresolverr_fetcher: FlareSolverrFetcher::from_config()
                .expect("Config should be readable"),
        }
    }
}

impl<F: Fetcher> Fetcher for CachedFlareSolverrFetcher<F> {
    fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, PluginError> {
        let host = build_referer(&req.url)?;
        let cache_key = format!("flaresolverr_{host}");

        if needs_safe_request(&req.url) {
            if let Ok(Some(cached_solution)) = var::get::<Json<Clearance>>(&cache_key) {
                // Try to fetch using the cached session
                if let Ok(data) = self
                    .flaresolverr_fetcher
                    .safe_fetch(cached_solution.0, req.clone())
                {
                    return Ok(data);
                }
            }
        }

        let solution = self.flaresolverr_fetcher.internal_fetch(req.clone())?;
        var::set(&cache_key, Json::<Clearance>(solution.clone().try_into()?))
            .map_err(|e| PluginError::LibraryError(format!("Couldn't store cache: {e}")))?;

        self.flaresolverr_fetcher.use_solution(req, solution)
    }
}
