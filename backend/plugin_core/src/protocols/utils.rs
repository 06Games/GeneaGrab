use regex::Match;

/**
Returns the trimmed string if the regex match is valid and non-empty, otherwise returns None.
*/
#[must_use]
pub fn validate_regex_match(reg_match: Option<Match>) -> Option<String> {
    reg_match
        .map(|m| m.as_str().trim().to_string())
        .and_then(|s| if s.is_empty() { None } else { Some(s) })
}
