use crate::date::Precision;
use std::fmt;

pub fn to_jdn(year: i32, month: u8, day: u8) -> i32 {
    let (mut y, mut m) = (year, month as i32);
    if m < 3 {
        y -= 1;
        m += 12;
    }
    365 * y + (y / 4) - (y / 100) + (y / 400) + (153 * m - 457) / 5 + day as i32 + 1721119
}

pub fn format(
    f: &mut fmt::Formatter<'_>,
    year: i32,
    month: u8,
    day: u8,
    precision: Precision,
) -> fmt::Result {
    match precision {
        Precision::Year => write!(f, "{:04}", year),
        Precision::Month => write!(f, "{:04}-{:02}", year, month),
        Precision::Day => write!(f, "{:04}-{:02}-{:02}", year, month, day),
    }
}
