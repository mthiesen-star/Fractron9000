//! Native persistence format for Fractron 9000 flame files (`.f9k`).
//!
//! Format: versioned, pretty-printed JSON — one flame per file.
//!
//! ## Palette storage
//! - **1D palettes**: inline as `["#RRGGBB", ...]`
//! - **2D palettes**: by name (`"default"`, `"dark"`, `"inferno2"`, `"frost"`)
//! - **No custom palette** (`null`): runtime default is used

use crate::affine2d::Affine2D;
use crate::flame::{Branch, Flame};
use crate::palette::Palette;
use glam::{Vec3, Vec4};
use serde::{Deserialize, Serialize};

pub const FORMAT_VERSION: u32 = 1;
pub const FILE_EXTENSION: &str = "f9k";

// ============================================================================
// Error
// ============================================================================

#[derive(Debug, Clone)]
pub enum PersistError {
    Json(String),
    InvalidData(String),
    UnsupportedVersion { found: u32, expected: u32 },
}

impl std::fmt::Display for PersistError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::Json(e) => write!(f, "JSON error: {}", e),
            Self::InvalidData(e) => write!(f, "Invalid data: {}", e),
            Self::UnsupportedVersion { found, expected } => write!(
                f,
                "Unsupported format version {} (this build supports {})",
                found, expected
            ),
        }
    }
}

impl std::error::Error for PersistError {}

// ============================================================================
// Palette reference
// ============================================================================

/// How a palette is stored in a `.f9k` file.
///
/// Serializes without a type tag — the presence of the `colors` vs `name` key
/// is sufficient to disambiguate.
#[derive(Serialize, Deserialize, Clone, Debug, PartialEq)]
#[serde(untagged)]
pub enum PaletteRef {
    /// A 1D palette stored inline as `"#RRGGBB"` hex strings.
    Inline { colors: Vec<String> },
    /// A named built-in 2D palette (`"default"`, `"dark"`, `"inferno2"`, `"frost"`).
    Named { name: String },
}

// ============================================================================
// FlameData — on-disk mirror of Flame, palette replaced by PaletteRef
// ============================================================================

/// On-disk representation of a flame.  Mirrors [`Flame`] except the palette
/// field uses [`PaletteRef`] instead of the runtime [`Palette`] type.
#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct FlameData {
    pub camera_transform: Affine2D,
    pub brightness: f32,
    pub gamma: f32,
    pub vibrancy: f32,
    pub background: Vec4,
    pub branches: Vec<Branch>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub palette: Option<PaletteRef>,
}

impl FlameData {
    fn from_flame(flame: &Flame) -> Self {
        let palette = flame.palette.as_ref().map(|p| {
            if p.height == 1 {
                PaletteRef::Inline {
                    colors: p.colors.iter().map(|c| encode_hex(*c)).collect(),
                }
            } else {
                // 2D palette: persist by name (falls back to "default" if unnamed)
                PaletteRef::Named {
                    name: p.name.clone().unwrap_or_else(|| "default".to_string()),
                }
            }
        });

        FlameData {
            camera_transform: flame.camera_transform,
            brightness: flame.brightness,
            gamma: flame.gamma,
            vibrancy: flame.vibrancy,
            background: flame.background,
            branches: flame.branches.clone(),
            palette,
        }
    }

    /// Build a runtime [`Flame`].
    ///
    /// `resolve_named` is called for 2D named palettes; return `None` to fall
    /// back to no palette (the renderer will use its default).
    pub fn into_flame(self, resolve_named: impl Fn(&str) -> Option<Palette>) -> Flame {
        let palette = self.palette.and_then(|p| match p {
            PaletteRef::Inline { colors } => decode_palette_1d(&colors).ok(),
            PaletteRef::Named { name } => resolve_named(&name),
        });

        Flame {
            camera_transform: self.camera_transform,
            brightness: self.brightness,
            gamma: self.gamma,
            vibrancy: self.vibrancy,
            background: self.background,
            branches: self.branches,
            palette,
        }
    }
}

// ============================================================================
// FractronFile — root of every .f9k file
// ============================================================================

/// The root object of a `.f9k` file.
#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct FractronFile {
    pub format_version: u32,
    pub flame: FlameData,
}

// ============================================================================
// Public API
// ============================================================================

