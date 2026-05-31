use geneagrab_plugin_core::{
    com_structs::{ExtractRequest, ExtractResponse, PluginError},
    data::{Image, RegistryBuilder, RegistryType},
    protocols::{
        fetchers::{Fetcher, HostFetcher},
        iiif::{Canvas, Manifest, Metadata},
        ligeo::LigeoClasseur,
        utils::validate_regex_match,
    },
};
use regex::Regex;
use scraper::{Html, Selector};
use std::fmt::Write;
use std::{borrow::Cow, collections::HashSet};

fn to_title_case(s: &str) -> String {
    let mut result = String::with_capacity(s.len());
    let mut capitalize_next = true;

    for c in s.chars() {
        if c.is_whitespace() || c == '-' || c == '\'' {
            result.push(c);
            capitalize_next = true;
        } else if capitalize_next {
            result.extend(c.to_uppercase());
            capitalize_next = false;
        } else {
            result.extend(c.to_lowercase());
        }
    }
    result
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

#[derive(Default)]
struct ParsedMetadata {
    location_primary: Option<String>,
    location_secondary: Option<String>,
    title: Option<String>,
    subtitle: Option<String>,
    author: Option<String>,
    date_from: Option<String>,
    date_to: Option<String>,
    notes_str: String,
    registry_types: HashSet<RegistryType>,
}

fn parse_manifest_metadata(metadata: &[Metadata]) -> ParsedMetadata {
    let mut parsed = ParsedMetadata::default();
    for meta in metadata {
        let value = deunicode::deunicode(&meta.to_string()).replace("<[^>]*>", "");
        match meta.label.as_str() {
            "Commune" | "Commune d’exercice du notaire" | "Lieu" | "Lieu d'édition" => {
                parsed.location_primary = Some(to_title_case(&value.to_lowercase()));
            }
            "Paroisse" | "Complément de lieu" => {
                parsed.location_secondary = Some(to_title_case(&value.to_lowercase()));
            }
            "Date" | "Date de l'acte" | "Année (s)" => {
                let parts: Vec<&str> = value.split('-').collect();
                parsed.date_from = parts.first().map(|s| s.trim().to_string());
                parsed.date_to = parts.last().map(|s| s.trim().to_string());
            }
            "Typologie" | "Type de document" | "Type d'acte" => {
                parsed.registry_types.extend(parse_types(&value));
            }
            "Analyse" => parsed.title = Some(value),
            "Folio" | "Volume" => parsed.subtitle = Some(value),
            "Auteur" | "Photographe" | "Sigillant" | "Bureau" | "Présentation du producteur" => {
                parsed.author = Some(value);
            }
            _ => {
                if !parsed.notes_str.is_empty() {
                    parsed.notes_str.push('\n');
                }
                let _ = writeln!(parsed.notes_str, "{}: {}", meta.label, value);
            }
        }
    }
    parsed
}

fn get_ead_pattern_and_types(eadid: &str) -> (Option<&'static str>, Vec<RegistryType>) {
    match eadid {
        "FRAD006_ETAT_CIVIL" => (
            Some(r"(?P<callnum>.+) +- +(?P<type>.*?) *?- *?\((?P<from>.+?)( à (?P<to>.+))?\)"),
            vec![],
        ),
        "FRAD006_CADASTRE_PLAN" => (
            Some(r"(?P<callnum>.+) +- +(?P<district>.*?) +- +(?P<subtitle>.*?) +- +(?P<from>.+?)"),
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
        "FRAD006_HYPOTHEQUES" => (
            Some(r"(?P<callnum>.+) +- +(?P<title>.+) +-"),
            vec![RegistryType::Legal(Cow::Borrowed("Catalogue"))],
        ),
        "FRAD006_HYPOTHEQUES_ACTES_TRANSLATIFS" => (
            Some(
                r"(?P<callnum>.+?) ?- +(?P<author>.+?) ?\.?- +(?P<title>.+?) ?- +(?P<from>.+?)(-(?P<to>.+))?$",
            ),
            vec![RegistryType::Legal(Cow::Borrowed("Engrossments"))],
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
        "FRAD006_NI" => (
            Some(r"(?P<callnum>NI .+?) *- *(?P<title>.*)\. *- *(?P<from>.*?) *- *(?P<to>.*?) *$"),
            vec![RegistryType::Other(Cow::Borrowed("OldArchives"))],
        ),
        "FRAD006_ARMOIRIES" => (
            Some(r"(?P<callnum>.+) +- +(?P<title>.+)"),
            vec![RegistryType::Other(Cow::Borrowed("Armorial"))],
        ),
        "FRAD006_OUVRAGES" => (
            Some(r"(?P<callnum>.+) +- +(?P<title>.+)"),
            vec![RegistryType::Other(Cow::Borrowed("Book"))],
        ),
        "FRAD006_ANNUAIRES" => (
            Some(r"(?P<title>.+)"),
            vec![RegistryType::Other(Cow::Borrowed("Directory"))],
        ),
        "FRAD006_PRESSE" => (
            Some(r"(?P<title>.+) \(\d*-\d*\), .*? +- +(?P<from>(\d|\/)+)(-(?P<to>(\d|\/)+))?"),
            vec![RegistryType::Other(Cow::Borrowed("Newspaper"))],
        ),
        "FRAD006_DELIBERATIONS_CONSEIL_GENERAL" => (
            Some(r"(?P<callnum>.+) +- +(?P<title>.+) +- +(?P<from>.+?)(-(?P<to>.+))?$"),
            vec![RegistryType::Other(Cow::Borrowed("Book"))],
        ),
        "FRAD006_11AV" => (
            Some(r"(?P<callnum>.+) +- +(?P<title>.+) +- +(?P<from>.+?)(-(?P<to>.+))?$"),
            vec![RegistryType::Other(Cow::Borrowed("Audiovisual"))],
        ),
        "FRAD006_10FI" => (
            Some(r"(?P<callnum>.+) +- +(?P<title>.+) +- +\((?P<from>.+?)-(?P<to>.+)\)"),
            vec![RegistryType::Other(Cow::Borrowed("Iconography"))],
        ),
        _ => (None, vec![]),
    }
}

fn enrich_from_classeur_and_html(
    parsed: &mut ParsedMetadata,
    canvas: &Canvas,
    label: &str,
    fetcher: &impl Fetcher,
) -> (Option<String>, Vec<String>) {
    let mut callnum = None;
    let mut collection = Vec::new();

    if let Ok(classeur) = LigeoClasseur::try_from(canvas) {
        if let Some(unitid) = &classeur.unitid {
            if !unitid.trim().is_empty() {
                callnum = Some(unitid.clone());
            }
        }

        let eadid = classeur.eadid.as_deref().unwrap_or("").to_ascii_uppercase();
        let (pattern, ead_types) = get_ead_pattern_and_types(&eadid);

        if let Some(pat) = pattern {
            if let Ok(re) = Regex::new(pat) {
                if let Some(caps) = re.captures(label) {
                    parsed.location_primary = parsed.location_primary.take().or_else(|| {
                        validate_regex_match(caps.name("city")).map(|s| to_title_case(&s))
                    });
                    parsed.location_secondary = parsed.location_secondary.take().or_else(|| {
                        validate_regex_match(caps.name("district")).map(|s| to_title_case(&s))
                    });

                    if let Some(t) = caps.name("type") {
                        parsed.registry_types.extend(parse_types(t.as_str()));
                    }

                    callnum = callnum.or_else(|| validate_regex_match(caps.name("callnum")));
                    parsed.title = parsed
                        .title
                        .take()
                        .or_else(|| validate_regex_match(caps.name("title")));
                    parsed.subtitle = parsed
                        .subtitle
                        .take()
                        .or_else(|| validate_regex_match(caps.name("subtitle")));
                    parsed.author = parsed
                        .author
                        .take()
                        .or_else(|| validate_regex_match(caps.name("author")));
                    parsed.date_from = parsed
                        .date_from
                        .take()
                        .or_else(|| validate_regex_match(caps.name("from")));
                    parsed.date_to = parsed
                        .date_to
                        .take()
                        .or_else(|| validate_regex_match(caps.name("to")))
                        .or_else(|| validate_regex_match(caps.name("from")));
                }
            }
        }
        parsed.registry_types.extend(ead_types);

        // Analysis page fetching
        if let Ok(analyse_html) = fetcher.fetch(canvas.id.clone().into()) {
            let document = Html::parse_document(&analyse_html);
            if let Ok(selector) = Selector::parse("#arc_ark_arianne ul > li > a > span") {
                collection = document
                    .select(&selector)
                    .map(|element| {
                        element
                            .text()
                            .collect::<String>()
                            .replace('.', "")
                            .trim()
                            .to_string()
                    })
                    .collect();
            }

            if eadid == "FRAD006_ETAT_CIVIL" && !collection.is_empty() {
                parsed.location_primary = collection.last().map(|s| to_title_case(s));
            } else if eadid == "FRAD006_3E" {
                // Title override for Notarial matching from-to
                if let (Some(t), Some(f), Some(to_d)) =
                    (&parsed.title, &parsed.date_from, &parsed.date_to)
                {
                    if t == &format!("{f}-{to_d}") {
                        if let Some(last_col) = collection.last() {
                            parsed.title = Some(last_col.clone());
                        }
                    }
                }
            }
        }
    }
    (callnum, collection)
}

fn extract_images(canvases: &[Canvas]) -> Vec<Image> {
    canvases
        .iter()
        .enumerate()
        .filter_map(|(i, canvas)| {
            canvas.images.first().and_then(|img_anno| {
                img_anno.resource.as_ref().map(|res| Image {
                    width: res.width,
                    height: res.height,
                    tile_size: None,
                    manifest_url: res.service.as_ref().map(|s| s.id.clone()),
                    ark_url: Some(canvas.id.clone()),
                    image_number: (i + 1) as u32,
                    name: canvas.label.clone(),
                    date_range: None,
                    notes: None,
                })
            })
        })
        .collect()
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

    let manifest_url = format!("{}/manifest", ark_url.trim_end_matches('/'));
    let manifest_json = fetcher.fetch(manifest_url.into())?;
    let manifest: Manifest = serde_json::from_str(&manifest_json)
        .map_err(|e| PluginError::ParsingError(format!("Failed to parse IIIF manifest: {e}")))?;

    let sequence = manifest
        .sequences
        .first()
        .ok_or_else(|| PluginError::ParsingError("No sequence in manifest".into()))?;

    // 1. Parse base metadata from the manifest
    let mut parsed = parse_manifest_metadata(&manifest.metadata);

    // 2. Enrich with specific Ligeo logic and HTML parsing if a canvas is available
    let (callnum, collection) = if let Some(canvas) = sequence.canvases.first() {
        let label = sequence.label.as_deref().unwrap_or("");
        enrich_from_classeur_and_html(&mut parsed, canvas, label, fetcher)
    } else {
        (None, Vec::new())
    };

    // 3. Extract all image resources from the canvases
    let images = extract_images(&sequence.canvases);

    // 4. Assemble the Registry Builder
    let mut builder = RegistryBuilder::default();
    builder
        .source_id("ad06".to_string())
        .registry_id(req.identified.registry_id.clone())
        .ark_url(Some(sequence.id.clone()))
        .archive_reference(callnum)
        .title(parsed.title)
        .subtitle(parsed.subtitle)
        .author(parsed.author)
        .date_from(parsed.date_from)
        .date_to(parsed.date_to)
        .collection(collection)
        .registry_types(parsed.registry_types);

    if !parsed.notes_str.is_empty() {
        builder.notes(Some(parsed.notes_str));
    }

    // Build places purely for geographical location sets
    let mut place = Vec::new();
    if let Some(loc) = parsed.location_primary {
        place.push(loc);
    }
    if let Some(dist) = parsed.location_secondary {
        place.push(dist);
    }
    if !place.is_empty() {
        builder.places(HashSet::from([place]));
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
    use geneagrab_plugin_core::protocols::test_utils::{
        load_test_cases, registry_extraction_integration_test, ExtractTestCase, TestCases,
    };

    use super::*;

    #[test]
    fn test_extract_registry_integration() {
        let test_cases: TestCases<ExtractTestCase> =
            load_test_cases(env!("CARGO_MANIFEST_DIR"), "extract_registry");
        registry_extraction_integration_test(test_cases, extract_registry_internal);
    }
}
