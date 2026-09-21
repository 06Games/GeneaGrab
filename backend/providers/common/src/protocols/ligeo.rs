use std::sync::LazyLock;

use regex::Regex;
use serde::{Deserialize, Serialize};
use url::Url;

use crate::IdentifyResponse;

static ARK_REGEX: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(r"/ark:/(?P<naan>[\w\.]+)(?:/(?P<document_id>[\w\.]+))?(?:/(?P<view_type>[\w\.]+))?(?:/(?P<sequence>\d+))?(?:/(?P<image_number>\d+))?")
        .expect("Invalid regex pattern")
});

pub fn parse_url(url: Url) -> Option<IdentifyResponse> {
    let captures = ARK_REGEX.captures(url.path())?;
    let name_assigning_authority_number =
        captures.name("naan").map_or("", |m| m.as_str()).to_string();
    let document_id = captures
        .name("document_id")
        .map_or("", |m| m.as_str())
        .to_string();

    // TODO: Support img:strImageBase format (but we need the manifest for that...)
    let image_number = captures
        .name("image_number")
        .map(|m| m.as_str())
        .and_then(|m| m.parse::<u32>().ok())
        .unwrap_or(1);

    let host = url.host_str()?;
    let ark_url = format!("https://{host}/ark:/{name_assigning_authority_number}/{document_id}");
    Some(IdentifyResponse {
        registry_id: document_id,
        image_number: Some(image_number),
        ark_url: Some(ark_url),
    })
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct LigeoClasseur {
    #[serde(rename = "strImageBase")]
    pub image_base: Option<String>,
    #[serde(rename = "strImageDir")]
    pub image_dir: Option<String>,
    #[serde(rename = "curTag")]
    pub tag: Option<String>,
    #[serde(rename = "curTagnum")]
    pub tag_number: Option<u32>,
    pub notice_id: Option<String>,
    pub ark: Option<String>,
    pub unitid: Option<String>,
    pub eadid: Option<String>,
}

impl TryFrom<&super::iiif::Canvas> for LigeoClasseur {
    type Error = &'static str;

    fn try_from(canvas: &super::iiif::Canvas) -> Result<Self, Self::Error> {
        let value = canvas
            .extra
            .get("ligeoClasseur")
            .ok_or("Canvas does not contain a ligeoClasseur extension")?;

        serde_json::from_value(value.clone())
            .map_err(|_| "Failed to deserialize LigeoClasseur from canvas data")
    }
}

#[derive(Debug, Deserialize, Serialize, Clone)]
pub struct LigeoCanvas {
    #[serde(rename = "ligeoPermalink")]
    pub permalink: Option<String>,
    #[serde(rename = "ligeoClasseur")]
    pub classeur: Option<LigeoClasseur>,
}

impl TryFrom<&super::iiif::Canvas> for LigeoCanvas {
    type Error = &'static str;

    fn try_from(canvas: &super::iiif::Canvas) -> Result<Self, Self::Error> {
        serde_json::from_value(canvas.extra.clone())
            .map_err(|_| "Failed to deserialize LigeoCanvas from canvas extra data")
    }
}
