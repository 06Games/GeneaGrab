use geneagrab_plugin_core::{
    com_structs::{ExtractRequest, ExtractResponse, PluginError},
    data::{Image, RegistryBuilder, RegistryType},
    protocols::{
        fetchers::{Fetcher, SimpleFetcher},
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
    let mut builder = RegistryBuilder::default();
    builder
        .source_id("ad06".to_string())
        .registry_id(req.identified.registry_id.clone());

    let manifest_url = format!("{}/manifest", req.url.trim_end_matches('/'));
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

            let eadid = classeur.eadid.as_deref().unwrap_or("");
            let label = sequence.label.as_deref().unwrap_or("");

            // Determine Regex and Type based on EAD ID
            // Using a simple inline dispatch for Rusty flow.
            let (pattern, ead_types) = match eadid {
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
    extract_registry_internal(req, &SimpleFetcher {})
}
