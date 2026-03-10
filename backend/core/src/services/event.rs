use sea_orm::{ActiveModelTrait, ColumnTrait, DbConn, EntityTrait, QueryFilter, Set};
use crate::{
    comm_models::{EventDetail, EventRow},
    errors::CoreError,
};

pub async fn fetch_event_rows(db: &DbConn, registry_id: String) -> Result<Vec<EventRow>, CoreError> {
    log::info!("fetch_event_rows called for registry {}", registry_id);

    Ok(vec![])
}

pub async fn fetch_event(db: &DbConn, event_id: u32) -> Result<EventDetail, CoreError> {
    log::info!("fetch_event called for {}", event_id);

    Err(CoreError::NotFound("Not implemented yet".into()))
}

pub async fn save_act(db: &DbConn, event: EventDetail) -> Result<(), CoreError> {
    log::info!("save_act called for event {}", event.event_id);

    Ok(())
}
