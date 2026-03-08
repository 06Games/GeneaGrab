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

    #[error("{0}")]
    Other(String),
}
