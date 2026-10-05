use std::str::FromStr;
use sea_orm_migration::prelude::*;
use sea_orm_migration::sea_orm::{DatabaseBackend, Statement};

#[derive(DeriveMigrationName)]
pub struct Migration;

fn parse_date_to_jdn(raw: &str) -> Option<i32> {
    let trimmed = raw.trim();
    if trimmed.is_empty() || trimmed == "null" {
        return None;
    }
    // 1. Try deserializing as JSON HistoricalDate object e.g. {"calendar":"Gregorian", ...}
    if let Ok(d) = serde_json::from_str::<dates::HistoricalDate>(trimmed) {
        return Some(d.to_jdn());
    }
    // 2. Try deserializing as JSON string e.g. "\"1850\""
    if let Ok(s) = serde_json::from_str::<String>(trimmed) {
        if let Ok(d) = dates::HistoricalDate::from_str(&s) {
            return Some(d.to_jdn());
        }
    }
    // 3. Try parsing as raw string directly
    if let Ok(d) = dates::HistoricalDate::from_str(trimmed) {
        return Some(d.to_jdn());
    }
    None
}

#[async_trait::async_trait]
impl MigrationTrait for Migration {
    async fn up(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        let db = manager.get_connection();

        let rows = db
            .query_all_raw(Statement::from_string(
                DatabaseBackend::Sqlite,
                r#"
                SELECT id, date_from, date_to 
                FROM registry_entry 
                WHERE (date_from IS NOT NULL AND date_from_normalized IS NULL) 
                   OR (date_to IS NOT NULL AND date_to_normalized IS NULL)
                "#,
            ))
            .await?;

        for row in rows {
            let id: u32 = row.try_get("", "id")?;
            let date_from_raw: Option<String> = row.try_get("", "date_from")?;
            let date_to_raw: Option<String> = row.try_get("", "date_to")?;

            if let Some(ref from_str) = date_from_raw {
                if let Some(jdn) = parse_date_to_jdn(from_str) {
                    db.execute_raw(Statement::from_sql_and_values(
                        DatabaseBackend::Sqlite,
                        "UPDATE registry_entry SET date_from_normalized = ? WHERE id = ? AND date_from_normalized IS NULL",
                        vec![jdn.into(), id.into()],
                    ))
                    .await?;
                }
            }

            if let Some(ref to_str) = date_to_raw {
                if let Some(jdn) = parse_date_to_jdn(to_str) {
                    db.execute_raw(Statement::from_sql_and_values(
                        DatabaseBackend::Sqlite,
                        "UPDATE registry_entry SET date_to_normalized = ? WHERE id = ? AND date_to_normalized IS NULL",
                        vec![jdn.into(), id.into()],
                    ))
                    .await?;
                }
            }
        }

        Ok(())
    }

    async fn down(&self, _manager: &SchemaManager) -> Result<(), DbErr> {
        Ok(())
    }
}
