use thiserror::Error;

#[derive(Error, Debug, Clone)]
pub enum ProviderError {
    #[error("Invalid field: {0}")]
    InvalidField(String),

    #[error("Missing field: {0}")]
    MissingField(String),

    #[error("Network error: {0}")]
    NetworkError(String),

    #[error("Parsing error: {0}")]
    ParsingError(String),

    #[error("{0}")]
    LibraryError(String),
}
