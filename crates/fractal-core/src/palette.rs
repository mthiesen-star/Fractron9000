//! 256-entry RGB palette with bilinear sampling.
//! Palettes can be 1D (256 colors sampled linearly) or 2D (for more variation).

use glam::Vec3;
use serde::{Deserialize, Serialize};

/// A color palette for mapping branch chroma values to RGB.
/// Stores colors and supports bilinear sampling in normalized [0, 1] space.
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Palette {
    /// Flat list of 256 RGB colors (Vec3 with components in [0, 1]).
    /// For 2D palettes, stored row-major: colors[y * width + x].
    pub colors: Vec<Vec3>,

    /// Width of the palette grid (typically 256 for 1D, or 16 for 2D 16x16).
    pub width: u32,

    /// Height of the palette grid (1 for 1D palettes, >1 for 2D).
    pub height: u32,

    /// Optional human-readable name (e.g., from source filename).
    pub name: Option<String>,
}

impl Palette {
    /// Create a new palette from a flat color array.
    /// For 1D palette: width=256, height=1.
    /// For 2D palette: width and height specify grid dimensions.
    pub fn new(colors: Vec<Vec3>, width: u32, height: u32, name: Option<String>) -> Self {
        assert_eq!(
            (width * height) as usize,
            colors.len(),
            "colors length must equal width * height"
        );
        Palette {
            colors,
            width,
            height,
            name,
        }
    }

    /// Create a default rainbow palette (256 colors).
    pub fn default_rainbow() -> Self {
        let mut colors = Vec::with_capacity(256);
        for i in 0..256 {
            let h = i as f32 / 256.0; // Hue: 0 to 1
            let rgb = hue_to_rgb(h);
            colors.push(rgb);
        }
        Palette {
            colors,
            width: 256,
            height: 1,
            name: Some("Default Rainbow".to_string()),
        }
    }

    /// Sample the palette at normalized coordinates (u, v) in [0, 1].
    /// Uses bilinear filtering for smooth color transitions.
    pub fn sample(&self, u: f32, v: f32) -> Vec3 {
        // Clamp to [0, 1] and convert to pixel coordinates
        let u = u.clamp(0.0, 1.0);
        let v = v.clamp(0.0, 1.0);

        let x = u * (self.width - 1) as f32;
        let y = v * (self.height - 1) as f32;

        let x0 = x.floor() as u32;
        let y0 = y.floor() as u32;
        let x1 = (x0 + 1).min(self.width - 1);
        let y1 = (y0 + 1).min(self.height - 1);

        let fx = x.fract();
        let fy = y.fract();

        // Bilinear interpolation
        let c00 = self.get_pixel(x0, y0);
        let c10 = self.get_pixel(x1, y0);
        let c01 = self.get_pixel(x0, y1);
        let c11 = self.get_pixel(x1, y1);

        let c0 = c00.lerp(c10, fx);
        let c1 = c01.lerp(c11, fx);
        c0.lerp(c1, fy)
    }

    /// Get a pixel at integer coordinates (clamped to valid range).
    pub fn get_pixel(&self, x: u32, y: u32) -> Vec3 {
        let x = x.min(self.width - 1);
        let y = y.min(self.height - 1);
        let idx = (y * self.width + x) as usize;
        self.colors[idx]
    }
}

/// Convert HSV hue (0-1) to RGB (each component in 0-1).
/// Uses full saturation and value for vibrant colors.
fn hue_to_rgb(h: f32) -> Vec3 {
    let h = h * 6.0; // Scale to 0-6 (six color sectors)
    let i = h.floor() as i32;
    let f = h - i as f32;

    let s = 1.0; // Full saturation
    let v = 1.0; // Full value

    let p = v * (1.0 - s);
    let q = v * (1.0 - f * s);
    let t = v * (1.0 - (1.0 - f) * s);

    match i % 6 {
        0 => Vec3::new(v, t, p),
        1 => Vec3::new(q, v, p),
        2 => Vec3::new(p, v, t),
        3 => Vec3::new(p, q, v),
        4 => Vec3::new(t, p, v),
        _ => Vec3::new(v, p, q),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_default_palette() {
        let pal = Palette::default_rainbow();
        assert_eq!(pal.colors.len(), 256);
        assert_eq!(pal.width, 256);
        assert_eq!(pal.height, 1);
    }

    #[test]
    fn test_sample_at_edges() {
        let pal = Palette::default_rainbow();
        let c0 = pal.sample(0.0, 0.0); // Should be close to pal.get_pixel(0, 0)
        let c1 = pal.sample(1.0, 0.0); // Should be close to pal.get_pixel(255, 0)
        assert!(c0.length() > 0.0);
        assert!(c1.length() > 0.0);
    }
}
