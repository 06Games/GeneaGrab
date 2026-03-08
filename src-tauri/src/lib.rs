use serde::{Deserialize, Serialize};
use std::collections::{HashMap, HashSet};

// --- Models ---
// These structs directly map to the frontend TypeScript interfaces.

#[derive(Debug, Serialize, Deserialize)]
pub struct RegistryMeta {
    pub source_id: String,
    pub archive_reference: String,
    pub source_types: HashSet<String>,
    pub town: String,
    pub repository_url: String,
    pub total_images: u32,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct UserImageMeta {
    pub name: Option<String>,
    pub date_range: Option<String>,
    pub notes: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ImageMeta {
    pub name: Option<String>,
    pub date_range: Option<String>,
    pub notes: Option<String>,
    pub image_number: u32,
    pub act_types: HashMap<String, u32>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct EventRow {
    pub event_id: u32,
    pub date: String,
    pub event_type: String,
    pub title: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct PersonEntry {
    pub person_id: String,
    pub role: String,
    pub first_name: String,
    pub last_name: String,
    pub sex: String,
    pub title: String,
    pub age: String,
    pub is_deceased: bool,
    pub occupation: String,
    pub origin_place: String,
    pub residence_place: String,
    pub sequence_number: String,
    pub notes: String,
    pub relationship_type: String,
    pub relationship_to: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct EventDetail {
    pub event_id: u32,
    pub date: String,
    pub date_normalized: String,
    pub event_type: String,
    pub title: String,
    pub act_number: String,
    pub page: String,
    pub image_number: String,
    pub town: String,
    pub parish: String,
    pub hamlet: String,
    pub transcription_text: String,
    pub notes: String,
    pub people: Vec<PersonEntry>,
}

// --- Commands (Stubs) ---

#[tauri::command]
fn get_registry_meta(id: String) -> Result<RegistryMeta, String> {
    log::info!("get_registry_meta called with id: {}", id);
    let mut source_types = HashSet::new();
    source_types.insert("Birth".into());
    source_types.insert("Marriage".into());
    source_types.insert("Death".into());

    Ok(RegistryMeta {
        source_id: id,
        archive_reference: "5 Mi 1/342 (Tauri stub)".into(),
        source_types,
        town: "Brignoles".into(),
        repository_url: "https://archives.var.fr".into(),
        total_images: 348,
    })
}

#[tauri::command]
fn get_image_meta(registry_id: String, image_id: u32) -> Result<ImageMeta, String> {
    log::info!("get_image_meta called: registry {} image {}", registry_id, image_id);
    let mut act_types = HashMap::new();
    act_types.insert("Birth".into(), 2);
    act_types.insert("Marriage".into(), 1);

    Ok(ImageMeta {
        name: None,
        date_range: Some("1793-11-01 to 1793-11-30".into()),
        notes: Some("Stub data from Tauri backend.".into()),
        image_number: image_id,
        act_types,
    })
}

#[tauri::command]
fn save_image_meta(registry_id: String, image_id: u32, meta: UserImageMeta) -> Result<(), String> {
    log::info!("save_image_meta called: registry {} image {} meta {:?}", registry_id, image_id, meta);
    // Add logic here to persist to the database via SeaORM
    Ok(())
}

#[tauri::command]
fn get_event_rows(registry_id: String) -> Result<Vec<EventRow>, String> {
    log::info!("get_event_rows called for {}", registry_id);
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
        }
    ])
}

#[tauri::command]
fn get_event_detail(event_id: u32) -> Result<Option<EventDetail>, String> {
    log::info!("get_event_detail called for {}", event_id);
    if event_id == 3 {
        Ok(Some(EventDetail {
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
            notes: "Notes loaded natively from Tauri backend!".into(),
            people: vec![],
        }))
    } else {
        Ok(None)
    }
}

#[tauri::command]
fn save_act(event: EventDetail) -> Result<(), String> {
    log::info!("save_act called for event {:?}", event.event_id);
    // Add logic here to persist to the database via SeaORM
    Ok(())
}

// --- Main Application Setup ---

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .setup(|app| {
            if cfg!(debug_assertions) {
                app.handle().plugin(
                    tauri_plugin_log::Builder::default()
                        .level(log::LevelFilter::Info)
                        .build(),
                )?;
            }
            Ok(())
        })
        // Register all IPC commands
        .invoke_handler(tauri::generate_handler![
            get_registry_meta,
            get_image_meta,
            save_image_meta,
            get_event_rows,
            get_event_detail,
            save_act
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
