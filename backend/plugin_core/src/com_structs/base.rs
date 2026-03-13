use anyhow::Error;

use crate::{
    com_structs::{ExtractRequest, ExtractResponse},
    data::PluginMetadata,
    define_extism_interface,
};

define_extism_interface! {
    trait PluginBase, host_ext HostPluginBase, guest_macro export_plugin_base {
        fn metadata(()) -> Result<PluginMetadata, Error>;
        fn extract_registry(ExtractRequest) -> Result<ExtractResponse, Error>;
    }
}
