use crate::{
    comm_models::{EventDetail, EventRow},
    errors::CoreError,
};
use sea_orm::DbConn;

pub async fn fetch_event_rows(_db: &DbConn, registry_id: u32) -> Result<Vec<EventRow>, CoreError> {
    tracing::info!("fetch_event_rows called for registry {registry_id}");

    Ok(vec![])
}

pub async fn fetch_event(_db: &DbConn, event_id: u32) -> Result<EventDetail, CoreError> {
    tracing::info!("fetch_event called for {event_id}");

    Err(CoreError::NotFound("Not implemented yet".into()))
}

pub async fn save_act(_db: &DbConn, event: EventDetail) -> Result<(), CoreError> {
    tracing::info!("save_act called for event {}", event.event_id);

    Ok(())
}
