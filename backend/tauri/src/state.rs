use geneagrab_core::errors::CoreError;
use sea_orm::DbConn;
use serde::Serialize;

pub struct AppState {
    pub db: DbConn,
}

#[derive(Serialize)]
pub struct CommandError(String);

impl From<CoreError> for CommandError {
    fn from(err: CoreError) -> Self {
        CommandError(err.to_string())
    }
}
