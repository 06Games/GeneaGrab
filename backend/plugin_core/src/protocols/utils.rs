use regex::Match;

/**
Returns the trimmed string if the regex match is valid and non-empty, otherwise returns None.
*/
pub fn validate_regex_match(reg_match: Option<Match>) -> Option<String> {
    reg_match
        .map(|m| m.as_str().trim().to_string())
        .map(|s| s.is_empty().then(|| None).unwrap_or(Some(s)))
        .flatten()
}
