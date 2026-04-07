use derive_builder::Builder;
use serde::{Deserialize, Serialize};
use strum::Display;

/**
Represents an HTTP method for fetch requests.
Not all methods may be supported by all fetchers.
*/
#[derive(Debug, Clone, Copy, Serialize, Deserialize, Display)]
pub enum FetchMethod {
    GET,
    POST,
    PUT,
    DELETE,
    HEAD,
    OPTIONS,
    PATCH,
}

/**
Information needed to perform an HTTP fetch.
*/
#[derive(Builder, Clone, Serialize, Deserialize)]
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
