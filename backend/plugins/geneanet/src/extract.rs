use std::collections::HashSet;

use extism_pdk::{http, info, trace, Error, HttpRequest};
use geneagrab_plugin_core::{
    com_structs::{ExtractRequest, ExtractResponse},
    data::{Image, RegistryBuilder},
};
use regex::{Match, Regex};
use scraper::{Html, Selector};
use url::Url;

fn parse_geneanet_types(type_str: &str, is_civil_status: bool) -> HashSet<String> {
    let mut types = HashSet::new();
    let t = type_str.to_lowercase();

    if t.contains("naissances") {
        types.insert(
            if is_civil_status {
                "Birth"
            } else {
                "BirthTable"
            }
            .to_string(),
        );
    } else if t.contains("baptemes") || t.contains("baptêmes") {
        types.insert("Baptism".to_string());
    } else if t.contains("communions") {
        types.insert("Communion".to_string());
    } else if t.contains("confirmations") {
        types.insert("Confirmation".to_string());
    } else if t.contains("promesses de mariage") {
        types.insert("Banns".to_string());
    } else if t.contains("mariages") {
        types.insert(
            if is_civil_status {
                "Marriage"
            } else {
                "MarriageTable"
            }
            .to_string(),
        );
    } else if t.contains("décès") || t.contains("deces") {
        types.insert(
            if is_civil_status {
                "Death"
            } else {
                "DeathTable"
            }
            .to_string(),
        );
    } else if t.contains("sépultures") || t.contains("sepultures") || t.contains("inhumation") {
        types.insert("Burial".to_string());
    } else if t.contains("recensements") {
        types.insert("Census".to_string());
    } else if t.contains("etat des âmes") || t.contains("etat des ames") {
        types.insert("LiberStatutAnimarum".to_string());
    } else if t.contains("archives notariales") {
        types.insert("Notarial".to_string());
    } else if t.contains("registres matricules") {
        types.insert("Military".to_string());
    } else if t.contains("autres") || t.contains("archives privées") {
        types.insert("Other".to_string());
    } else {
        types.insert("Unknown".to_string());
    }

    types
}

fn fetch_string(url: &str) -> Result<String, Error> {
    let req = HttpRequest::new(url);
    let res = http::request::<()>(&req, None)?;
    info!("Sent request to {}, got status {}", url, res.status_code());
    trace!("Response body: {:?}", res.body());
    String::from_utf8(res.body()).map_err(|e| Error::msg(format!("Invalid UTF-8: {}", e)))
}

fn validate_match(reg_match: Option<Match>) -> Option<String> {
    reg_match
        .map(|m| m.as_str().trim().to_string())
        .map(|s| s.is_empty().then(|| None).unwrap_or(Some(s)))
        .flatten()
}

