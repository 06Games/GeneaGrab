mod cached_flaresolverr;
mod fetcher_trait;
mod flaresolverr;
mod host;
mod simple;

pub use cached_flaresolverr::CachedFlareSolverrFetcher;
pub use fetcher_trait::*;
pub use flaresolverr::FlareSolverrFetcher;
pub use host::HostFetcher;
pub use simple::SimpleFetcher;
