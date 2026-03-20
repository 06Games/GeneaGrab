use std::{
    borrow::Cow,
    collections::{HashMap, HashSet},
};

use extism_pdk::{http, Error, HttpRequest};
use geneagrab_plugin_core::{
    com_structs::{
        ArkRequest, ExtractRequest, ExtractResponse, IdentifyRequest, IdentifyResponse, PluginBase,
        TileRequest, TileResponse,
    },
    data::{Image, PluginMetadata, Registry},
    export_plugin_base,
};

const PLUGIN_METADATA: PluginMetadata = PluginMetadata {
    id: Cow::Borrowed("iiif"),
    name: Cow::Borrowed("IIIF"),
    description: Some(Cow::Borrowed(
        "A plugin for extracting data from IIIF manifests.",
    )),
    author: Some(Cow::Borrowed("Evan Galli")),
    version: Some(Cow::Borrowed("1.0.0")),
    source_url: Some(Cow::Borrowed(
        "https://github.com/06Games/GeneaGrab/tree/v4/backend/plugins/iiif",
    )),
    suggested_websites: Cow::Borrowed(&[]),
};

struct PluginImpl;

impl PluginBase for PluginImpl {
    fn metadata(_: ()) -> Result<PluginMetadata, Error> {
        Ok(PLUGIN_METADATA)
    }

    fn identify(_req: IdentifyRequest) -> Result<IdentifyResponse, Error> {
        todo!()
    }

    fn extract_registry(req: ExtractRequest) -> Result<ExtractResponse, Error> {
        let http_req = HttpRequest::new(&req.url);
        let http_res = http::request::<()>(&http_req, None)?;

        let _body = http_res.body();

        let registry = Registry {
            source_id: "example_source_id".into(),
            registry_id: "example_registry_id".into(),
            archive_reference: Some("example_archive_reference".into()),
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

    fn generate_tile_request(_req: TileRequest) -> Result<TileResponse, Error> {
        todo!()
    }

    fn get_ark(_req: ArkRequest) -> Result<String, Error> {
        todo!()
    }
}

export_plugin_base!(PluginImpl);
