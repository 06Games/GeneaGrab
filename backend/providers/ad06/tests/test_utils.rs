use geneagrab_providers::com_structs::{ExtractRequest, ExtractResponse, IdentifyResponse};
use geneagrab_providers::data::{FetchMethod, Request};
use geneagrab_providers::errors::ProviderError;
use geneagrab_providers::traits::Fetcher;
use geneagrab_providers::protocols::fetchers::FlareSolverrFetcher;
use jsonc_parser::ParseOptions;
use reqwest::blocking::Client;
use reqwest::header::{HeaderName, HeaderValue};
use reqwest::Method;
use serde::Deserialize;
use serde_json_assert::{assert_json_matches, CompareMode, Config};
use std::fs;
use std::path::PathBuf;
use std::str::FromStr;
use std::sync::Mutex;
use async_trait::async_trait;

pub struct TestCases<T> {
    pub dir: PathBuf,
    pub cases: Vec<T>,
}

#[derive(Deserialize)]
pub struct MockRequest {
    url: String,
    response_file: String,
}

/// Loads the test cases
///
/// # Panics
/// When the test data directory or the cases file cannot be found
/// When the cases file couldn't be parsed
#[must_use]
pub fn load_test_cases<T>(manifest_dir: &'static str, test_name: &str) -> TestCases<T>
where
    T: for<'de> Deserialize<'de>,
{
    let manifest_dir = PathBuf::from(manifest_dir);
    let test_data_dir = manifest_dir.join("tests").join("data").join(test_name);

    let cases_json = fs::read_to_string(test_data_dir.join("cases.json"))
        .expect("Failed to read cases.json manifest");

    let cases: Vec<T> = jsonc_parser::parse_to_serde_value(&cases_json, &ParseOptions::default())
        .expect("Failed to parse cases.json");

    TestCases {
        dir: test_data_dir,
        cases,
    }
}

static MUTEX: Mutex<()> = Mutex::new(());

#[allow(clippy::unnecessary_wraps, clippy::needless_pass_by_value)]
fn http_req(req: Request) -> Result<Vec<u8>, ProviderError> {
    let lock = MUTEX.lock().unwrap();
    let client = Client::new();

    let req_method = match req.method {
        FetchMethod::GET => Method::GET,
        FetchMethod::POST => Method::POST,
        FetchMethod::PUT => Method::PUT,
        FetchMethod::DELETE => Method::DELETE,
        FetchMethod::PATCH => Method::PATCH,
        FetchMethod::OPTIONS => Method::OPTIONS,
        FetchMethod::HEAD => Method::HEAD,
    };

    let mut builder = client.request(req_method, &req.url);

    for (key, value) in &req.headers {
        match (HeaderName::from_str(key), HeaderValue::from_str(value)) {
            (Ok(name), Ok(val)) => {
                builder = builder.header(name, val);
            }
            _ => {
                eprintln!("Warning: Invalid header key/value pair ({key}, {value})");
            }
        }
    }
    if let Some(body_content) = &req.body {
        builder = builder.body(body_content.clone());
    }
    let response = builder.send().unwrap();
    let text = response.error_for_status().unwrap().bytes().unwrap();

    drop(lock);

    Ok(text.into())
}

pub struct HttpReqFetcher;

#[async_trait]
impl Fetcher for HttpReqFetcher {
    async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError> {
        tokio::task::spawn_blocking(move || {
            http_req(req)
        })
        .await
        .map_err(|e| ProviderError::LibraryError(e.to_string()))?
    }
}

/// Implementation of the Fetcher trait that reads the response from a file
pub struct MockFetcher {
    pub mocks: Vec<MockRequest>,
    pub base_dir: PathBuf,
}

#[async_trait::async_trait]
impl Fetcher for MockFetcher {
    async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError> {
        let mock = self
            .mocks
            .iter()
            .find(|mock| mock.url == req.url)
            .unwrap_or_else(|| panic!("No mock response found for URL: {}", req.url));

        let file_path = self.base_dir.join(&mock.response_file);
        if !file_path.exists() {
            eprintln!("Mock file doesn't exist '{}'", mock.response_file);
            let res =
                FlareSolverrFetcher::new("http://localhost:8191", &HttpReqFetcher).fetch(req.clone()).await?;
            if let Err(e) = fs::write(&file_path, &res) {
                eprintln!(
                    "Couldn't save response from '{}' to '{}': {}",
                    req.url, mock.response_file, e
                );
            }
            return Ok(res.into_bytes());
        }

        fs::read(&file_path).map_err(|e| {
            panic!(
                "Mock failed to read file '{}' for URL {}: {}",
                mock.response_file, req.url, e
            )
        })
    }
}

#[derive(Deserialize)]
pub struct ExtractTestCase {
    pub request_url: String,
    pub registry_id: String,
    pub image_number: Option<u32>,
    pub ark_url: Option<String>,
    pub mocks: Vec<MockRequest>,
    pub expected_image_count: usize,
    pub expected_registry: serde_json::Value,
}

/// Run a test case for the registry extraction
///
/// # Panics
/// If the result isn't what was expected
pub fn registry_extraction_integration_test<F>(
    case: ExtractTestCase,
    base_dir: PathBuf,
    extract_fn: F,
) where
    F: FnOnce(&ExtractRequest, &MockFetcher) -> Result<ExtractResponse, ProviderError>,
{
    let req = ExtractRequest {
        identified: IdentifyResponse {
            registry_id: case.registry_id.clone(),
            image_number: case.image_number,
            ark_url: case.ark_url.clone(),
        },
        url: case.request_url.clone(),
    };

    let response = extract_fn(
        &req,
        &MockFetcher {
            mocks: case.mocks,
            base_dir,
        },
    )
    .expect("extract_registry_internal failed");

    assert_eq!(
        response.images.len(),
        case.expected_image_count,
        "parsed image count mismatch"
    );

    let config = Config::new(CompareMode::Inclusive).consider_array_sorting(false);
    assert_json_matches!(
        serde_json::to_value(&response.registry).unwrap(),
        serde_json::to_value(&case.expected_registry).unwrap(),
        &config
    );
}
