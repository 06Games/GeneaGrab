use anyhow::Error;

use crate::{com_structs::*, data::PluginMetadata, define_extism_interface};

define_extism_interface! {
    trait PluginBase, host_ext HostPluginBase, guest_macro export_plugin_base {
        /* Returns metadata about the plugin.*/
        fn metadata(()) -> Result<PluginMetadata, Error>;

        /* Checks if the plugin can handle the URL and extracts basic info*/
        fn identify(IdentifyRequest) -> Result<IdentifyResponse, Error>;

        /* Parses the full metadata, dates, locations, and builds the Image array*/
        fn extract_registry(ExtractRequest) -> Result<ExtractResponse, Error>;

        /* Generates the HTTP URL for a single tile */
        fn generate_tile_request(TileRequest) -> Result<TileResponse, Error>;

        /* Generates the permalink for citing a specific page */
        fn get_ark(ArkRequest) -> Result<String, Error>;
    }
}
