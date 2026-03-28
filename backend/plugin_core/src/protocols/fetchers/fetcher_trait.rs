use derive_builder::Builder;

use crate::com_structs::PluginError;

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

/**
Represents an HTTP method for fetch requests.
Not all methods may be supported by all fetchers.
*/
#[derive(Debug, Clone, Copy)]
pub enum FetchMethod {
    GET,
    POST,
    PUT,
    DELETE,
    HEAD,
    OPTIONS,
    PATCH,
}

impl From<FetchMethod> for String {
    fn from(method: FetchMethod) -> Self {
        match method {
            FetchMethod::GET => "GET",
            FetchMethod::POST => "POST",
            FetchMethod::PUT => "PUT",
            FetchMethod::DELETE => "DELETE",
            FetchMethod::HEAD => "HEAD",
            FetchMethod::OPTIONS => "OPTIONS",
            FetchMethod::PATCH => "PATCH",
        }
        .to_string()
    }
}

/**
Information needed to perform an HTTP fetch.
*/
#[derive(Builder, Clone)]
pub struct Request {
    pub url: String,
    #[builder(default = "vec![]")]
    pub headers: Vec<(String, String)>,
    #[builder(default = "FetchMethod::GET")]
    pub method: FetchMethod,
    #[builder(default = "None")]
    pub body: Option<String>,
}

impl From<String> for Request {
    fn from(url: String) -> Self {
        Self {
            url,
            headers: vec![],
            method: FetchMethod::GET,
            body: None,
        }
    }
}
