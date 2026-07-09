pub mod calendars;
mod date;
pub use date::*;

#[cfg(test)]
mod date_tests {
    use crate::date::{HistoricalDate, Precision};
    use std::str::FromStr;

    fn parser_data() -> Vec<(&'static str, HistoricalDate)> {
        vec![
            (
                "17/01/1420",
                HistoricalDate::Julian {
                    year: 1420,
                    month: 1,
                    day: 17,
                    precision: Precision::Day,
                },
            ),
            (
                "1420",
                HistoricalDate::Julian {
                    year: 1420,
                    month: 1,
                    day: 1,
                    precision: Precision::Year,
                },
            ),
            (
                "2023-07-30",
                HistoricalDate::Gregorian {
                    year: 2023,
                    month: 7,
                    day: 30,
                    precision: Precision::Day,
                },
            ),
            (
                "2023-07",
                HistoricalDate::Gregorian {
                    year: 2023,
                    month: 7,
                    day: 1,
                    precision: Precision::Month,
                },
            ),
            (
                "2023",
                HistoricalDate::Gregorian {
                    year: 2023,
                    month: 1,
                    day: 1,
                    precision: Precision::Year,
                },
            ),
            (
                "An XII",
                HistoricalDate::FrenchRepublican {
                    year: 12,
                    month: 1,
                    day: 1,
                    precision: Precision::Year,
                },
            ),
            (
                "An XIII de la République Française",
                HistoricalDate::FrenchRepublican {
                    year: 13,
                    month: 1,
                    day: 1,
                    precision: Precision::Year,
                },
            ),
            (
                "23 Ventôse An 4",
                HistoricalDate::FrenchRepublican {
                    year: 4,
                    month: 6,
                    day: 23,
                    precision: Precision::Day,
                },
            ),
            (
                "23 Ventôse An IV",
                HistoricalDate::FrenchRepublican {
                    year: 4,
                    month: 6,
                    day: 23,
                    precision: Precision::Day,
                },
            ),
            (
                "23 Ventose An IV",
                HistoricalDate::FrenchRepublican {
                    year: 4,
                    month: 6,
                    day: 23,
                    precision: Precision::Day,
                },
            ),
            (
                "10 Vendémiaire An X de la République Française",
                HistoricalDate::FrenchRepublican {
                    year: 10,
                    month: 1,
                    day: 10,
                    precision: Precision::Day,
                },
            ),
        ]
    }

    #[test]
    fn check_parser() {
        for (text, expected) in parser_data() {
            let parsed = HistoricalDate::from_str(text)
                .unwrap_or_else(|_| panic!("Parser failed to parse text: '{}'", text));

            assert_eq!(
                parsed.precision(),
                expected.precision(),
                "Precision mismatch for: {}",
                text
            );

            assert_eq!(parsed, expected, "Parsed date mismatch for: {}", text);
        }
    }

    fn stringify_data() -> Vec<(HistoricalDate, &'static str)> {
        vec![
            (
                HistoricalDate::Julian {
                    year: 1420,
                    month: 1,
                    day: 17,
                    precision: Precision::Day,
                },
                "1420-01-17",
            ),
            (
                HistoricalDate::Julian {
                    year: 1420,
                    month: 1,
                    day: 1,
                    precision: Precision::Year,
                },
                "1420",
            ),
            (
                HistoricalDate::Gregorian {
                    year: 2023,
                    month: 7,
                    day: 30,
                    precision: Precision::Day,
                },
                "2023-07-30",
            ),
            (
                HistoricalDate::Gregorian {
                    year: 2023,
                    month: 7,
                    day: 1,
                    precision: Precision::Month,
                },
                "2023-07",
            ),
            (
                HistoricalDate::Gregorian {
                    year: 2023,
                    month: 1,
                    day: 1,
                    precision: Precision::Year,
                },
                "2023",
            ),
            (
                HistoricalDate::FrenchRepublican {
                    year: 12,
                    month: 1,
                    day: 1,
                    precision: Precision::Year,
                },
                "An XII",
            ),
            (
                HistoricalDate::FrenchRepublican {
                    year: 13,
                    month: 1,
                    day: 1,
                    precision: Precision::Year,
                },
                "An XIII",
            ),
            (
                HistoricalDate::FrenchRepublican {
                    year: 3,
                    month: 5,
                    day: 1,
                    precision: Precision::Month,
                },
                "Pluviôse an III",
            ),
            (
                HistoricalDate::FrenchRepublican {
                    year: 4,
                    month: 6,
                    day: 23,
                    precision: Precision::Day,
                },
                "23 Ventôse an IV",
            ),
            (
                HistoricalDate::FrenchRepublican {
                    year: 10,
                    month: 1,
                    day: 10,
                    precision: Precision::Day,
                },
                "10 Vendémiaire an X",
            ),
        ]
    }

    #[test]
    fn check_stringify() {
        for (date, expected) in stringify_data() {
            assert_eq!(date.to_string(), expected, "Stringify output mismatch");
        }
    }

    #[test]
    fn check_serialization() {
        for (date, _) in stringify_data() {
            let json = serde_json::to_string(&date).expect("Failed to serialize");
            println!("{}", json);
            let deserialized: HistoricalDate =
                serde_json::from_str(&json).expect("Failed to deserialize");
            assert_eq!(date.precision(), deserialized.precision());
            assert_eq!(date, deserialized, "Serialization roundtrip failed");
        }
    }

    #[test]
    fn check_cross_calendar_comparisons() {
        let julian_date = HistoricalDate::from_str("1420-01-17").unwrap();
        let french_date = HistoricalDate::from_str("10 Vendémiaire An X").unwrap();
        let gregorian_date = HistoricalDate::from_str("2023-07-30").unwrap();

        assert!(julian_date < french_date);
        assert!(french_date < gregorian_date);
        assert!(gregorian_date > julian_date);
    }
}
