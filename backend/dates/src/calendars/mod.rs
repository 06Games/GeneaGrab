pub mod french;
pub mod gregorian;
pub mod julian;

use crate::date::Precision;
use chrono::{Datelike, NaiveDate};

/// Shared utility for standard YYYY-MM-DD parsing utilizing `chrono`.
/// We use the Gregorian parser from `chrono` and rely on our core date enum
/// to remap it to Julian later based on the 1582 threshold.
pub fn parse_standard_ymd(s: &str) -> Option<(i32, u8, u8, Precision)> {
    // Try Full Precision (Day) - YYYY-MM-DD or DD/MM/YYYY
    if let Ok(d) = NaiveDate::parse_from_str(s, "%Y-%m-%d")
        .or_else(|_| NaiveDate::parse_from_str(s, "%d/%m/%Y"))
    {
        return Some((d.year(), d.month() as u8, d.day() as u8, Precision::Day));
    }

    // Try Month Precision (Month) - YYYY-MM or MM/YYYY
    // We append a dummy day "-01" because Chrono requires a full date to parse.
    if let Ok(d) = NaiveDate::parse_from_str(&format!("{}-01", s), "%Y-%m-%d")
        .or_else(|_| NaiveDate::parse_from_str(&format!("01/{}", s), "%d/%m/%Y"))
    {
        return Some((d.year(), d.month() as u8, 1, Precision::Month));
    }

    // Try Year Precision (Year) - YYYY
    // We append a dummy month and day "-01-01" so Chrono has a full date to parse.
    if let Ok(d) = NaiveDate::parse_from_str(&format!("{}-01-01", s), "%Y-%m-%d") {
        return Some((d.year(), 1, 1, Precision::Year));
    }

    None
}
