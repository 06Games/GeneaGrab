use crate::date::Precision;
use regex::Regex;
use std::fmt;
use std::sync::LazyLock;

static FR_REGEX: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(r"(?i)^(?:(?P<day>\d+)\s+)?(?:(?P<month>[\p{L}\p{M}]+)\s+)?an\s+(?P<year>[IVX\d]+)")
        .unwrap()
});

pub fn to_jdn(year: i32, month: u8, day: u8) -> i32 {
    let start_jdns = [
        2375839, 2376204, 2376569, 2376934, 2377300, 2377665, 2378030, 2378395, 2378760, 2379125,
        2379490, 2379855, 2380221, 2380586,
    ];
    let base_jdn = *start_jdns
        .get((year as usize).saturating_sub(1))
        .unwrap_or(&2375839);
    base_jdn + ((month as i32 - 1) * 30) + (day as i32 - 1)
}

pub fn format(
    f: &mut fmt::Formatter<'_>,
    year: i32,
    month: u8,
    day: u8,
    precision: Precision,
) -> fmt::Result {
    let y_roman = to_roman(year);
    let m_name = month_name(month);
    match precision {
        Precision::Year => write!(f, "An {}", y_roman),
        Precision::Month => write!(f, "{} an {}", m_name, y_roman),
        Precision::Day => write!(f, "{} {} an {}", day, m_name, y_roman),
    }
}

pub fn parse(s: &str) -> Option<(i32, u8, u8, Precision)> {
    let caps = FR_REGEX.captures(s)?;

    let year = caps
        .name("year")
        .and_then(|m| parse_roman(m.as_str()))
        .unwrap_or(1);

    let (month, has_m) = caps
        .name("month")
        .and_then(|m| parse_month(m.as_str()))
        .map_or((1, false), |m| (m, true));

    let (day, has_d) = caps
        .name("day")
        .and_then(|m| m.as_str().parse::<u8>().ok())
        .map_or((1, false), |d| (d, true));

    let precision = if has_d {
        Precision::Day
    } else if has_m {
        Precision::Month
    } else {
        Precision::Year
    };

    Some((year, month, day, precision))
}

// --- Utilities ---

fn parse_roman(s: &str) -> Option<i32> {
    match s.to_uppercase().as_str() {
        "I" => Some(1),
        "II" => Some(2),
        "III" => Some(3),
        "IV" => Some(4),
        "V" => Some(5),
        "VI" => Some(6),
        "VII" => Some(7),
        "VIII" => Some(8),
        "IX" => Some(9),
        "X" => Some(10),
        "XI" => Some(11),
        "XII" => Some(12),
        "XIII" => Some(13),
        "XIV" => Some(14),
        _ => s.parse::<i32>().ok(),
    }
}

fn to_roman(num: i32) -> &'static str {
    match num {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        6 => "VI",
        7 => "VII",
        8 => "VIII",
        9 => "IX",
        10 => "X",
        11 => "XI",
        12 => "XII",
        13 => "XIII",
        14 => "XIV",
        _ => "Unknown",
    }
}

fn parse_month(s: &str) -> Option<u8> {
    let s = s
        .to_lowercase()
        .replace(['ô', 'o'], "o")
        .replace(['é', 'e'], "e");
    let months = [
        "vendemiaire",
        "brumaire",
        "frimaire",
        "nivose",
        "pluviose",
        "ventose",
        "germinal",
        "floreal",
        "prairial",
        "messidor",
        "thermidor",
        "fructidor",
        "sansculottides",
    ];
    months.iter().position(|&m| m == s).map(|i| (i + 1) as u8)
}

fn month_name(month: u8) -> &'static str {
    let months = [
        "Vendémiaire",
        "Brumaire",
        "Frimaire",
        "Nivôse",
        "Pluviôse",
        "Ventôse",
        "Germinal",
        "Floréal",
        "Prairial",
        "Messidor",
        "Thermidor",
        "Fructidor",
        "Sansculottides",
    ];
    months
        .get((month as usize).saturating_sub(1))
        .unwrap_or(&"Unknown")
}
