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

    #[error("Provider error: {0}")]
    ProviderError(String),

    #[error("Number error: {0}")]
    NumberError(#[from] std::num::TryFromIntError),

    #[error("{0}")]
    Other(String),
}

impl From<geneagrab_providers::errors::ProviderError> for CoreError {
    fn from(err: geneagrab_providers::errors::ProviderError) -> Self {
        CoreError::ProviderError(err.to_string())
    }
}
