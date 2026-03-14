use serde::{Deserialize, Serialize};
use std::borrow::Cow;

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct PluginMetadata {
    /** A unique identifier for the plugin. */
    pub id: Cow<'static, str>,

    /** The name of the plugin. */
    pub name: Cow<'static, str>,

    /** A description of the plugin. */
    pub description: Option<Cow<'static, str>>,

    /** The author of the plugin. */
    pub author: Option<Cow<'static, str>>,

    /** The version of the plugin. */
    pub version: Option<Cow<'static, str>>,

    /** A URL to the plugin's source code or homepage. */
    pub source_url: Option<Cow<'static, str>>,

    /** A list of websites that the plugin is designed to work with. */
    pub suggested_websites: Cow<'static, [Cow<'static, str>]>,
}
