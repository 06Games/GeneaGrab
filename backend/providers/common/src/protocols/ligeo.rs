use serde::{Deserialize, Serialize};

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
