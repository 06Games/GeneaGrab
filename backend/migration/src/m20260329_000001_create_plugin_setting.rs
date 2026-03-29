use sea_orm_migration::prelude::*;

#[derive(DeriveMigrationName)]
pub struct Migration;

#[async_trait::async_trait]
impl MigrationTrait for Migration {
    async fn up(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        manager
            .create_table(
                Table::create()
                    .table(PluginSetting::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(PluginSetting::PluginId).string().not_null())
                    .col(ColumnDef::new(PluginSetting::Key).string().not_null())
                    .col(ColumnDef::new(PluginSetting::Value).string().not_null())
                    .primary_key(
                        Index::create()
                            .name("pk-plugin_setting")
                            .col(PluginSetting::PluginId)
                            .col(PluginSetting::Key),
                    )
                    .to_owned(),
            )
            .await
    }

    async fn down(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        manager
            .drop_table(Table::drop().table(PluginSetting::Table).to_owned())
            .await
    }
}

#[derive(DeriveIden)]
enum PluginSetting {
    Table,
    PluginId,
    Key,
    Value,
}
