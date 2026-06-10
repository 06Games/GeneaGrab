use sea_orm_migration::prelude::*;

#[derive(DeriveMigrationName)]
pub struct Migration;

#[async_trait::async_trait]
impl MigrationTrait for Migration {
    async fn up(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        let columns_to_drop = vec![
            "date_from",
            "date_from_normalized",
            "date_to",
            "date_to_normalized",
        ];

        for col in columns_to_drop {
            manager
                .alter_table(
                    Table::alter()
                        .table(RegistryEntry::Table)
                        .drop_column(Alias::new(col))
                        .to_owned(),
                )
                .await?;
        }

        manager
            .alter_table(
                Table::alter()
                    .table(RegistryEntry::Table)
                    .add_column(ColumnDef::new(Alias::new("date_from")).json_binary().null())
                    .to_owned(),
            )
            .await?;

        manager
            .alter_table(
                Table::alter()
                    .table(RegistryEntry::Table)
                    .add_column(
                        ColumnDef::new(Alias::new("date_from_normalized"))
                            .integer()
                            .null(),
                    )
                    .to_owned(),
            )
            .await?;

        manager
            .alter_table(
                Table::alter()
                    .table(RegistryEntry::Table)
                    .add_column(ColumnDef::new(Alias::new("date_to")).json_binary().null())
                    .to_owned(),
            )
            .await?;

        manager
            .alter_table(
                Table::alter()
                    .table(RegistryEntry::Table)
                    .add_column(
                        ColumnDef::new(Alias::new("date_to_normalized"))
                            .integer()
                            .null(),
                    )
                    .to_owned(),
            )
            .await?;

        Ok(())
    }

    async fn down(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        let columns_to_drop = vec![
            "date_from",
            "date_from_normalized",
            "date_to",
            "date_to_normalized",
        ];

        for col in columns_to_drop {
            manager
                .alter_table(
                    Table::alter()
                        .table(RegistryEntry::Table)
                        .drop_column(Alias::new(col))
                        .to_owned(),
                )
                .await?;
        }

        manager
            .alter_table(
                Table::alter()
                    .table(RegistryEntry::Table)
                    .add_column(ColumnDef::new(Alias::new("date_from")).string().null())
                    .to_owned(),
            )
            .await?;

        manager
            .alter_table(
                Table::alter()
                    .table(RegistryEntry::Table)
                    .add_column(
                        ColumnDef::new(Alias::new("date_from_normalized"))
                            .date_time()
                            .null(),
                    )
                    .to_owned(),
            )
            .await?;

        manager
            .alter_table(
                Table::alter()
                    .table(RegistryEntry::Table)
                    .add_column(ColumnDef::new(Alias::new("date_to")).string().null())
                    .to_owned(),
            )
            .await?;

        manager
            .alter_table(
                Table::alter()
                    .table(RegistryEntry::Table)
                    .add_column(
                        ColumnDef::new(Alias::new("date_to_normalized"))
                            .date_time()
                            .null(),
                    )
                    .to_owned(),
            )
            .await?;

        Ok(())
    }
}

#[derive(DeriveIden)]
enum RegistryEntry {
    Table,
}
