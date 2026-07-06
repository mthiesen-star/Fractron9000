//! Fractal-core: Pure Rust flame fractal data model and utilities.
//!
//! This crate defines the immutable data structures for Apophysis-compatible IFS flames.
//! All models support copy-on-write semantics for easy undo/redo implementation.
//!
//! # Design Principles
//!
//! - **Immutability**: All structures are immutable by design. Modifications return new copies.
//! - **Builder Pattern**: Use with_* methods to create modified copies (e.g., lame.with_brightness(2.0)).
//! - **Serde Support**: All models derive Serialize and Deserialize for XML I/O.
//! - **Legacy Compatibility**: Variation IDs and structure match the original Fractron9000/Apophysis format.
//! - **GPU Ready**: Uses glam types (Mat3, Vec2, Vec4) that map directly to GPU memory layouts.

pub mod affine2d;
pub mod flame;
pub mod io;
pub mod palette;
pub mod persist;
pub mod variations;

pub use affine2d::Affine2D;
