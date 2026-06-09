use std::{borrow::Cow, collections::HashSet};

use geneagrab_plugin_core::{
    com_structs::{ExtractRequest, ExtractResponse, PluginError},
    data::{Image, RegistryBuilder, RegistryType},
    protocols::{
        fetchers::{CachedFlareSolverrFetcher, Fetcher},
        utils::validate_regex_match,
    },
};
use regex::Regex;
use scraper::{Html, Selector};
use url::Url;

fn parse_geneanet_types(type_str: &str, is_civil_status: bool) -> HashSet<RegistryType> {
    let mut types = HashSet::new();
    let t = deunicode::deunicode(type_str).to_lowercase();

    if t.contains("naissances") {
        types.insert(RegistryType::Vital(Cow::Borrowed(if is_civil_status {
            "Birth"
        } else {
            "BirthTable"
        })));
    } else if t.contains("baptemes") {
        types.insert(RegistryType::Vital(Cow::Borrowed("Baptism")));
    } else if t.contains("communions") {
        types.insert(RegistryType::Vital(Cow::Borrowed("Communion")));
    } else if t.contains("confirmations") {
        types.insert(RegistryType::Vital(Cow::Borrowed("Confirmation")));
    } else if t.contains("promesses de mariage") {
        types.insert(RegistryType::Union(Cow::Borrowed("Banns")));
    } else if t.contains("mariages") {
        types.insert(RegistryType::Union(Cow::Borrowed(if is_civil_status {
            "Marriage"
        } else {
            "MarriageTable"
        })));
    } else if t.contains("décès") || t.contains("deces") {
        types.insert(RegistryType::Mortality(Cow::Borrowed(if is_civil_status {
            "Death"
        } else {
            "DeathTable"
        })));
    } else if t.contains("sepultures") || t.contains("inhumation") {
        types.insert(RegistryType::Mortality(Cow::Borrowed("Burial")));
    } else if t.contains("recensements") {
        types.insert(RegistryType::Census(Cow::Borrowed("Census")));
    } else if t.contains("etat des ames") {
        types.insert(RegistryType::Census(Cow::Borrowed("Liber Statut Animarum")));
    } else if t.contains("archives notariales") {
        types.insert(RegistryType::Legal(Cow::Borrowed("Notarial")));
    } else if t.contains("registres matricules") {
        types.insert(RegistryType::Military(Cow::Borrowed("Military")));
    } else if !t.is_empty() {
        types.insert(RegistryType::Other(Cow::Owned(t)));
    } else {
        types.insert(RegistryType::Unknown);
    }

    types
}

fn parse_viewer_page(builder: &mut RegistryBuilder, html: &str) -> Result<(), PluginError> {
    let document = Html::parse_document(html);

    let popup_selector = Selector::parse("#popup-informations")
        .map_err(|e| PluginError::ParsingError(format!("Failed to parse popup selector: {e}")))?;
    let popup_element =
        document
            .select(&popup_selector)
            .next()
            .ok_or(PluginError::ParsingError(
                "Popup information section not found in HTML".into(),
            ))?;
    let popup_html = popup_element.inner_html();

    let info_regex = Regex::new(r#"(?s)<p class="text-small">.*?<p>(?:\[.*?\] - )?(?P<location>.*?) \((?P<locationDetails>.*?)\) - (?P<globalType>.*?)(?: \((?P<type>.*?)\))?(?: - .*)? *\| (?P<from>.*?) - (?P<to>.*?)</p>.*?<p>(?P<cote>.*?)</p>(?:.*?<p>(?P<notaire>.*?)</p>)?.*?<p class="no-margin-bottom">(?P<betterType>.*?)(?:\..*| -.*)?</p>.*?<p>(?P<note>.*?)</p>"#).unwrap();
    let caps = info_regex
        .captures(&popup_html)
        .ok_or(PluginError::ParsingError(
            "Failed to match registry info regex".into(),
        ))?;

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

    let district_regex = Regex::new(r"Paroisse de (?P<location>.*?)(?:\.|-|<)").unwrap();
    if let Some(dist_caps) = district_regex.captures(html) {
        if let Some(dist) = dist_caps.name("location") {
            place.push(dist.as_str().trim().to_string());
        }
    }
    builder.places(HashSet::from([place]));

    builder.date_from(validate_regex_match(caps.name("from")));
    builder.date_to(validate_regex_match(caps.name("to")));
    builder.archive_reference(validate_regex_match(caps.name("cote")));
    builder.notes(validate_regex_match(caps.name("note")));

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

fn parse_image_api(base_url: &Url, api_json: &str) -> Result<Vec<Image>, PluginError> {
    let json_array: Vec<serde_json::Value> =
        serde_json::from_str(api_json).map_err(|e| PluginError::ParsingError(e.to_string()))?;

    let mut images = Vec::new();
    for item in json_array {
        if let Some(page_num) = item.get("page").and_then(serde_json::Value::as_u64) {
            let page_num =
                u32::try_from(page_num).map_err(|e| PluginError::ParsingError(e.to_string()))?;
            let manifest_url = item
                .get("image_base_url")
                .and_then(|u| u.as_str())
                .map(|u| base_url.join(u))
                .transpose()
                .map_err(|e| PluginError::ParsingError(e.to_string()))?
                .map(|u| u.to_string());
            let ark_url = item
                .get("image_route")
                .and_then(|u| u.as_str())
                .map(|u| base_url.join(u))
                .transpose()
                .map_err(|e| PluginError::ParsingError(e.to_string()))?
                .map(|s| s.to_string());

            images.push(Image {
                width: None,
                height: None,
                tile_size: None,
                manifest_url,
                ark_url,
                image_number: page_num,
                name: None,       // Irrelevant
                date_range: None, // Could be retrieved from https://www.geneanet.org/registres/api/tool-panel/marqueur_date/view/{registry_id}/{page_num}?lang=fr
                notes: None, // Could be populated with transcribed text from https://www.geneanet.org/registres/api/tool-panel/transcription/view/{registry_id}/{page_num}?lang=fr
            });
        }
    }

    Ok(images)
}

/// Internal impl of `extract_registry`. A custom fetcher can be provided.
/// # Errors
/// If a request or the extraction fails
pub fn extract_registry_internal(
    req: &ExtractRequest,
    fetcher: &impl Fetcher,
) -> Result<ExtractResponse, PluginError> {
    let view_url = format!(
        "https://www.geneanet.org/registres/view/{}",
        req.identified.registry_id
    );
    let parsed_view_url: Url = Url::parse(&view_url)
        .map_err(|e| PluginError::InvalidField(format!("Invalid view URL: {e}")))?;

    let mut builder = RegistryBuilder::default();
    builder
        .source_id("geneanet".to_string())
        .registry_id(req.identified.registry_id.clone())
        .ark_url(Some(view_url.clone()));

    let html = fetcher.fetch(view_url.into())?;
    parse_viewer_page(&mut builder, &html)?;

    let registry = builder
        .build()
        .map_err(|e| PluginError::ParsingError(format!("Failed to build registry: {e}")))?;

    let api_url = format!(
        "https://www.geneanet.org/registres/api/images/{}?min_page=1&max_page=999999",
        req.identified.registry_id
    );

    // Use the injected fetcher again
    let api_json = fetcher.fetch(api_url.into())?;

    let images = parse_image_api(&parsed_view_url, &api_json)?;

    Ok(ExtractResponse { registry, images })
}

pub(crate) fn extract_registry(req: &ExtractRequest) -> Result<ExtractResponse, PluginError> {
    extract_registry_internal(req, &CachedFlareSolverrFetcher::default())
}
