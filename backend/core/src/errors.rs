use thiserror::Error;

#[derive(Error, Debug)]
pub enum CoreError {
    #[error("Not found: {0}")]
    NotFound(String),

    #[error("Database error: {0}")]
    DbError(String),

    #[error("Invalid input: {0}")]
    InvalidInput(String),

    #[error("Unimplemented")]
    Unimplemented,

    #[error("Lock error: {0}")]
    LockError(String),

    #[error("Plugin error: {0}")]
    PluginError(String),

    #[error("Number error: {0}")]
    NumberError(#[from] std::num::TryFromIntError),

    #[error("{0}")]
    Other(String),
}
