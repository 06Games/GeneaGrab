use crate::{com_structs::PluginError, data::http::Request};

pub trait Fetcher {
    fn fetch(&self, req: Request) -> Result<String, PluginError>;
}

impl<F> Fetcher for F
where
    F: Fn(Request) -> Result<String, PluginError>,
{
    fn fetch(&self, req: Request) -> Result<String, PluginError> {
        (self)(req)
    }
}
