use std::collections::{HashMap, HashSet};

use extism_pdk::*;
use geneagrab_plugin_core::{
    com_structs::{ExtractRequest, ExtractResponse},
    data::{Image, Registry},
};

#[plugin_fn]
pub fn extract_registry(Json(req): Json<ExtractRequest>) -> FnResult<Json<ExtractResponse>> {
    let http_req = HttpRequest::new(&req.url);
    let http_res = http::request::<()>(&http_req, None)?;

    let _body = http_res.body();

    let registry = Registry {
        source_id: "example_source_id".into(),
        registry_id: "example_registry_id".into(),
        archive_reference: "example_archive_reference".into(),
        registry_types: HashSet::new(),
        collection: vec![],
        manifest_url: None,
        ark_url: Some(req.url.clone()),
        title: Some("Example Title".into()),
        subtitle: Some("Example Subtitle".into()),
        author: Some("Example Author".into()),
        date_from: None,
        date_from_normalized: None,
        date_to: None,
        date_to_normalized: None,
        places: HashSet::new(),
        notes: None,
        extra: HashMap::new(),
    };
    let images: Vec<Image> = vec![];

    let res = ExtractResponse { registry, images };

    Ok(Json(res))
}
