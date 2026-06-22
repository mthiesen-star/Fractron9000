//! Variation enum — 30 named parametric distortion functions.
//! IDs must match the Legacy codebase for flame file compatibility.

use serde::{Deserialize, Serialize};

/// Immutable variation function descriptor. IDs 0-29 match Apophysis/Fractron legacy.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash, Serialize, Deserialize)]
pub enum Variation {
    Linear = 0,
    Sinusoidal = 1,
    Spherical = 2,
    Swirl = 3,
    Horseshoe = 4,
    Polar = 5,
    Handkerchief = 6,
    Heart = 7,
    Disc = 8,
    Spiral = 9,
    Hyperbolic = 10,
    Diamond = 11,
    Ex = 12,
    Julia = 13,
    Bent = 14,
    Waves = 15,
    Fisheye = 16,
    Popcorn = 17,
    Exponential = 18,
    Power = 19,
    Cosine = 20,
    Eyefish = 21,
    Bubble = 22,
    Cylinder = 23,
    Noise = 24,
    Blur = 25,
    GaussianBlur = 26,
    Orb9000 = 27,
    Ripple9000 = 28,
    Bulge9000 = 29,
}

impl Variation {
    /// All variations in order by ID.
    pub fn all() -> &'static [Variation] {
        &[
            Variation::Linear,
            Variation::Sinusoidal,
            Variation::Spherical,
            Variation::Swirl,
            Variation::Horseshoe,
            Variation::Polar,
            Variation::Handkerchief,
            Variation::Heart,
            Variation::Disc,
            Variation::Spiral,
            Variation::Hyperbolic,
            Variation::Diamond,
            Variation::Ex,
            Variation::Julia,
            Variation::Bent,
            Variation::Waves,
            Variation::Fisheye,
            Variation::Popcorn,
            Variation::Exponential,
            Variation::Power,
            Variation::Cosine,
            Variation::Eyefish,
            Variation::Bubble,
            Variation::Cylinder,
            Variation::Noise,
            Variation::Blur,
            Variation::GaussianBlur,
            Variation::Orb9000,
            Variation::Ripple9000,
            Variation::Bulge9000,
        ]
    }

    /// Total count of variations.
    pub fn count() -> usize {
        30
    }

    /// Get variation by numeric ID (0-29).
    pub fn by_id(id: u8) -> Option<Variation> {
        match id {
            0 => Some(Variation::Linear),
            1 => Some(Variation::Sinusoidal),
            2 => Some(Variation::Spherical),
            3 => Some(Variation::Swirl),
            4 => Some(Variation::Horseshoe),
            5 => Some(Variation::Polar),
            6 => Some(Variation::Handkerchief),
            7 => Some(Variation::Heart),
            8 => Some(Variation::Disc),
            9 => Some(Variation::Spiral),
            10 => Some(Variation::Hyperbolic),
            11 => Some(Variation::Diamond),
            12 => Some(Variation::Ex),
            13 => Some(Variation::Julia),
            14 => Some(Variation::Bent),
            15 => Some(Variation::Waves),
            16 => Some(Variation::Fisheye),
            17 => Some(Variation::Popcorn),
            18 => Some(Variation::Exponential),
            19 => Some(Variation::Power),
            20 => Some(Variation::Cosine),
            21 => Some(Variation::Eyefish),
            22 => Some(Variation::Bubble),
            23 => Some(Variation::Cylinder),
            24 => Some(Variation::Noise),
            25 => Some(Variation::Blur),
            26 => Some(Variation::GaussianBlur),
            27 => Some(Variation::Orb9000),
            28 => Some(Variation::Ripple9000),
            29 => Some(Variation::Bulge9000),
            _ => None,
        }
    }

    /// Get variation ID (0-29).
    pub fn id(self) -> u8 {
        self as u8
    }

    /// Get the display name.
    pub fn name(self) -> &'static str {
        match self {
            Variation::Linear => "Linear",
            Variation::Sinusoidal => "Sinusoidal",
            Variation::Spherical => "Spherical",
            Variation::Swirl => "Swirl",
            Variation::Horseshoe => "Horseshoe",
            Variation::Polar => "Polar",
            Variation::Handkerchief => "Handkerchief",
            Variation::Heart => "Heart",
            Variation::Disc => "Disc",
            Variation::Spiral => "Spiral",
            Variation::Hyperbolic => "Hyperbolic",
            Variation::Diamond => "Diamond",
            Variation::Ex => "Ex",
            Variation::Julia => "Julia",
            Variation::Bent => "Bent",
            Variation::Waves => "Waves",
            Variation::Fisheye => "Fisheye",
            Variation::Popcorn => "Popcorn",
            Variation::Exponential => "Exponential",
            Variation::Power => "Power",
            Variation::Cosine => "Cosine",
            Variation::Eyefish => "Eyefish",
            Variation::Bubble => "Bubble",
            Variation::Cylinder => "Cylinder",
            Variation::Noise => "Noise",
            Variation::Blur => "Blur",
            Variation::GaussianBlur => "Gaussian Blur",
            Variation::Orb9000 => "Orb 9000",
            Variation::Ripple9000 => "Ripple 9000",
            Variation::Bulge9000 => "Bulge 9000",
        }
    }

    /// Get the XML attribute name (lowercase with underscores).
    pub fn attr_name(self) -> &'static str {
        match self {
            Variation::Linear => "linear",
            Variation::Sinusoidal => "sinusoidal",
            Variation::Spherical => "spherical",
            Variation::Swirl => "swirl",
            Variation::Horseshoe => "horseshoe",
            Variation::Polar => "polar",
            Variation::Handkerchief => "handkerchief",
            Variation::Heart => "heart",
            Variation::Disc => "disc",
            Variation::Spiral => "spiral",
            Variation::Hyperbolic => "hyperbolic",
            Variation::Diamond => "diamond",
            Variation::Ex => "ex",
            Variation::Julia => "julia",
            Variation::Bent => "bent",
            Variation::Waves => "waves",
            Variation::Fisheye => "fisheye",
            Variation::Popcorn => "popcorn",
            Variation::Exponential => "exponential",
            Variation::Power => "power",
            Variation::Cosine => "cosine",
            Variation::Eyefish => "eyefish",
            Variation::Bubble => "bubble",
            Variation::Cylinder => "cylinder",
            Variation::Noise => "noise",
            Variation::Blur => "blur",
            Variation::GaussianBlur => "gaussian_blur",
            Variation::Orb9000 => "f9k_orb",
            Variation::Ripple9000 => "f9k_ripple",
            Variation::Bulge9000 => "f9k_bulge",
        }
    }

    /// Look up variation by XML attribute name.
    pub fn by_attr_name(name: &str) -> Option<Variation> {
        Variation::all()
            .iter()
            .find(|&&v| v.attr_name() == name)
            .copied()
    }
}

impl std::fmt::Display for Variation {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.name())
    }
}
