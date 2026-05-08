#![warn(clippy::pedantic)]

use std::borrow::Cow;

use geneagrab_plugin_core::{
    com_structs::{
        DownloadRequest, ExtractImageRequest, ExtractImageResponse, ExtractRequest,
        ExtractResponse, IdentifyRequest, IdentifyResponse, PluginBase, PluginError, TileRequest,
        TileResponse,
    },
    data::PluginMetadata,
    export_plugin_base,
};

mod extract;
mod image;

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

struct PluginImpl;

impl PluginBase for PluginImpl {
    fn metadata((): ()) -> Result<PluginMetadata, PluginError> {
        Ok(PLUGIN_METADATA)
    }

    fn identify(_req: IdentifyRequest) -> Result<IdentifyResponse, PluginError> {
        todo!();
    }

    fn extract_registry(req: ExtractRequest) -> Result<ExtractResponse, PluginError> {
        crate::extract::extract_registry(&req)
    }

    fn is_image_missing_data(req: ExtractImageRequest) -> Result<Option<String>, PluginError> {
        crate::image::is_image_missing_data(req)
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
