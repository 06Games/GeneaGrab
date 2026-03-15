use std::{borrow::Cow, collections::{HashMap, HashSet}, sync::LazyLock};

use extism_pdk::{http, Error, HttpRequest};
use geneagrab_plugin_core::{
    com_structs::{
        ArkRequest, ExtractRequest, ExtractResponse, IdentifyRequest, IdentifyResponse, PluginBase,
        TileRequest, TileResponse,
    },
    data::{Image, PluginMetadata, Registry},
    export_plugin_base,
};
use regex::Regex;

const PLUGIN_METADATA: PluginMetadata = PluginMetadata {
    id: Cow::Borrowed("geneanet"),
    name: Cow::Borrowed("Geneanet"),
    description: Some(Cow::Borrowed("A plugin for extracting data from Geneanet.")),
    author: Some(Cow::Borrowed("Evan Galli")),
    version: Some(Cow::Borrowed("1.0.0")),
    source_url: Some(Cow::Borrowed(
        "https://github.com/06Games/GeneaGrab/tree/v4/backend/plugins/geneanet",
    )),
    suggested_websites: Cow::Borrowed(&[std::borrow::Cow::Borrowed("https://www.geneanet.org/")]),
};

static URL_REGEX: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(
        r"(?:idcollection=(?<col1>\d*).*page=(?<page1>\d*))|(?:/(?<col2>\d+)(?:\z|/(?<page2>\d*)))",
    )
    .expect("Invalid regex pattern")
});

