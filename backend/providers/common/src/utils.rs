use std::fmt::Display;
use regex::Match;

/// Represents the base geometry of a tiled image pyramid.
#[derive(Debug, Clone, Copy)]
pub struct ImageGeometry {
    pub width: u32,
    pub height: u32,
    pub tile_size: u32,
}

/// Information about a specific zoom level's geometry.
#[derive(Debug, Clone, Copy)]
pub struct LevelGeometry {
    pub level: u32,
    pub width: u32,
    pub height: u32,
    pub tiles_x: u32,
    pub tiles_y: u32,
    pub scale_factor: u32,
}

impl Display for LevelGeometry {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "level {} ({}x{} tiles, total {}x{})",
            self.level, self.tiles_x, self.tiles_y, self.width, self.height
        )
    }
}

impl ImageGeometry {
    #[must_use]
    pub fn new(width: u32, height: u32, tile_size: u32) -> Self {
        Self {
            width,
            height,
            tile_size,
        }
    }

    /// Calculates the maximum zoom level available.
    #[must_use]
    pub fn max_level(&self) -> u32 {
        let max_dim = self.width.max(self.height);
        let ratio = max_dim.div_ceil(self.tile_size);
        if ratio <= 1 {
            0
        } else {
            (ratio - 1).ilog2() + 1
        }
    }

    /// Returns the geometry parameters for a specific level index.
    #[must_use]
    pub fn level(&self, level: u32) -> LevelGeometry {
        let max = self.max_level();
        let level = level.min(max);
        let scale_factor = 2u32.pow(max - level);

        // Dimensions at this specific zoom level
        let l_width = self.width / scale_factor;
        let l_height = self.height / scale_factor;

        LevelGeometry {
            level,
            width: l_width,
            height: l_height,
            tiles_x: l_width.div_ceil(self.tile_size),
            tiles_y: l_height.div_ceil(self.tile_size),
            scale_factor,
        }
    }
}

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
/// use geneagrab_providers::utils::to_title_case;
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
