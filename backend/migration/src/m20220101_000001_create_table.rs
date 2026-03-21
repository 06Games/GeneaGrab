use sea_orm_migration::prelude::*;

#[derive(DeriveMigrationName)]
pub struct Migration;

#[async_trait::async_trait]
impl MigrationTrait for Migration {
    async fn up(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        // Create registry_entry table
        manager
            .create_table(
                Table::create()
                    .table(RegistryEntry::Table)
                    .if_not_exists()
                    .col(
                        ColumnDef::new(RegistryEntry::Id)
                            .integer()
                            .not_null()
                            .primary_key()
                            .auto_increment(),
                    )
                    .col(ColumnDef::new(RegistryEntry::SourceId).string().not_null())
                    .col(
                        ColumnDef::new(RegistryEntry::RegistryId)
                            .string()
                            .not_null(),
                    )
                    .col(ColumnDef::new(RegistryEntry::ArchiveReference).string())
                    .col(
                        ColumnDef::new(RegistryEntry::RegistryTypes)
                            .json()
                            .not_null(),
                    )
                    .col(ColumnDef::new(RegistryEntry::Collection).json().not_null())
                    .col(ColumnDef::new(RegistryEntry::ManifestUrl).string())
                    .col(ColumnDef::new(RegistryEntry::ArkUrl).string())
                    .col(ColumnDef::new(RegistryEntry::Title).string())
                    .col(ColumnDef::new(RegistryEntry::Subtitle).string())
                    .col(ColumnDef::new(RegistryEntry::Author).string())
                    .col(ColumnDef::new(RegistryEntry::DateFrom).string())
                    .col(ColumnDef::new(RegistryEntry::DateFromNormalized).date_time())
                    .col(ColumnDef::new(RegistryEntry::DateTo).string())
                    .col(ColumnDef::new(RegistryEntry::DateToNormalized).date_time())
                    .col(ColumnDef::new(RegistryEntry::Places).json().not_null())
                    .col(ColumnDef::new(RegistryEntry::Notes).text())
                    .col(ColumnDef::new(RegistryEntry::Extra).json().not_null())
                    .to_owned(),
            )
            .await?;

        // Create image_entry table
        manager
            .create_table(
                Table::create()
                    .table(ImageEntry::Table)
                    .if_not_exists()
                    .col(
                        ColumnDef::new(ImageEntry::Id)
                            .integer()
                            .not_null()
                            .primary_key()
                            .auto_increment(),
                    )
                    .col(
                        ColumnDef::new(ImageEntry::RegistryEntryId)
                            .integer()
                            .not_null(),
                    )
                    .col(ColumnDef::new(ImageEntry::Width).integer())
                    .col(ColumnDef::new(ImageEntry::Height).integer())
                    .col(ColumnDef::new(ImageEntry::TileSize).integer())
                    .col(ColumnDef::new(ImageEntry::ManifestUrl).string())
                    .col(ColumnDef::new(ImageEntry::ArkUrl).string())
                    .col(ColumnDef::new(ImageEntry::ImageNumber).integer().not_null())
                    .col(ColumnDef::new(ImageEntry::Name).string())
                    .col(ColumnDef::new(ImageEntry::DateRange).string())
                    .col(ColumnDef::new(ImageEntry::Notes).text())
                    .foreign_key(
                        ForeignKey::create()
                            .name("fk-image_entry-registry_entry_id")
                            .from(ImageEntry::Table, ImageEntry::RegistryEntryId)
                            .to(RegistryEntry::Table, RegistryEntry::Id)
                            .on_delete(ForeignKeyAction::Cascade)
                            .on_update(ForeignKeyAction::Cascade),
                    )
                    .to_owned(),
            )
            .await?;

        Ok(())
    }

    async fn down(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        // Drop tables in reverse order to respect foreign keys
        manager
            .drop_table(Table::drop().table(ImageEntry::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(RegistryEntry::Table).to_owned())
            .await?;

        Ok(())
    }
}

// These Iden enums represent the tables and columns for the builder
#[derive(DeriveIden)]
enum RegistryEntry {
    Table,
    Id,
    SourceId,
    RegistryId,
    ArchiveReference,
    RegistryTypes,
    Collection,
    ManifestUrl,
    ArkUrl,
    Title,
    Subtitle,
    Author,
    DateFrom,
    DateFromNormalized,
    DateTo,
    DateToNormalized,
    Places,
    Notes,
    Extra,
}

#[derive(DeriveIden)]
enum ImageEntry {
    Table,
    Id,
    RegistryEntryId,
    Width,
    Height,
    TileSize,
    ManifestUrl,
    ArkUrl,
    ImageNumber,
    Name,
    DateRange,
    Notes,
}