fn parse_geneanet_types(type_str: &str, is_civil_status: bool) -> HashSet<String> {
    let mut types = HashSet::new();
    let t = type_str.to_lowercase();
    
    if t.contains("naissances") { 
        types.insert(if is_civil_status { "Birth" } else { "BirthTable" }.to_string()); 
    } else if t.contains("baptemes") || t.contains("baptêmes") { 
        types.insert("Baptism".to_string()); 
    } else if t.contains("communions") { 
        types.insert("Communion".to_string()); 
    } else if t.contains("confirmations") { 
        types.insert("Confirmation".to_string()); 
    } else if t.contains("promesses de mariage") { 
        types.insert("Banns".to_string()); 
    } else if t.contains("mariages") { 
        types.insert(if is_civil_status { "Marriage" } else { "MarriageTable" }.to_string()); 
    } else if t.contains("décès") || t.contains("deces") { 
        types.insert(if is_civil_status { "Death" } else { "DeathTable" }.to_string()); 
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
    String::from_utf8(res.body()).map_err(|e| Error::msg(format!("Invalid UTF-8: {}", e)))
}

struct PluginImpl;

impl PluginBase for PluginImpl {
    fn metadata(_: ()) -> Result<PluginMetadata, Error> {
        Ok(PLUGIN_METADATA)
    }

    fn identify(req: IdentifyRequest) -> Result<IdentifyResponse, Error> {
        let captures = URL_REGEX.captures(&req.url);

        let col_value = captures
            .as_ref()
            .and_then(|caps| caps.name("col1").or(caps.name("col2")))
            .map(|m| m.as_str())
            .filter(|s| !s.is_empty())
            .ok_or(Error::msg("Collection ID not found"))?;

        let image_number = captures
            .as_ref()
            .and_then(|caps| caps.name("page1").or(caps.name("page2")))
            .map(|m| m.as_str())
            .filter(|s| !s.is_empty())
            .and_then(|s| s.parse::<u32>().ok())
            .unwrap_or(1);

        Ok(IdentifyResponse {
            registry_id: col_value.into(),
            image_number: Some(image_number),
        })
    }

    fn extract_registry(req: ExtractRequest) -> Result<ExtractResponse, Error> {
        let view_url = format!("https://www.geneanet.org/registres/view/{}", req.identified.registry_id);
        let html = fetch_string(&view_url)?;

        let popup_regex = Regex::new(r#"(?s)id="popup-informations"[^>]*>(.*?)</div>"#)
            .map_err(|e| Error::msg(e.to_string()))?;
            
        let popup_html = popup_regex.captures(&html)
            .and_then(|c| c.get(1))
            .map(|m| m.as_str())
            .unwrap_or("");

        let mut places = HashSet::new();
        let mut registry_types = HashSet::new();
        let mut date_from = None;
        let mut date_to = None;
        let mut archive_reference = String::new();
        let mut notes = None;

        if !popup_html.is_empty() {
            let info_regex = Regex::new(r#"(?s)<p>(?:\[.*?\] - )?(?P<location>.*?) \((?P<locationDetails>.*?)\) - (?P<globalType>.*?)(?: \((?P<type>.*?)\))?(?: - .*)? *\| (?P<from>.*?) - (?P<to>.*?)</p>.*?<p>(?P<cote>.*?)</p>(?:.*?<p>(?P<notaire>.*?)</p>)?.*?<p class="no-margin-bottom">(?P<betterType>.*?)(?:\..*| -.*)?</p>.*?<p>(?P<note>.*?)</p>"#).unwrap();

            if let Some(caps) = info_regex.captures(popup_html) {
                if let Some(loc_details) = caps.name("locationDetails") {
                    for part in loc_details.as_str().split(',').rev() {
                        let trimmed = part.trim();
                        if !trimmed.is_empty() {
                            places.insert(trimmed.to_string());
                        }
                    }
                }
                if let Some(loc) = caps.name("location") {
                    places.insert(loc.as_str().trim().to_string());
                }

                let district_regex = Regex::new(r#"Paroisse de (?P<location>.*?)(?:\.|-|<)"#).unwrap();
                if let Some(dist_caps) = district_regex.captures(&html) {
                    if let Some(dist) = dist_caps.name("location") {
                        places.insert(dist.as_str().trim().to_string());
                    }
                }

                date_from = caps.name("from").map(|m| m.as_str().trim().to_string());
                date_to = caps.name("to").map(|m| m.as_str().trim().to_string());

                archive_reference = caps.name("cote").map(|m| m.as_str().trim().to_string()).unwrap_or_default();
                notes = caps.name("note").map(|m| m.as_str().trim().to_string());

                let global_type = caps.name("globalType").map(|m| m.as_str().to_lowercase()).unwrap_or_default();
                let type_str = caps.name("type").map(|m| m.as_str().to_lowercase()).unwrap_or_default();
                let better_type = caps.name("betterType").map(|m| m.as_str().to_lowercase());
                
                let combined_type = better_type.unwrap_or(type_str);
                let is_civil_status = global_type.contains("état civil");
                
                for t in combined_type.split(',') {
                    registry_types.extend(parse_geneanet_types(t.trim(), is_civil_status));
                }
            }
        }

        if archive_reference.is_empty() {
            archive_reference = req.identified.registry_id.clone();
        }

        let api_url = format!("https://www.geneanet.org/registres/api/images/{}?min_page=1&max_page=999999", req.identified.registry_id);
        let api_json = fetch_string(&api_url)?;
        
        let json_array: Vec<serde_json::Value> = serde_json::from_str(&api_json)
            .map_err(|e| Error::msg(format!("Failed to parse API JSON: {}", e)))?;

        let mut images = Vec::new();
        for item in json_array {
            if let Some(page_num) = item.get("page").and_then(|p| p.as_u64()) {
                let image_base = item.get("image_base_url").and_then(|u| u.as_str()).unwrap_or("").trim_end_matches('/');
                let ark_url = item.get("image_route").and_then(|u| u.as_str()).map(|s| s.to_string());
                
                images.push(Image {
                    width: None,
                    height: None,
                    tile_size: None,
                    manifest_url: Some(format!("https://www.geneanet.org{}/", image_base)),
                    ark_url,
                    image_number: page_num as u32,
                    name: None,
                    date_range: None,
                    notes: None,
                });
            }
        }

        let registry = Registry {
            source_id: "geneanet".to_string(),
            registry_id: req.identified.registry_id,
            archive_reference,
            registry_types,
            collection: vec![],
            manifest_url: None,
            ark_url: Some(view_url),
            title: None,
            subtitle: None,
            author: None,
            date_from,
            date_from_normalized: None,
            date_to,
            date_to_normalized: None,
            places,
            notes,
            extra: HashMap::new(),
        };

        Ok(ExtractResponse { registry, images })
    }

    fn generate_tile_request(_req: TileRequest) -> Result<TileResponse, Error> {
        todo!()
    }

    fn get_ark(_req: ArkRequest) -> Result<String, Error> {
        todo!()
    }
}

export_plugin_base!(PluginImpl);
