use geneagrab_core::{errors::CoreError, plugins::PluginManager};
use sea_orm::DbConn;
use serde::Serialize;

pub struct AppState {
    pub db: DbConn,
    pub plugin_manager: PluginManager,
}

#[derive(Serialize)]
pub struct CommandError(String);

impl From<CoreError> for CommandError {
    fn from(err: CoreError) -> Self {
        log::error!("{}", err);
        CommandError(err.to_string())
    }
}
