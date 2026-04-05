use crate::{com_structs::*, data::PluginMetadata, define_extism_interface};

define_extism_interface! {
    trait PluginBase, host_ext HostPluginBase, guest_macro export_plugin_base {
        /** Returns metadata about the plugin. */
        fn metadata(()) -> Result<PluginMetadata, PluginError>;

        /** Checks if the plugin can handle the URL and extracts basic info */
        fn identify(IdentifyRequest) -> Result<IdentifyResponse, PluginError>;

        /** Parses the full metadata, dates, locations, and builds the Image array */
        fn extract_registry(ExtractRequest) -> Result<ExtractResponse, PluginError>;

        /** Checks if the image is missing data and needs to be extracted */
        fn is_image_missing_data(ExtractImageRequest) -> Result<Option<String>, PluginError>;

        /** Parses the image metadata. */
        fn extract_image(ExtractImageRequest) -> Result<ExtractImageResponse, PluginError>;

        /** Gets a single tile */
        fn fetch_tile(TileRequest) -> Result<TileResponse, PluginError>;
    }
}
