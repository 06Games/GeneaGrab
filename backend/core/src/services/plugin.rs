use std::collections::HashMap;
use sea_orm::{ColumnTrait, DbConn, EntityTrait, QueryFilter, Select};
use crate::{db_entries::plugin_settings, errors::CoreError};

pub async fn get_plugin_config(
    db: &DbConn,
    plugin_id: &str,
) -> Result<HashMap<String, String>, CoreError> {
    let query: Select<plugin_settings::Entity> =
        plugin_settings::Entity::find().filter(plugin_settings::Column::PluginId.eq(plugin_id));
    Ok(query
        .all(db)
        .await
        .map_err(|e| CoreError::Other(format!("DB error: {e}")))?
        .into_iter()
        .map(|settings| (settings.key, settings.value))
        .collect::<HashMap<String, String>>())
}
