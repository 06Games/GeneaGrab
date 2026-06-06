use regex::Match;

/// Returns the trimmed string if the regex match is valid and non-empty, otherwise returns None.
#[must_use]
pub fn validate_regex_match(reg_match: Option<Match>) -> Option<String> {
    reg_match
        .map(|m| m.as_str().trim().to_string())
        .and_then(|s| if s.is_empty() { None } else { Some(s) })
}

/// Converts a lowercase string to title case
///
/// ```rust
/// use geneagrab_plugin_core::protocols::utils::to_title_case;
/// 
/// let text = "hello world";
/// assert_eq!(to_title_case(text), "Hello World");
/// ```
#[must_use]
pub fn to_title_case(s: &str) -> String {
    let mut result = String::with_capacity(s.len());
    let mut capitalize_next = true;

    for c in s.chars() {
        if c.is_whitespace() || c == '-' || c == '\'' || c == '(' {
            result.push(c);
            capitalize_next = true;
        } else if capitalize_next {
            result.extend(c.to_uppercase());
            capitalize_next = false;
        } else {
            result.extend(c.to_lowercase());
        }
    }
    result
}
