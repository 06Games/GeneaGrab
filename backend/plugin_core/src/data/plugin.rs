use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct PluginMetadata {
    pub id: String,
    pub name: String,
}
