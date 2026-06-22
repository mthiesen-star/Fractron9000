//! Flame data model — Apophysis-compatible IFS parameter set.
//! All structures are immutable; mutations return new copies for undo/redo support.

use crate::variations::Variation;
use glam::{Mat3, Vec2, Vec4};
use serde::{Deserialize, Serialize};

/// A single variation entry (variation function + weight).
/// This is part of a branch's weighted sum of variations.
#[derive(Clone, Copy, Debug, Serialize, Deserialize)]
pub struct VariEntry {
    pub variation: Variation,
    pub weight: f32,
}

impl VariEntry {
    pub fn new(variation: Variation, weight: f32) -> Self {
        VariEntry { variation, weight }
    }

    /// Return a new VariEntry with updated weight.
    pub fn with_weight(self, weight: f32) -> Self {
        VariEntry { weight, ..self }
    }
}

/// An IFS branch (xform): a weighted affine transformation with variations and color.
/// All fields are immutable; use `with_*` methods to create modified copies.
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Branch {
    /// Pre-variation affine transform (2D rotation, scale, shear + translation).
    /// Stored as 3x3 affine matrix for GPU compatibility.
    pub pre_affine: Mat3,

    /// Post-variation affine transform.
    pub post_affine: Mat3,

    /// Chroma color coordinates in [0, 1] palette space (u, v).
    /// Used to index into the color palette.
    pub chroma: Vec2,

    /// Branch selection weight (probability).
    pub weight: f32,

    /// Color weight/alpha contribution (typically 0.5).
    pub color_weight: f32,

    /// Weighted sum of variation functions applied to the iterated point.
    /// Starts with Linear at weight 1.0 by default.
    pub variations: Vec<VariEntry>,
}

impl Branch {
    /// Create a branch with default values.
    pub fn default() -> Self {
        Branch {
            pre_affine: Mat3::from_scale(Vec2::splat(0.5)),
            post_affine: Mat3::IDENTITY,
            chroma: Vec2::new(0.5, 0.5),
            weight: 1.0,
            color_weight: 0.5,
            variations: vec![VariEntry::new(Variation::Linear, 1.0)],
        }
    }

    /// Return a new Branch with updated pre-affine.
    pub fn with_pre_affine(mut self, affine: Mat3) -> Self {
        self.pre_affine = affine;
        self
    }

    /// Return a new Branch with updated post-affine.
    pub fn with_post_affine(mut self, affine: Mat3) -> Self {
        self.post_affine = affine;
        self
    }

    /// Return a new Branch with updated chroma (color coordinates).
    pub fn with_chroma(mut self, chroma: Vec2) -> Self {
        self.chroma = chroma;
        self
    }

    /// Return a new Branch with updated weight.
    pub fn with_weight(mut self, weight: f32) -> Self {
        self.weight = weight;
        self
    }

    /// Return a new Branch with updated color weight.
    pub fn with_color_weight(mut self, color_weight: f32) -> Self {
        self.color_weight = color_weight;
        self
    }

    /// Return a new Branch with updated variations.
    pub fn with_variations(mut self, variations: Vec<VariEntry>) -> Self {
        self.variations = variations;
        self
    }
}

/// A flame: a complete IFS fractal descriptor compatible with Apophysis.
/// All fields are immutable; use `with_*` methods to create modified copies.
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Flame {
    /// Human-readable name.
    pub name: String,

    /// Version string (e.g., "Fractron9000 2.0").
    pub version: String,

    /// Camera affine transform (defines view rectangle and zoom).
    /// Stored as 3x3 affine matrix: typically maps [-1, 1] to screen.
    pub camera_transform: Mat3,

    /// Tone-mapping brightness (multiplicative scale for histogram).
    pub brightness: f32,

    /// Gamma correction exponent (typically 2.0).
    pub gamma: f32,

    /// Vibrancy blend factor (0-1): blends between log-scale and linear.
    pub vibrancy: f32,

    /// Background color (RGBA).
    pub background: Vec4,

    /// The IFS branches (xforms) that define the fractal.
    pub branches: Vec<Branch>,

    /// Optional custom color palette. If None, uses a default.
    pub palette: Option<crate::palette::Palette>,
}

