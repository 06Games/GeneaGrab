use sea_orm::DbConn;
use serde::Serialize;

pub struct AppState {
    pub db: DbConn,
}

#[derive(Serialize)]
pub struct CommandError(pub String);

impl<T: ToString> From<T> for CommandError {
    fn from(err: T) -> Self {
        let err = err.to_string();
        tracing::error!("{err}");
        CommandError(err)
    }
}
