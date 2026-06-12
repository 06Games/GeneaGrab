use sea_orm_migration::prelude::*;

#[derive(DeriveMigrationName)]
pub struct Migration;

#[async_trait::async_trait]
impl MigrationTrait for Migration {
    async fn up(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        manager
            .alter_table(
                Table::alter()
                    .table("image_entry")
                    .add_column(ColumnDef::new(Alias::new("api_url")).string().null())
                    .to_owned(),
            )
            .await?;

        manager
            .alter_table(
                Table::alter()
                    .table("image_entry")
                    .add_column(ColumnDef::new(Alias::new("download_url")).string().null())
                    .to_owned(),
            )
            .await?;

        Ok(())
    }

    async fn down(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        manager
            .alter_table(
                Table::alter()
                    .table("image_entry")
                    .drop_column(Alias::new("api_url"))
                    .to_owned(),
            )
            .await?;

        manager
            .alter_table(
                Table::alter()
                    .table("image_entry")
                    .drop_column(Alias::new("download_url"))
                    .to_owned(),
            )
            .await?;

        Ok(())
    }
}
