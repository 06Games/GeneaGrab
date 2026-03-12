use extism_pdk::*;
use serde::{Deserialize, Serialize};

#[derive(Deserialize)]
pub struct ExtractRequest {
    pub url: String,
}

// TODO
#[derive(Serialize)]
pub struct ExtractResponse {
    pub archive_reference: String,
    pub title: Option<String>,
    pub total_images: u32
}

#[plugin_fn]
pub fn extract_registry(Json(req): Json<ExtractRequest>) -> FnResult<Json<ExtractResponse>> {
    // Extism allows making HTTP calls directly from the WASM guest!
    // The host must allow the domain in its manifest configuration.
    let http_req = HttpRequest::new(&req.url);
    let http_res = http::request::<()>(&http_req, None)?;

    // Parse your IIIF/Ligeo/Bach manifest from `http_res.body()`
    let _body = http_res.body();

    let res = ExtractResponse {
        archive_reference: format!("Extracted from {}", req.url),
        title: Some("Registry Name".into()),
        total_images: 42
    };

    Ok(Json(res))
}
