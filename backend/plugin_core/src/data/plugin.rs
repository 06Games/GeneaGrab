use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct PluginMetadata {
    /** A unique identifier for the plugin. */
    pub id: String,
    /** The name of the plugin. */
    pub name: String,
    /** A description of the plugin. */
    pub description: Option<String>,
    /** The author of the plugin. */
    pub author: Option<String>,
    /** The version of the plugin. */
    pub version: Option<String>,
    /** A URL to the plugin's source code or homepage. */
    pub source_url: Option<String>,
    /** A list of websites that the plugin is designed to work with. */
    pub suggested_websites: Vec<String>,
}
