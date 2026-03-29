use std::{borrow::Cow, sync::LazyLock};

use geneagrab_plugin_core::{
    com_structs::{
        ArkRequest, ExtractImageRequest, ExtractImageResponse, ExtractRequest, ExtractResponse,
        IdentifyRequest, IdentifyResponse, PluginBase, PluginError, TileRequest, TileResponse,
    },
    data::PluginMetadata,
    export_plugin_base,
};
use regex::Regex;

mod extract;
mod image;

const PLUGIN_METADATA: PluginMetadata = PluginMetadata {
    id: Cow::Borrowed("geneanet"),
    name: Cow::Borrowed("Geneanet"),
    description: Some(Cow::Borrowed("A plugin for extracting data from Geneanet.")),
    author: Some(Cow::Borrowed("Evan Galli")),
    version: Some(Cow::Borrowed("1.0.0")),
    source_url: Some(Cow::Borrowed(
        "https://github.com/06Games/GeneaGrab/tree/v4/backend/plugins/geneanet",
    )),
    suggested_websites: Cow::Borrowed(&[std::borrow::Cow::Borrowed("https://www.geneanet.org/")]),
};

static URL_REGEX: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(
        r"(?:idcollection=(?<col1>\d*).*page=(?<page1>\d*))|(?:/(?<col2>\d+)(?:\z|/(?<page2>\d*)))",
    )
    .expect("Invalid regex pattern")
});

struct PluginImpl;

impl PluginBase for PluginImpl {
    fn metadata(_: ()) -> Result<PluginMetadata, PluginError> {
        Ok(PLUGIN_METADATA)
    }

    fn identify(req: IdentifyRequest) -> Result<IdentifyResponse, PluginError> {
        let captures = URL_REGEX.captures(&req.url);

        let col_value = captures
            .as_ref()
            .and_then(|caps| caps.name("col1").or(caps.name("col2")))
            .map(|m| m.as_str())
            .filter(|s| !s.is_empty())
            .ok_or(PluginError::InvalidField("Collection ID not found".into()))?;

        let image_number = captures
            .as_ref()
            .and_then(|caps| caps.name("page1").or(caps.name("page2")))
            .map(|m| m.as_str())
            .filter(|s| !s.is_empty())
            .and_then(|s| s.parse::<u32>().ok())
            .unwrap_or(1);

        Ok(IdentifyResponse {
            registry_id: col_value.into(),
            image_number: Some(image_number),
        })
    }

    fn extract_registry(req: ExtractRequest) -> Result<ExtractResponse, PluginError> {
        crate::extract::extract_registry(req)
    }

    fn is_image_missing_data(req: ExtractImageRequest) -> Result<Option<String>, PluginError> {
        crate::image::is_image_missing_data(req)
    }

    fn extract_image(req: ExtractImageRequest) -> Result<ExtractImageResponse, PluginError> {
        crate::image::extract_image(req)
    }

    fn generate_tile_request(req: TileRequest) -> Result<TileResponse, PluginError> {
        crate::image::generate_tile_request(req)
    }

    fn get_ark(req: ArkRequest) -> Result<String, PluginError> {
        req.image_ark.ok_or(PluginError::MissingField(
            "ARK URL not found for image".into(),
        ))
    }
}

export_plugin_base!(PluginImpl);
