use geneagrab_plugin_core::{
    com_structs::{ExtractRequest, ExtractResponse, PluginError},
    protocols::fetchers::{Fetcher, SimpleFetcher},
};

fn extract_registry_internal(
    _req: &ExtractRequest,
    _fetcher: &impl Fetcher,
) -> Result<ExtractResponse, PluginError> {
    todo!();
}

pub(crate) fn extract_registry(req: &ExtractRequest) -> Result<ExtractResponse, PluginError> {
    extract_registry_internal(req, &SimpleFetcher {})
}

#[cfg(test)]
mod tests {
    use super::*;
    use assert_json_diff::assert_json_include;
    use geneagrab_plugin_core::com_structs::{IdentifyResponse, PluginError};
    use geneagrab_plugin_core::data::http::Request;
    use serde::Deserialize;
    use std::fs;
    use std::path::PathBuf;

    struct TestCases<T> {
        dir: PathBuf,
        cases: Vec<T>,
    }

    #[derive(Deserialize)]
    struct MockRequest {
        url: String,
        response_file: String,
    }

    #[derive(Deserialize)]
    struct TestCase {
        request_url: String,
        registry_id: String,
        image_number: Option<u32>,
        mocks: Vec<MockRequest>,
        expected_image_count: usize,
        expected_registry: serde_json::Value,
    }

    fn load_test_cases<T>(test_name: &str) -> TestCases<T>
    where
        T: for<'de> Deserialize<'de>,
    {
        let manifest_dir = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
        let test_data_dir = manifest_dir.join("test_data").join(test_name);

        let cases_json = fs::read_to_string(test_data_dir.join("cases.json"))
            .expect("Failed to read cases.json manifest");

        let cases: Vec<T> = serde_json::from_str(&cases_json).expect("Failed to parse cases.json");

        TestCases {
            dir: test_data_dir,
            cases,
        }
    }

    fn mock_fetcher_factory(
        mocks: Vec<MockRequest>,
        base_dir: PathBuf,
    ) -> impl Fn(Request) -> Result<Vec<u8>, PluginError> {
        move |req: Request| -> Result<Vec<u8>, PluginError> {
            let matching_mock = mocks.iter().find(|mock| mock.url == req.url);

            if let Some(mock) = matching_mock {
                let file_path = base_dir.join(&mock.response_file);
                fs::read(&file_path).map_err(|e| {
                    PluginError::NetworkError(format!(
                        "Mock failed to read file '{}' for URL {}: {}",
                        mock.response_file, req.url, e
                    ))
                })
            } else {
                Err(PluginError::NetworkError(format!(
                    "No mock response found for URL: {}",
                    req.url
                )))
            }
        }
    }

    #[test]
    fn test_extract_registry_integration() {
        let test_cases: TestCases<TestCase> = load_test_cases("extract_registry");

        for case in test_cases.cases {
            let description = format!(
                "Registry ID: {}, Image Number: {:?}",
                case.registry_id, case.image_number
            );

            let req = ExtractRequest {
                identified: IdentifyResponse {
                    registry_id: case.registry_id.clone(),
                    image_number: case.image_number,
                },
                url: case.request_url.clone(),
            };

            let result = extract_registry_internal(
                &req,
                &mock_fetcher_factory(case.mocks, test_cases.dir.clone()),
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
}
