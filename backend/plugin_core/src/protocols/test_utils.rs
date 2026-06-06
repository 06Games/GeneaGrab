use crate::com_structs::{ExtractRequest, ExtractResponse, IdentifyResponse, PluginError};
use crate::data::http::Request;
use crate::protocols::fetchers::Fetcher;
use jsonc_parser::ParseOptions;
use serde::Deserialize;
use serde_json_assert::{assert_json_matches, CompareMode, Config};
use std::fs;
use std::panic;
use std::path::PathBuf;

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
    let test_data_dir = manifest_dir.join("test_data").join(test_name);

    let cases_json = fs::read_to_string(test_data_dir.join("cases.json"))
        .expect("Failed to read cases.json manifest");

    let cases: Vec<T> = jsonc_parser::parse_to_serde_value(&cases_json, &ParseOptions::default())
        .expect("Failed to parse cases.json");

    TestCases {
        dir: test_data_dir,
        cases,
    }
}

/// Implementation of the Fetcher trait that reads the response from a file
pub struct MockFetcher {
    mocks: Vec<MockRequest>,
    base_dir: PathBuf,
}

impl Fetcher for MockFetcher {
    /// # Panics
    /// If the wanted url haven't been mocked
    fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, PluginError> {
        let mock = self
            .mocks
            .iter()
            .find(|mock| mock.url == req.url)
            .unwrap_or_else(|| panic!("No mock response found for URL: {}", req.url));

        let file_path = self.base_dir.join(&mock.response_file);
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
    request_url: String,
    registry_id: String,
    image_number: Option<u32>,
    ark_url: Option<String>,
    mocks: Vec<MockRequest>,
    expected_image_count: usize,
    expected_registry: serde_json::Value,
}

/// Run a test case for the registry extraction
///
/// # Panics
/// If the result isn't what was expected
pub fn registry_extraction_integration_test(
    case: ExtractTestCase,
    base_dir: PathBuf,
    extract_fn: &impl Fn(&ExtractRequest, &MockFetcher) -> Result<ExtractResponse, PluginError>,
) {
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

    let config = Config::new(CompareMode::Inclusive).consider_array_sorting(false);
    assert_json_matches!(
        serde_json::to_value(&response.registry).unwrap(),
        serde_json::to_value(&case.expected_registry).unwrap(),
        &config
    );
    assert_eq!(
        response.images.len(),
        case.expected_image_count,
        "parsed image count mismatch"
    );
}
