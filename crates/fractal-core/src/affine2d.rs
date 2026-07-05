//! 2D affine transform: x_axis, y_axis, translation.
//!
//! Stores exactly the 6 meaningful coefficients of a 2D affine transform.
//! The implicit third row is always [0, 0, 1], so non-affine matrices are
//! not representable — invalid states are not constructable.
//!
//! Coefficient layout (compatible with Apophysis a/b/c/d/e/f convention):
//!
//! ```text
//! [ A  B  C ]   A = x_axis.x      B = y_axis.x      C = translation.x
//! [ D  E  F ]   D = x_axis.y      E = y_axis.y      F = translation.y
//! ```
//!
//! Transforms a column vector `v` as:
//!   `result = A*vx + B*vy + C,  D*vx + E*vy + F`

use glam::{Mat3, Vec2, Vec3};
use serde::{Deserialize, Serialize};
use std::ops::Mul;

/// A 2D affine transform stored as (x_axis, y_axis, translation).
#[derive(Clone, Copy, Debug, PartialEq, Serialize, Deserialize)]
pub struct Affine2D {
    /// X basis vector (column A, D).
    pub x_axis: Vec2,
    /// Y basis vector (column B, E).
    pub y_axis: Vec2,
    /// Translation (column C, F).
    pub translation: Vec2,
}

impl Affine2D {
    /// The identity transform.
    pub const IDENTITY: Self = Self {
        x_axis: Vec2::new(1.0, 0.0),
        y_axis: Vec2::new(0.0, 1.0),
        translation: Vec2::ZERO,
    };

    /// Uniform scale with no rotation or translation.
    pub fn from_scale(scale: Vec2) -> Self {
        Self {
            x_axis: Vec2::new(scale.x, 0.0),
            y_axis: Vec2::new(0.0, scale.y),
            translation: Vec2::ZERO,
        }
    }

    /// Transform a point (applies linear part **and** translation).
    #[inline]
    pub fn transform_point(self, v: Vec2) -> Vec2 {
        Vec2::new(
            self.x_axis.x * v.x + self.y_axis.x * v.y + self.translation.x,
            self.x_axis.y * v.x + self.y_axis.y * v.y + self.translation.y,
        )
    }

    /// Transform a direction vector (applies linear part only, **no** translation).
    #[inline]
    pub fn transform_vector(self, v: Vec2) -> Vec2 {
        Vec2::new(
            self.x_axis.x * v.x + self.y_axis.x * v.y,
            self.x_axis.y * v.x + self.y_axis.y * v.y,
        )
    }

    /// Scalar determinant of the linear (2×2) part.
    #[inline]
    pub fn determinant(self) -> f32 {
        self.x_axis.x * self.y_axis.y - self.x_axis.y * self.y_axis.x
    }

    /// Inverse transform.  Panics (via division) if the transform is singular.
    pub fn inverse(self) -> Self {
        let det = self.determinant();
        Self {
            x_axis: Vec2::new(self.y_axis.y / det, -self.x_axis.y / det),
            y_axis: Vec2::new(-self.y_axis.x / det, self.x_axis.x / det),
            translation: Vec2::new(
                (self.translation.y * self.y_axis.x - self.translation.x * self.y_axis.y) / det,
                (self.translation.x * self.x_axis.y - self.translation.y * self.x_axis.x) / det,
            ),
        }
    }

    /// Compose two affine transforms: `self` applied after `rhs`.
    /// Equivalent to matrix multiplication `self * rhs`.
    pub fn compose(self, rhs: Self) -> Self {
        Self {
            x_axis: self.transform_vector(rhs.x_axis),
            y_axis: self.transform_vector(rhs.y_axis),
            translation: self.transform_point(rhs.translation),
        }
    }

    /// Expand to a column-major `Mat3` for GPU upload.
    /// The Z row is always `[0, 0, 1]`.
    pub fn to_mat3(self) -> Mat3 {
        Mat3::from_cols(
            Vec3::new(self.x_axis.x, self.x_axis.y, 0.0),
            Vec3::new(self.y_axis.x, self.y_axis.y, 0.0),
            Vec3::new(self.translation.x, self.translation.y, 1.0),
        )
    }
}

/// Compose: `lhs` applied after `rhs`  (same convention as matrix multiplication).
impl Mul<Affine2D> for Affine2D {
    type Output = Affine2D;

    fn mul(self, rhs: Affine2D) -> Affine2D {
        self.compose(rhs)
    }
}

/// Convert a column-major `Mat3` to an `Affine2D`, discarding the Z row.
impl From<Mat3> for Affine2D {
    fn from(m: Mat3) -> Self {
        Self {
            x_axis: Vec2::new(m.x_axis.x, m.x_axis.y),
            y_axis: Vec2::new(m.y_axis.x, m.y_axis.y),
            translation: Vec2::new(m.z_axis.x, m.z_axis.y),
        }
    }
}
