use std::{fs, path::Path};
use tokio::runtime::Runtime;

use geneagrab_provider_ad06::extract::extract_registry_internal;
use test_utils::{registry_extraction_integration_test, ExtractTestCase};

mod test_utils;

fn test_single_registry_case(path: &Path) -> datatest_stable::Result<()> {
    let case_dir = path
        .parent()
        .expect("Test path should always have a parent directory");
    let file_content = fs::read_to_string(path)?;
    let case: ExtractTestCase = serde_json::from_str(&file_content)?;

    registry_extraction_integration_test(case, case_dir.to_path_buf(), |req, fetcher| {
        let rt = Runtime::new().unwrap();
        rt.block_on(extract_registry_internal(req, fetcher))
    });

    Ok(())
}

datatest_stable::harness! {
    { test = test_single_registry_case, root = "tests/data/extract_registry", pattern = r"(?:^|[/\\])test_def\.json$" },
}
