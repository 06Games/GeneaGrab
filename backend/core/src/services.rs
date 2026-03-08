use crate::{errors::CoreError, models::{EventDetail, EventRow, ImageMeta, RegistryMeta, UserImageMeta}};
use std::collections::{HashMap, HashSet};

pub async fn fetch_registry_meta(id: String) -> Result<RegistryMeta, CoreError> {
    log::info!("fetch_registry_meta called with id: {}", id);
    let mut source_types = HashSet::new();
    source_types.insert("Birth".into());
    source_types.insert("Marriage".into());
    source_types.insert("Death".into());

    Ok(RegistryMeta {
        source_id: id,
        archive_reference: "5 Mi 1/342 (core stub)".into(),
        source_types,
        town: "Brignoles".into(),
        repository_url: "https://archives.var.fr".into(),
        total_images: 348,
    })
}

pub async fn fetch_image_meta(_registry_id: String, image_id: u32) -> Result<ImageMeta, CoreError> {
    log::info!("fetch_image_meta called for image {}", image_id);
    let mut act_types = HashMap::new();
    act_types.insert("Birth".into(), 2);
    act_types.insert("Marriage".into(), 1);

    Ok(ImageMeta {
        name: None,
        date_range: Some("1793-11-01 to 1793-11-30".into()),
        notes: Some("Stub data from core backend.".into()),
        image_number: image_id,
        act_types,
    })
}

pub async fn save_image_meta(_registry_id: String, _image_id: u32, _meta: UserImageMeta) -> Result<(), CoreError> {
    log::info!("save_image_meta called");
    // Persist via DB in the future
    Ok(())
}

pub async fn fetch_event_rows(_registry_id: String) -> Result<Vec<EventRow>, CoreError> {
    log::info!("fetch_event_rows called");
    Ok(vec![
        EventRow {
            event_id: 1,
            date: "03 Frim. II".into(),
            event_type: "Birth".into(),
            title: "MARTIN, Jean-Baptiste".into(),
        },
        EventRow {
            event_id: 3,
            date: "05 Frim. II".into(),
            event_type: "Marriage".into(),
            title: "ARNAUD, Pierre ∞ BLANC".into(),
        },
    ])
}

pub async fn fetch_event(event_id: u32) -> Result<EventDetail, CoreError>  {
    log::info!("fetch_event called for {}", event_id);
    if event_id == 3 {
        Ok(EventDetail {
            event_id: 3,
            date: "05 Frimaire An II".into(),
            date_normalized: "1793-11-25".into(),
            event_type: "Marriage".into(),
            title: "Marriage ARNAUD, Pierre ∞ BLANC".into(),
            act_number: "47".into(),
            page: "12r".into(),
            image_number: "12".into(),
            town: "Brignoles".into(),
            parish: "".into(),
            hamlet: "".into(),
            transcription_text: "".into(),
            notes: "Notes loaded natively from core backend.".into(),
            people: vec![],
        })
    } else {
        Err(CoreError::NotFound(format!("Event with ID {} not found", event_id)))
    }
}

pub async fn save_act(_event: EventDetail) -> Result<(), CoreError> {
    log::info!("save_act called");
    // Persist via DB in the future
    Ok(())
}