/// Serialize a [`Flame`] to a pretty-printed `.f9k` JSON string.
pub fn save_flame(flame: &Flame) -> Result<String, PersistError> {
    let file = FractronFile {
        format_version: FORMAT_VERSION,
        flame: FlameData::from_flame(flame),
    };
    serde_json::to_string_pretty(&file).map_err(|e| PersistError::Json(e.to_string()))
}

/// Deserialize a `.f9k` JSON string into a [`FractronFile`].
///
/// Returns the raw file so the caller can resolve named palettes before
/// calling [`FlameData::into_flame`].
pub fn load_flame(json: &str) -> Result<FractronFile, PersistError> {
    let file: FractronFile =
        serde_json::from_str(json).map_err(|e| PersistError::Json(e.to_string()))?;
    if file.format_version != FORMAT_VERSION {
        return Err(PersistError::UnsupportedVersion {
            found: file.format_version,
            expected: FORMAT_VERSION,
        });
    }
    Ok(file)
}

// ============================================================================
// Hex helpers (private)
// ============================================================================

fn encode_hex(c: Vec3) -> String {
    let r = (c.x.clamp(0.0, 1.0) * 255.0).round() as u8;
    let g = (c.y.clamp(0.0, 1.0) * 255.0).round() as u8;
    let b = (c.z.clamp(0.0, 1.0) * 255.0).round() as u8;
    format!("#{r:02X}{g:02X}{b:02X}")
}

fn decode_hex(s: &str) -> Result<Vec3, PersistError> {
    let s = s.trim_start_matches('#');
    if s.len() != 6 {
        return Err(PersistError::InvalidData(format!(
            "invalid hex color \"{}\"",
            s
        )));
    }
    let parse = |slice: &str| {
        u8::from_str_radix(slice, 16)
            .map_err(|_| PersistError::InvalidData(format!("invalid hex byte \"{}\"", slice)))
    };
    let r = parse(&s[0..2])?;
    let g = parse(&s[2..4])?;
    let b = parse(&s[4..6])?;
    Ok(Vec3::new(r as f32 / 255.0, g as f32 / 255.0, b as f32 / 255.0))
}

fn decode_palette_1d(colors: &[String]) -> Result<Palette, PersistError> {
    let decoded: Result<Vec<Vec3>, _> = colors.iter().map(|s| decode_hex(s)).collect();
    let decoded = decoded?;
    let count = decoded.len() as u32;
    Ok(Palette::new(decoded, count, 1, None))
}

// ============================================================================
// Tests
// ============================================================================

#[cfg(test)]
mod tests {
    use super::*;
    use crate::flame::Flame;

    #[test]
    fn round_trip_demo_flame() {
        let original = Flame::demo();
        let json = save_flame(&original).expect("save should succeed");
        let file = load_flame(&json).expect("load should succeed");
        let restored = file.flame.into_flame(|_| None);

        assert_eq!(restored.brightness, original.brightness);
        assert_eq!(restored.branches.len(), original.branches.len());
        assert_eq!(restored.palette, original.palette);

        // Variation names survive the round-trip as strings
        for (rb, ob) in restored.branches.iter().zip(original.branches.iter()) {
            assert_eq!(rb.variations, ob.variations);
        }
    }

    #[test]
    fn palette_inline_hex_round_trip() {
        let colors: Vec<Vec3> = (0u8..=255)
            .map(|i| Vec3::new(i as f32 / 255.0, 0.0, 1.0 - i as f32 / 255.0))
            .collect();
        let palette = Palette::new(colors.clone(), 256, 1, None);
        let encoded: Vec<String> = colors.iter().map(|c| encode_hex(*c)).collect();
        let decoded = decode_palette_1d(&encoded).expect("decode should succeed");

        for (orig, dec) in colors.iter().zip(decoded.colors.iter()) {
            // Round-trip through u8 — tolerance of 1/255
            assert!((orig.x - dec.x).abs() < 1.0 / 255.0 + 1e-6);
            assert!((orig.z - dec.z).abs() < 1.0 / 255.0 + 1e-6);
        }
        let _ = palette; // suppress unused warning
    }

    #[test]
    fn named_palette_serializes_as_name() {
        use crate::palette::Palette;
        use glam::Vec3;
        let colors = vec![Vec3::ZERO; 256 * 256];
        let pal = Palette::new(colors, 256, 256, Some("dark".to_string()));
        let flame = Flame::default().with_palette(Some(pal));
        let json = save_flame(&flame).expect("save should succeed");
        assert!(json.contains(r#""name": "dark""#));
        assert!(!json.contains("colors"));
    }
}
