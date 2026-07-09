use chrono::NaiveDate;
use serde::{Deserialize, Serialize};
use std::cmp::Ordering;
use std::fmt;
use std::str::FromStr;

use crate::calendars::{self, french, gregorian, julian};

#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Serialize, Deserialize)]
pub enum Precision {
    Year,
    Month,
    Day,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(tag = "calendar")]
pub enum HistoricalDate {
    Gregorian {
        year: i32,
        month: u8,
        day: u8,
        precision: Precision,
    },
    Julian {
        year: i32,
        month: u8,
        day: u8,
        precision: Precision,
    },
    FrenchRepublican {
        year: i32,
        month: u8,
        day: u8,
        precision: Precision,
    },
}

impl HistoricalDate {
    pub fn precision(&self) -> Precision {
        match *self {
            Self::Gregorian { precision, .. } => precision,
            Self::Julian { precision, .. } => precision,
            Self::FrenchRepublican { precision, .. } => precision,
        }
    }

    pub fn to_jdn(&self) -> i32 {
        match *self {
            Self::Gregorian {
                year, month, day, ..
            } => gregorian::to_jdn(year, month, day),
            Self::Julian {
                year, month, day, ..
            } => julian::to_jdn(year, month, day),
            Self::FrenchRepublican {
                year, month, day, ..
            } => french::to_jdn(year, month, day),
        }
    }

    pub fn to_modern_date(&self) -> Option<NaiveDate> {
        NaiveDate::from_num_days_from_ce_opt(self.to_jdn() - 1721425)
    }
}

impl PartialEq for HistoricalDate {
    fn eq(&self, other: &Self) -> bool {
        self.to_jdn() == other.to_jdn() && self.precision() == other.precision()
    }
}
impl Eq for HistoricalDate {}

impl PartialOrd for HistoricalDate {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(self.cmp(other))
    }
}

impl Ord for HistoricalDate {
    fn cmp(&self, other: &Self) -> Ordering {
        self.to_jdn()
            .cmp(&other.to_jdn())
            .then_with(|| self.precision().cmp(&other.precision()))
    }
}

impl fmt::Display for HistoricalDate {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match *self {
            Self::Gregorian {
                year,
                month,
                day,
                precision,
            } => gregorian::format(f, year, month, day, precision),
            Self::Julian {
                year,
                month,
                day,
                precision,
            } => julian::format(f, year, month, day, precision),
            Self::FrenchRepublican {
                year,
                month,
                day,
                precision,
            } => french::format(f, year, month, day, precision),
        }
    }
}

impl FromStr for HistoricalDate {
    type Err = String;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let s = s.trim();

        if let Some((year, month, day, precision)) = french::parse(s) {
            return Ok(Self::FrenchRepublican {
                year,
                month,
                day,
                precision,
            });
        }

        if let Some((year, month, day, precision)) = calendars::parse_standard_ymd(s) {
            let is_gregorian = year > 1582
                || (year == 1582 && month > 10)
                || (year == 1582 && month == 10 && day >= 15);

            if is_gregorian {
                return Ok(Self::Gregorian {
                    year,
                    month,
                    day,
                    precision,
                });
            } else {
                return Ok(Self::Julian {
                    year,
                    month,
                    day,
                    precision,
                });
            }
        }

        Err("Could not detect calendar format".to_string())
    }
}