impl Flame {
    /// Create a new flame with default values.
    pub fn default() -> Self {
        Flame {
            name: "New Fractal".to_string(),
            version: "Fractron9000 2.0".to_string(),
            camera_transform: Mat3::IDENTITY,  // No scale - use iteration space directly
            brightness: 500.0,  // DIAGNOSTIC: cranked way up
            gamma: 2.0,
            vibrancy: 1.0,
            background: Vec4::new(0.0, 0.0, 0.0, 1.0),
            branches: vec![Branch::default()],
            palette: None,
        }
    }

    /// Create a simple test/demo flame (Sierpinski triangle).
    pub fn demo() -> Self {
        let mut flame = Flame::default();

        // Sierpinski-like pattern: three half-scale branches at different positions
        flame.branches.clear();

        // Branch 1: top center
        let b1 = Branch::default()
            .with_pre_affine(Mat3::from_cols(
                glam::Vec3::new(0.5, 0.0, 0.0),    // x_axis: [a, c, 0]
                glam::Vec3::new(0.0, 0.5, 0.0),    // y_axis: [b, d, 0]
                glam::Vec3::new(0.0, 0.25, 1.0),   // z_axis: [e, f, 1] - translation to (0, 0.25)
            ))
            .with_chroma(Vec2::new(1.0, 0.5))
            .with_weight(1.0);
        flame.branches.push(b1);

        // Branch 2: bottom left
        let b2 = Branch::default()
            .with_pre_affine(Mat3::from_cols(
                glam::Vec3::new(0.5, 0.0, 0.0),      // x_axis
                glam::Vec3::new(0.0, 0.5, 0.0),      // y_axis
                glam::Vec3::new(-0.433, -0.25, 1.0), // z_axis: translation to (-0.433, -0.25)
            ))
            .with_chroma(Vec2::new(0.25, 0.9))
            .with_weight(1.0);
        flame.branches.push(b2);

        // Branch 3: bottom right
        let b3 = Branch::default()
            .with_pre_affine(Mat3::from_cols(
                glam::Vec3::new(0.5, 0.0, 0.0),     // x_axis
                glam::Vec3::new(0.0, 0.5, 0.0),     // y_axis
                glam::Vec3::new(0.433, -0.25, 1.0), // z_axis: translation to (0.433, -0.25)
            ))
            .with_chroma(Vec2::new(0.25, 0.1))
            .with_weight(1.0);
        flame.branches.push(b3);

        flame
    }

    /// Return a new Flame with updated name.
    pub fn with_name(mut self, name: String) -> Self {
        self.name = name;
        self
    }

    /// Return a new Flame with updated version.
    pub fn with_version(mut self, version: String) -> Self {
        self.version = version;
        self
    }

    /// Return a new Flame with updated camera transform.
    pub fn with_camera_transform(mut self, camera_transform: Mat3) -> Self {
        self.camera_transform = camera_transform;
        self
    }

    /// Return a new Flame with updated brightness.
    pub fn with_brightness(mut self, brightness: f32) -> Self {
        self.brightness = brightness;
        self
    }

    /// Return a new Flame with updated gamma.
    pub fn with_gamma(mut self, gamma: f32) -> Self {
        self.gamma = gamma;
        self
    }

    /// Return a new Flame with updated vibrancy.
    pub fn with_vibrancy(mut self, vibrancy: f32) -> Self {
        self.vibrancy = vibrancy;
        self
    }

    /// Return a new Flame with updated background color.
    pub fn with_background(mut self, background: Vec4) -> Self {
        self.background = background;
        self
    }

    /// Return a new Flame with updated branches.
    pub fn with_branches(mut self, branches: Vec<Branch>) -> Self {
        self.branches = branches;
        self
    }

    /// Return a new Flame with a specific branch replaced.
    pub fn with_branch_at(mut self, index: usize, branch: Branch) -> Option<Self> {
        if index < self.branches.len() {
            self.branches[index] = branch;
            Some(self)
        } else {
            None
        }
    }

    /// Return a new Flame with updated palette.
    pub fn with_palette(mut self, palette: Option<crate::palette::Palette>) -> Self {
        self.palette = palette;
        self
    }
}
