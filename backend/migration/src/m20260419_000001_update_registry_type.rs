use sea_orm_migration::prelude::*;

#[derive(DeriveMigrationName)]
pub struct Migration;

#[async_trait::async_trait]
impl MigrationTrait for Migration {
    async fn up(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        let db = manager.get_connection();

        // Transforms ["TypeA", "TypeB"] 
        // into [{"category": "other", "label": "TypeA"}, {"category": "other", "label": "TypeB"}]
        db.execute_unprepared(
            r#"
            UPDATE registry_entry
            SET registry_types = (
                SELECT json_group_array(
                    json_object('category', 'other', 'label', value)
                )
                FROM json_each(registry_types)
            )
            WHERE registry_types IS NOT NULL 
              AND json_type(registry_types) = 'array';
            "#
        ).await?;

        Ok(())
    }

    async fn down(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        let db = manager.get_connection();

        // Reverts the change by extracting only the 'label' value back into a plain string array
        db.execute_unprepared(
            r#"
            UPDATE registry_entry
            SET registry_types = (
                SELECT json_group_array(json_extract(value, '$.label'))
                FROM json_each(registry_types)
            )
            WHERE registry_types IS NOT NULL 
              AND json_type(registry_types) = 'array';
            "#
        ).await?;

        Ok(())
    }
}
