#![warn(clippy::pedantic)]

use geneagrab_plugin_core::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, ExtractImageResponse, ExtractRequest,
        ExtractResponse, IdentifyRequest, IdentifyResponse, PluginBase, PluginError, TileRequest,
        TileResponse,
    },
    data::PluginMetadata,
    export_plugin_base,
};
use regex::Regex;
use std::{borrow::Cow, sync::LazyLock};

pub mod extract;
pub mod image;

const PLUGIN_METADATA: PluginMetadata = PluginMetadata {
    id: Cow::Borrowed("ad06"),
    name: Cow::Borrowed("AD06"),
    description: Some(Cow::Borrowed(
        "A plugin for extracting data from Alpes-Maritimes (France) Departmental Archives.",
    )),
    author: Some(Cow::Borrowed("Evan Galli")),
    version: Some(Cow::Borrowed("1.0.0")),
    source_url: Some(Cow::Borrowed(
        "https://github.com/06Games/GeneaGrab/tree/v4/backend/plugins/ad06",
    )),
    suggested_websites: Cow::Borrowed(&[std::borrow::Cow::Borrowed("https://archives06.fr/")]),
};

static ARK_REGEX: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(r"/ark:/(?P<naan>[\w\.]+)(?:/(?P<document_id>[\w\.]+))?(?:/(?P<view_type>[\w\.]+))?(?:/(?P<sequence>\d+))?(?:/(?P<image_number>\d+))?")
        .expect("Invalid regex pattern")
});

struct PluginImpl;

impl PluginBase for PluginImpl {
    fn metadata((): ()) -> Result<PluginMetadata, PluginError> {
        Ok(PLUGIN_METADATA)
    }

    fn identify(req: IdentifyRequest) -> Result<IdentifyResponse, PluginError> {
        let url =
            url::Url::parse(&req.url).map_err(|e| PluginError::InvalidField(e.to_string()))?;

        let host = url.host_str();
        if host != Some("archives06.fr") || !url.path().starts_with("/ark:/") {
            return Err(PluginError::InvalidField("Not an AD06 URL".into()));
        }
        let host = host.unwrap();

        let captures = ARK_REGEX
            .captures(url.path())
            .ok_or(PluginError::InvalidField("Invalid ARK URL".into()))?;
        let name_assigning_authority_number =
            captures.name("naan").map_or("", |m| m.as_str()).to_string();
        let document_id = captures
            .name("document_id")
            .map_or("", |m| m.as_str())
            .to_string();
        let image_number = captures
            .name("image_number")
            .and_then(|m| m.as_str().parse::<u32>().ok())
            .unwrap_or(1);

        let ark_url =
            format!("https://{host}/ark:/{name_assigning_authority_number}/{document_id}");
        Ok(IdentifyResponse {
            registry_id: document_id,
            image_number: Some(image_number),
            ark_url: Some(ark_url),
        })
    }

    fn extract_registry(req: ExtractRequest) -> Result<ExtractResponse, PluginError> {
        crate::extract::extract_registry(&req)
    }

    fn is_image_missing_data(req: ExtractImageRequest) -> Result<Option<String>, PluginError> {
        Ok(crate::image::is_image_missing_data(&req))
    }

    fn extract_image(req: ExtractImageRequest) -> Result<ExtractImageResponse, PluginError> {
        crate::image::extract_image(&req)
    }

    fn fetch_tile(req: TileRequest) -> Result<TileResponse, PluginError> {
        crate::image::fetch_tile(&req)
    }

    fn download_image(req: DownloadRequest) -> Result<Option<TileResponse>, PluginError> {
        crate::image::download_image(req)
    }
}

export_plugin_base!(PluginImpl);
