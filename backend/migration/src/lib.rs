pub use sea_orm_migration::prelude::*;

mod m20220101_000001_create_table;
mod m20260329_000001_create_plugin_setting;
mod m20260419_000001_update_registry_type;
mod m20260610_000001_dates;

pub struct Migrator;

#[async_trait::async_trait]
impl MigratorTrait for Migrator {
    fn migrations() -> Vec<Box<dyn MigrationTrait>> {
        vec![
            Box::new(m20220101_000001_create_table::Migration),
            Box::new(m20260329_000001_create_plugin_setting::Migration),
            Box::new(m20260419_000001_update_registry_type::Migration),
            Box::new(m20260610_000001_dates::Migration),
        ]
    }
}
