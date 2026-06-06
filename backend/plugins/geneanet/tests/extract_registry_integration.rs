use std::{fs, path::Path};

use geneagrab_plugin_core::protocols::test_utils::{
    registry_extraction_integration_test, ExtractTestCase,
};
use geneagrab_plugin_geneanet::extract::extract_registry_internal;

fn test_single_registry_case(path: &Path) -> datatest_stable::Result<()> {
    let case_dir = path
        .parent()
        .expect("Test path should always have a parent directory");
    let file_content = fs::read_to_string(path)?;
    let case: ExtractTestCase = serde_json::from_str(&file_content)?;

    registry_extraction_integration_test(case, case_dir.to_path_buf(), &extract_registry_internal);
    Ok(())
}

datatest_stable::harness!(
    test_single_registry_case,
    "tests/data/extract_registry",
    r"(?:^|[/\\])test_def\.json$"
);
