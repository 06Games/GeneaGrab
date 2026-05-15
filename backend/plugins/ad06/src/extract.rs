use geneagrab_plugin_core::{
    com_structs::{ExtractRequest, ExtractResponse, PluginError},
    data::{Image, RegistryBuilder, RegistryType},
    protocols::{
        fetchers::{Fetcher, HostFetcher},
        iiif::Manifest,
        ligeo::LigeoClasseur,
        utils::validate_regex_match,
    },
};
use regex::Regex;
use scraper::{Html, Selector};
use std::{borrow::Cow, collections::HashSet};

fn to_title_case(s: &str) -> String {
    let mut c = s.chars();
    match c.next() {
        None => String::new(),
        Some(f) => f.to_uppercase().collect::<String>() + c.as_str(),
    }
}

fn parse_types(type_str: &str) -> HashSet<RegistryType> {
    let mut types = HashSet::new();
    let parts: Vec<&str> = type_str.split(',').collect();

    for p in parts {
        let t = p.trim().to_lowercase();
        match t.as_str() {
            "naissances" => {
                types.insert(RegistryType::Vital(Cow::Borrowed("Birth")));
            }
            "tables décennales des naissances" | "tables alphabétiques des naissances" => {
                types.insert(RegistryType::Vital(Cow::Borrowed("BirthTable")));
            }
            "baptêmes" => {
                types.insert(RegistryType::Vital(Cow::Borrowed("Baptism")));
            }
            "tables des baptêmes" => {
                types.insert(RegistryType::Vital(Cow::Borrowed("BaptismTable")));
            }
            "confirmations" => {
                types.insert(RegistryType::Vital(Cow::Borrowed("Confirmation")));
            }
            "tables des communions" => {
                types.insert(RegistryType::Vital(Cow::Borrowed("Communion")));
            }
            "publications" | "publications de mariages" => {
                types.insert(RegistryType::Union(Cow::Borrowed("Banns")));
            }
            "mariages" => {
                types.insert(RegistryType::Union(Cow::Borrowed("Marriage")));
            }
            "tables des mariages"
            | "tables décennales des mariages"
            | "tables alphabétiques des mariages" => {
                types.insert(RegistryType::Union(Cow::Borrowed("MarriageTable")));
            }
            "divorces" => {
                types.insert(RegistryType::Union(Cow::Borrowed("Divorce")));
            }
            "décès" => {
                types.insert(RegistryType::Mortality(Cow::Borrowed("Death")));
            }
            "tables décennales des décès" | "tables alphabétiques des décès" => {
                types.insert(RegistryType::Mortality(Cow::Borrowed("DeathTable")));
            }
            "sépultures" | "sépultures des enfants décédés sans baptêmes" => {
                types.insert(RegistryType::Mortality(Cow::Borrowed("Burial")));
            }
            "tables des sépultures" => {
                types.insert(RegistryType::Mortality(Cow::Borrowed("BurialTable")));
            }
            "répertoire" => {
                types.insert(RegistryType::Legal(Cow::Borrowed("Catalogue")));
            }
            "inventaire" => {
                types.insert(RegistryType::Other(Cow::Borrowed("Inventory")));
            }
            "matrice cadastrale" => {
                types.insert(RegistryType::Land(Cow::Borrowed("CadastralMatrix")));
            }
            "état de section" => {
                types.insert(RegistryType::Land(Cow::Borrowed("CadastralSectionStates")));
            }
            _ => {
                types.insert(RegistryType::Unknown);
            }
        }
    }
    types
}

