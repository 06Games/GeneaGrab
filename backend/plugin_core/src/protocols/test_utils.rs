use crate::com_structs::{ExtractRequest, ExtractResponse, IdentifyResponse, PluginError};
use crate::data::http::Request;
use crate::protocols::fetchers::Fetcher;
use assert_json_diff::assert_json_include;
use serde::Deserialize;
use std::fs;
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

    let cases: Vec<T> = serde_json::from_str(&cases_json).expect("Failed to parse cases.json");

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
        let matching_mock = self.mocks.iter().find(|mock| mock.url == req.url);

        if let Some(mock) = matching_mock {
            let file_path = self.base_dir.join(&mock.response_file);
            fs::read(&file_path).map_err(|e| {
                panic!(
                    "Mock failed to read file '{}' for URL {}: {}",
                    mock.response_file, req.url, e
                )
            })
        } else {
            panic!("No mock response found for URL: {}", req.url);
        }
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

/// Run integration tests for the registry extraction
///
/// # Panics
/// If the result isn't what was expected
pub fn registry_extraction_integration_test(
    test_cases: TestCases<ExtractTestCase>,
    extract_fn: impl Fn(&ExtractRequest, &MockFetcher) -> Result<ExtractResponse, PluginError>,
) {
    for case in test_cases.cases {
        let description = format!(
            "Registry ID: {}, Image Number: {:?}",
            case.registry_id, case.image_number
        );

        let req = ExtractRequest {
            identified: IdentifyResponse {
                registry_id: case.registry_id.clone(),
                image_number: case.image_number,
                ark_url: case.ark_url,
            },
            url: case.request_url.clone(),
        };

        let result = extract_fn(
            &req,
            &MockFetcher {
                mocks: case.mocks,
                base_dir: test_cases.dir.clone(),
            },
        );

        assert!(
            result.is_ok(),
            "[{}] extract_registry_internal failed: {:?}",
            description,
            result.err()
        );

        let response = result.unwrap();

        assert_json_include!(
            actual: serde_json::to_value(&response.registry).unwrap(),
            expected: serde_json::to_value(&case.expected_registry).unwrap()
        );
        assert_eq!(
            response.images.len(),
            case.expected_image_count,
            "[{description}] parsed image count mismatch"
        );
    }
}
