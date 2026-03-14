use std::collections::{HashMap, HashSet};

use extism_pdk::{http, Error, HttpRequest};
use geneagrab_plugin_core::{
    com_structs::{ExtractRequest, ExtractResponse, PluginBase},
    data::{Image, PluginMetadata, Registry},
    export_plugin_base,
};

struct PluginImpl;

impl PluginBase for PluginImpl {
    fn extract_registry(req: ExtractRequest) -> Result<ExtractResponse, Error> {
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

        Ok(res)
    }

    fn metadata(_: ()) -> Result<PluginMetadata, Error> {
        Ok(PluginMetadata {
            id: "iiif".into(),
            name: "IIIF".into(),
            description: Some("A plugin for extracting data from IIIF manifests.".into()),
            author: Some("Evan Galli".into()),
            version: Some("1.0.0".into()),
            source_url: Some(
                "https://github.com/06Games/GeneaGrab/tree/v4/backend/plugins/iiif".into(),
            ),
            suggested_websites: vec![],
        })
    }
}

export_plugin_base!(PluginImpl);