fn parse_viewer_page(builder: &mut RegistryBuilder, html: String) -> Result<(), Error> {
    let document = Html::parse_document(&html);

    let popup_selector = Selector::parse("#popup-informations")
        .map_err(|e| Error::msg(format!("Failed to parse popup selector: {}", e)))?;
    let popup_element = document
        .select(&popup_selector)
        .next()
        .ok_or(Error::msg("Popup information section not found in HTML"))?;
    let popup_html = popup_element.inner_html();

    let info_regex = Regex::new(r#"(?s)<p>(?:\[.*?\] - )?(?P<location>.*?) \((?P<locationDetails>.*?)\) - (?P<globalType>.*?)(?: \((?P<type>.*?)\))?(?: - .*)? *\| (?P<from>.*?) - (?P<to>.*?)</p>.*?<p>(?P<cote>.*?)</p>(?:.*?<p>(?P<notaire>.*?)</p>)?.*?<p class="no-margin-bottom">(?P<betterType>.*?)(?:\..*| -.*)?</p>.*?<p>(?P<note>.*?)</p>"#).unwrap();
    let caps = info_regex
        .captures(&popup_html)
        .ok_or(Error::msg("Failed to match registry info regex"))?;

    let mut place = Vec::new();
    if let Some(loc_details) = caps.name("locationDetails") {
        for part in loc_details.as_str().split(',').rev() {
            let trimmed = part.trim();
            if !trimmed.is_empty() {
                place.push(trimmed.to_string());
            }
        }
    }
    if let Some(loc) = caps.name("location") {
        place.push(loc.as_str().trim().to_string());
    }

    let district_regex = Regex::new(r#"Paroisse de (?P<location>.*?)(?:\.|-|<)"#).unwrap();
    if let Some(dist_caps) = district_regex.captures(&html) {
        if let Some(dist) = dist_caps.name("location") {
            place.push(dist.as_str().trim().to_string());
        }
    }
    builder.places(HashSet::from([place]));

    builder.date_from(validate_match(caps.name("from")));
    builder.date_to(validate_match(caps.name("to")));
    builder.archive_reference(validate_match(caps.name("cote")));
    builder.notes(validate_match(caps.name("note")));

    let global_type = caps
        .name("globalType")
        .map(|m| m.as_str().to_lowercase())
        .unwrap_or_default();
    let type_str = caps
        .name("type")
        .map(|m| m.as_str().to_lowercase())
        .unwrap_or_default();
    let better_type = caps.name("betterType").map(|m| m.as_str().to_lowercase());

    let combined_type = better_type.unwrap_or(type_str);
    let is_civil_status = global_type.contains("état civil");

    let mut registry_types = HashSet::new();
    for t in combined_type.split(',') {
        registry_types.extend(parse_geneanet_types(t.trim(), is_civil_status));
    }
    builder.registry_types(registry_types);

    Ok(())
}

fn parse_image_api(base_url: Url, api_json: String) -> Result<Vec<Image>, Error> {
    let json_array: Vec<serde_json::Value> = serde_json::from_str(&api_json)
        .map_err(|e| Error::msg(format!("Failed to parse API JSON: {}", e)))?;

    let mut images = Vec::new();
    for item in json_array {
        if let Some(page_num) = item.get("page").and_then(|p| p.as_u64()) {
            let manifest_url = item
                .get("image_base_url")
                .and_then(|u| u.as_str())
                .map(|u| base_url.join(u))
                .transpose()?
                .map(|u| u.to_string());
            let ark_url = item
                .get("image_route")
                .and_then(|u| u.as_str())
                .map(|u| base_url.join(u))
                .transpose()?
                .map(|s| s.to_string());

            images.push(Image {
                width: None,
                height: None,
                tile_size: None,
                manifest_url,
                ark_url,
                image_number: page_num as u32,
                name: None,       // Irrelevant
                date_range: None, // Could be retrieved from https://www.geneanet.org/registres/api/tool-panel/marqueur_date/view/{registry_id}/{page_num}?lang=fr
                notes: None, // Could be populated with transcribed text from https://www.geneanet.org/registres/api/tool-panel/transcription/view/{registry_id}/{page_num}?lang=fr
            });
        }
    }

    Ok(images)
}

fn extract_registry_internal<F>(req: ExtractRequest, fetcher: F) -> Result<ExtractResponse, Error>
where
    F: Fn(&str) -> Result<String, Error>,
{
    let view_url = format!(
        "https://www.geneanet.org/registres/view/{}",
        req.identified.registry_id
    );
    let parsed_view_url: Url = Url::parse(&view_url)?;

    let mut builder = RegistryBuilder::default();
    builder
        .source_id("geneanet".to_string())
        .registry_id(req.identified.registry_id.clone())
        .ark_url(Some(view_url.clone()));

    let html = fetcher(&view_url)?;
    parse_viewer_page(&mut builder, html)?;

    let registry = builder.build()?;

    let api_url = format!(
        "https://www.geneanet.org/registres/api/images/{}?min_page=1&max_page=999999",
        req.identified.registry_id
    );

    // Use the injected fetcher again
    let api_json = fetcher(&api_url)?;

    let images = parse_image_api(parsed_view_url, api_json)?;

    Ok(ExtractResponse { registry, images })
}

pub(crate) fn extract_registry(req: ExtractRequest) -> Result<ExtractResponse, Error> {
    extract_registry_internal(req, fetch_string)
}

#[cfg(test)]
mod tests {
    use super::*;
    use assert_json_diff::assert_json_include;
    use geneagrab_plugin_core::com_structs::IdentifyResponse;
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
    ) -> impl Fn(&str) -> Result<String, Error> {
        move |url: &str| -> Result<String, Error> {
            let matching_mock = mocks.iter().find(|mock| mock.url == url);

            if let Some(mock) = matching_mock {
                let file_path = base_dir.join(&mock.response_file);
                fs::read_to_string(&file_path).map_err(|e| {
                    Error::msg(format!(
                        "Mock failed to read file '{}' for URL {}: {}",
                        mock.response_file, url, e
                    ))
                })
            } else {
                Err(Error::msg(format!(
                    "No mock response found for URL: {}",
                    url
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
                req,
                mock_fetcher_factory(case.mocks, test_cases.dir.clone()),
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
                "[{}] parsed image count mismatch",
                description
            );
        }
    }
}