fn extract_registry_internal(
    req: &ExtractRequest,
    fetcher: &impl Fetcher,
) -> Result<ExtractResponse, PluginError> {
    let ark_url = req
        .identified
        .ark_url
        .clone()
        .ok_or(PluginError::MissingField("ark_url".to_string()))?;

    let mut builder = RegistryBuilder::default();
    builder
        .source_id("ad06".to_string())
        .registry_id(req.identified.registry_id.clone());

    let manifest_url = format!("{}/manifest", ark_url.trim_end_matches('/'));
    let manifest_json = fetcher.fetch(manifest_url.into())?;
    let manifest: Manifest = serde_json::from_str(&manifest_json)
        .map_err(|e| PluginError::ParsingError(format!("Failed to parse IIIF manifest: {e}")))?;

    let mut location_primary = None;
    let mut location_secondary = None;
    let mut registry_types = HashSet::new();

    for meta in &manifest.metadata {
        let value = deunicode::deunicode(&meta.to_string()).replace("<[^>]*>", "");
        match meta.label.as_str() {
            "Commune" | "Commune d’exercice du notaire" | "Lieu" | "Lieu d'édition" => {
                location_primary = Some(to_title_case(&value.to_lowercase()));
            }
            "Paroisse" | "Complément de lieu" => {
                location_secondary = Some(to_title_case(&value.to_lowercase()));
            }
            "Date" | "Date de l'acte" | "Année (s)" => {
                let parts: Vec<&str> = value.split('-').collect();
                builder.date_from(parts.first().map(|s| s.trim().to_string()));
                builder.date_to(parts.last().map(|s| s.trim().to_string()));
            }
            "Typologie" | "Type de document" | "Type d'acte" => {
                registry_types.extend(parse_types(&value));
            }
            "Analyse" => {
                builder.title(Some(value));
            }
            "Folio" | "Volume" => {
                builder.subtitle(Some(value));
            }
            "Auteur" | "Photographe" | "Sigillant" | "Bureau" | "Présentation du producteur" => {
                builder.author(Some(value));
            }
            _ => {
                // Append unknown to notes
                // TODO: Safely retrieve and mutate builder.notes
            }
        }
    }

    let sequence = manifest
        .sequences
        .first()
        .ok_or_else(|| PluginError::ParsingError("No sequence in manifest".into()))?;
    builder.ark_url(Some(sequence.id.clone()));

    let mut location_details = Vec::new();
    let mut images = Vec::new();

    if let Some(canvas) = sequence.canvases.first() {
        if let Ok(classeur) = LigeoClasseur::try_from(canvas) {
            builder.archive_reference(classeur.unitid.clone());

            let eadid = classeur.eadid.as_deref().unwrap_or("").to_ascii_uppercase();
            let label = sequence.label.as_deref().unwrap_or("");

            // Determine Regex and Type based on EAD ID
            // Using a simple inline dispatch for Rusty flow.
            let (pattern, ead_types) = match eadid.as_ref() {
                "FRAD006_ETAT_CIVIL" => (
                    Some(
                        r"(?P<callnum>.+) +- +(?P<type>.*?) *?- *?\((?P<from>.+?)( à (?P<to>.+))?\)",
                    ),
                    vec![],
                ),
                "FRAD006_CADASTRE_PLAN" => (
                    Some(
                        r"(?P<callnum>.+) +- +(?P<district>.*?) +- +(?P<subtitle>.*?) +- +(?P<from>.+?)",
                    ),
                    vec![RegistryType::Land(Cow::Borrowed("CadastralMap"))],
                ),
                "FRAD006_CADASTRE_MATRICE" => (
                    Some(r"(?P<callnum>.+?) +- +(?P<title>.*?) *?-"),
                    vec![RegistryType::Land(Cow::Borrowed("CadastralMatrix"))],
                ),
                "FRAD006_CADASTRE_ETAT_SECTION" => (
                    Some(r"(?P<callnum>.+) +- +(?P<title>.*?) *?-"),
                    vec![RegistryType::Land(Cow::Borrowed("CadastralSectionStates"))],
                ),
                "FRAD006_RECENSEMENT_POPULATION" => (
                    Some(r"(?P<city>.+) +- +(?P<from>.+)(, (?<district>.*))"),
                    vec![RegistryType::Census(Cow::Borrowed("Census"))],
                ),
                "FRAD006_REPERTOIRE_NOTAIRES" => (
                    Some(r"(?P<callnum>.+) +- +(?P<title>.+)"),
                    vec![RegistryType::Legal(Cow::Borrowed("Notarial"))],
                ),
                "FRAD006_3E" => (
                    Some(
                        r"(?P<callnum>3 E [\d ]+?) *- *(?P<title>.*)\. *- *(?P<from>.*?) *- *(?P<to>.*?) *$",
                    ),
                    vec![RegistryType::Legal(Cow::Borrowed("Notarial"))],
                ),
                "FRAD006_C" => (
                    Some(
                        r"(?P<callnum>C [\d ]+?) *- *(?P<title>.*)\. *- *(?P<from>.*?) *- *(?P<to>.*?) *$",
                    ),
                    vec![RegistryType::Other(Cow::Borrowed("OldArchives"))],
                ),
                _ => (None, vec![]),
            };

            if let Some(pat) = pattern {
                if let Ok(re) = Regex::new(pat) {
                    if let Some(caps) = re.captures(label) {
                        if location_primary.is_none() {
                            location_primary =
                                validate_regex_match(caps.name("city")).map(|s| to_title_case(&s));
                        }
                        if location_secondary.is_none() {
                            location_secondary = validate_regex_match(caps.name("district"))
                                .map(|s| to_title_case(&s));
                        }
                        if let Some(t) = caps.name("type") {
                            registry_types.extend(parse_types(t.as_str()));
                        }
                    }
                }
            }
            registry_types.extend(ead_types);

            // Analysis page fetching
            if let Ok(analyse_html) = fetcher.fetch(req.url.clone().into()) {
                let document = Html::parse_document(&analyse_html);
                if let Ok(selector) = Selector::parse("ul > li > a > span") {
                    for element in document.select(&selector) {
                        let text = element.text().collect::<String>().replace('.', "");
                        location_details.push(text.trim().to_string());
                    }
                }

                if eadid == "FRAD006_ETAT_CIVIL" && !location_details.is_empty() {
                    location_primary = location_details.last().map(|s| to_title_case(s));
                }
            }
        }
    }

    // Process Location sets
    let mut final_places = Vec::new();
    if let Some(loc) = location_primary {
        final_places.push(loc);
    }
    if let Some(dist) = location_secondary {
        final_places.push(dist);
    }
    builder.places(HashSet::from([final_places]));
    builder.registry_types(registry_types);

    // Populate Images
    for (i, canvas) in sequence.canvases.iter().enumerate() {
        if let Some(img_anno) = canvas.images.first() {
            if let Some(res) = &img_anno.resource {
                images.push(Image {
                    width: res.width,
                    height: res.height,
                    tile_size: None,
                    manifest_url: res.service.as_ref().map(|s| s.id.clone()),
                    ark_url: Some(canvas.id.clone()),
                    image_number: (i + 1) as u32,
                    name: canvas.label.clone(),
                    date_range: None,
                    notes: None,
                });
            }
        }
    }

    let registry = builder
        .build()
        .map_err(|e| PluginError::ParsingError(format!("Failed to build registry: {e}")))?;

    Ok(ExtractResponse { registry, images })
}

pub(crate) fn extract_registry(req: &ExtractRequest) -> Result<ExtractResponse, PluginError> {
    extract_registry_internal(req, &HostFetcher {})
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
        ark_url: Option<String>,
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
                    ark_url: case.ark_url,
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
