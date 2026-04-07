#![allow(clippy::missing_errors_doc)]

use crate::{com_structs::PluginError, data::http::Request};

pub trait Fetcher {
    fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, PluginError>;

    fn fetch(&self, req: Request) -> Result<String, PluginError> {
        let bytes = self.fetch_raw(req)?;
        Ok(String::from_utf8_lossy(&bytes).to_string())
    }
}

impl<F> Fetcher for F
where
    F: Fn(Request) -> Result<Vec<u8>, PluginError>,
{
    fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, PluginError> {
        (self)(req)
    }
}
