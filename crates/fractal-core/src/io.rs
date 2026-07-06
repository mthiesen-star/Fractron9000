//! XML I/O for Apophysis-compatible flame files.
//!
//! This module provides deserialization from Apophysis XML format to our immutable data models.
//! We manually parse XML with quick-xml to handle dynamic attributes (variation names with weights)
//! which don't fit neatly into serde's struct-based deserialization.

use crate::affine2d::Affine2D;
use crate::flame::{Branch, Flame, VariEntry};
use crate::variations::Variation;
use glam::{Vec2, Vec4};
use quick_xml::events::Event;
use quick_xml::Reader;
use std::io::Cursor;

/// Error type for XML parsing and validation.
#[derive(Debug, Clone)]
pub enum ParseError {
    Xml(String),
    InvalidData(String),
    MissingField(String),
}

impl std::fmt::Display for ParseError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            ParseError::Xml(e) => write!(f, "XML error: {}", e),
            ParseError::InvalidData(e) => write!(f, "Invalid data: {}", e),
            ParseError::MissingField(e) => write!(f, "Missing field: {}", e),
        }
    }
}

impl std::error::Error for ParseError {}

/// Helper to parse space-separated floats.
fn parse_floats(s: &str) -> Result<Vec<f32>, ParseError> {
    s.split_whitespace()
        .map(|num| {
            num.parse::<f32>()
                .map_err(|_| ParseError::InvalidData(format!("Cannot parse float: {}", num)))
        })
        .collect()
}

/// Helper to parse a single space-separated float pair.
fn parse_float_pair(s: &str) -> Result<(f32, f32), ParseError> {
    let floats = parse_floats(s)?;
    if floats.len() != 2 {
        return Err(ParseError::InvalidData(format!(
            "Expected 2 floats, got {}",
            floats.len()
        )));
    }
    Ok((floats[0], floats[1]))
}

/// Helper to parse 6 floats (2D affine transform coefficients: a b c d e f).
fn parse_affine_coefs(s: &str) -> Result<Affine2D, ParseError> {
    let coefs = parse_floats(s)?;
    if coefs.len() != 6 {
        return Err(ParseError::InvalidData(format!(
            "Expected 6 affine coefs, got {}",
            coefs.len()
        )));
    }
    // Coefs are: a b c d e f
    // They represent the 2x3 affine: [a c e]
    //                                [b d f]
    Ok(Affine2D {
        x_axis: Vec2::new(coefs[0], coefs[1]),
        y_axis: Vec2::new(coefs[2], coefs[3]),
        translation: Vec2::new(coefs[4], coefs[5]),
    })
}

/// Helper to extract xform attributes and create a branch.
fn parse_xform_element(e: &quick_xml::events::BytesStart, reader: &quick_xml::Reader<std::io::Cursor<&[u8]>>) -> Result<Branch, ParseError> {
    let mut weight = 1.0_f32;
    let mut color = 0.5_f32;
    let mut f9k_color2 = 0.5_f32;
    let mut coefs_str = String::new();
    let mut post_str: Option<String> = None;
    let mut variations: Vec<(Variation, f32)> = Vec::new();

    for attr_result in e.attributes() {
        let attr = attr_result.map_err(|e| ParseError::Xml(format!("{}", e)))?;
        let key = std::str::from_utf8(attr.key.as_ref()).map_err(|_| {
            ParseError::Xml("Invalid UTF-8 in xform attribute key".to_string())
        })?;
        let value = attr
            .decode_and_unescape_value(reader)
            .map_err(|e| ParseError::Xml(format!("{}", e)))?;

        match key {
            "weight" => {
                weight = value.parse().map_err(|_| {
                    ParseError::InvalidData(format!("Invalid weight: {}", value))
                })?
            }
            "color" => {
                color = value.parse().map_err(|_| {
                    ParseError::InvalidData(format!("Invalid color: {}", value))
                })?
            }
            "f9k_color2" => {
                f9k_color2 = value.parse().map_err(|_| {
                    ParseError::InvalidData(format!("Invalid f9k_color2: {}", value))
                })?
            }
            "coefs" => coefs_str = value.to_string(),
            "post" => post_str = Some(value.to_string()),
            _ => {
                // Try to match as a variation name
                if let Ok(var_weight) = value.parse::<f32>() {
                    if var_weight > 0.0001 {
                        if let Some(var) = Variation::by_attr_name(key) {
                            variations.push((var, var_weight));
                        }
                    }
                }
            }
        }
    }

    // Default to Linear if no variations specified
    if variations.is_empty() {
        variations.push((Variation::Linear, 1.0));
    }

    // Parse affine transforms
    let pre_affine = parse_affine_coefs(&coefs_str)?;
    let post_affine = match post_str {
        Some(post) => parse_affine_coefs(&post)?,
        None => Affine2D::IDENTITY,
    };

    // Build branch
    let var_entries: Vec<VariEntry> =
        variations.into_iter().map(|(v, w)| VariEntry::new(v, w)).collect();

    Ok(Branch {
        pre_affine,
        post_affine,
        chroma: Vec2::new(color, f9k_color2),
        weight,
        color_weight: 0.5,
        variations: var_entries,
    })
}

/// Parse a single flame from XML string (Apophysis format).
/// Returns the flame's name alongside the parsed `Flame`.
pub fn parse_flame_xml(xml: &str) -> Result<(String, Flame), ParseError> {
    let mut reader = Reader::from_reader(Cursor::new(xml.as_bytes()));
    let mut buf = Vec::new();

    let mut flame_name = String::new();
    let mut flame_size = "800 600".to_string();
    let mut flame_center = "0 0".to_string();
    let mut flame_scale = 1.0_f32;
    let mut flame_zoom = 0.0_f32;
    let mut flame_rotate = 0.0_f32;
    let mut flame_background = "0 0 0".to_string();
    let mut flame_brightness = 1.0_f32;
    let mut flame_gamma = 2.0_f32;
    let mut flame_vibrancy = 1.0_f32;
    let mut branches = Vec::new();

    loop {
        match reader.read_event_into(&mut buf) {
            Ok(Event::Start(e)) => {
                let tag_name_result = e.name()
                    .as_ref()
                    .iter()
                    .map(|&b| b as char)
                    .collect::<String>();

                if tag_name_result == "flame" {
                    for attr_result in e.attributes() {
                        let attr = attr_result.map_err(|e| ParseError::Xml(format!("{}", e)))?;
                        let key = std::str::from_utf8(attr.key.as_ref())
                            .map_err(|_| ParseError::Xml("Invalid UTF-8 in attribute key".to_string()))?;
                        let value = attr
                            .decode_and_unescape_value(&reader)
                            .map_err(|e| ParseError::Xml(format!("{}", e)))?;

                        match key {
                            "name" => flame_name = value.to_string(),
                            "version" => {}
                            "size" => flame_size = value.to_string(),
                            "center" => flame_center = value.to_string(),
                            "scale" => {
                                flame_scale = value.parse().map_err(|_| {
                                    ParseError::InvalidData("Invalid scale".to_string())
                                })?
                            }
                            "zoom" => {
                                flame_zoom = value.parse().map_err(|_| {
                                    ParseError::InvalidData("Invalid zoom".to_string())
                                })?
                            }
                            "rotate" => {
                                flame_rotate = value.parse().map_err(|_| {
                                    ParseError::InvalidData("Invalid rotate".to_string())
                                })?
                            }
                            "background" => flame_background = value.to_string(),
                            "brightness" => {
                                flame_brightness = value.parse().map_err(|_| {
                                    ParseError::InvalidData("Invalid brightness".to_string())
                                })?
                            }
                            "gamma" => {
                                flame_gamma = value.parse().map_err(|_| {
                                    ParseError::InvalidData("Invalid gamma".to_string())
                                })?
                            }
                            "vibrancy" => {
                                flame_vibrancy = value.parse().map_err(|_| {
                                    ParseError::InvalidData("Invalid vibrancy".to_string())
                                })?
                            }
                            _ => {}
                        }
                    }
                } else if tag_name_result == "xform" {
                    let branch = parse_xform_element(&e, &reader)?;
                    branches.push(branch);
                }
            }
            Ok(Event::Empty(e)) => {
                let tag_name_result = e.name()
                    .as_ref()
                    .iter()
                    .map(|&b| b as char)
                    .collect::<String>();
                
                if tag_name_result == "xform" {
                    let branch = parse_xform_element(&e, &reader)?;
                    branches.push(branch);
                }
            }
            Ok(Event::Eof) => break,
            Err(e) => return Err(ParseError::Xml(format!("XML parse error: {}", e))),
            _ => {}
        }
        buf.clear();
    }

    // Parse camera transform from flame parameters
    let (width, height) = parse_float_pair(&flame_size)?;
    let (center_x, center_y) = parse_float_pair(&flame_center)?;

    let min_size = width.min(height);
    let zoom_scale = 0.5_f32.powf(flame_zoom);
    let cam_span = (min_size / flame_scale) * zoom_scale;
    let cam_scale = cam_span / 2.0;

    let theta = flame_rotate * std::f32::consts::PI / 180.0;
    let xx = theta.cos() * cam_scale;
    let xy = theta.sin() * cam_scale;

    let camera_transform = Affine2D {
        x_axis: Vec2::new(xx, xy),
        y_axis: Vec2::new(-xy, xx),
        translation: Vec2::new(center_x, center_y),
    };

    // Parse background color
    let bg_floats = parse_floats(&flame_background)?;
    let background = match bg_floats.len() {
        3 => Vec4::new(bg_floats[0], bg_floats[1], bg_floats[2], 1.0),
        4 => Vec4::new(bg_floats[0], bg_floats[1], bg_floats[2], bg_floats[3]),
        _ => {
            return Err(ParseError::InvalidData(format!(
                "Expected 3 or 4 background components, got {}",
                bg_floats.len()
            )))
        }
    };

    // Default to one branch if none specified
    if branches.is_empty() {
        branches.push(Branch::default());
    }

    Ok((flame_name, Flame {
        camera_transform,
        brightness: flame_brightness,
        gamma: flame_gamma,
        vibrancy: flame_vibrancy,
        background,
        branches,
        palette: None,
    }))
}

/// Parse all flames from a .flame file (which may contain multiple `<flame>` elements).
/// Returns a Vec of (flame_name, Flame) tuples.
/// If a flame fails to parse, it's skipped with an error message.
pub fn parse_flame_file(contents: &str) -> Result<Vec<(String, Flame)>, ParseError> {
    let mut result = Vec::new();
    
    // Find all <flame ...> ... </flame> blocks
    let mut pos = 0;
    while let Some(start) = contents[pos..].find("<flame ") {
        let start_abs = pos + start;
        
        // Find the opening >
        if let Some(open_bracket) = contents[start_abs..].find('>') {
            let open_abs = start_abs + open_bracket + 1;
            
            // Find closing </flame>
            if let Some(close) = contents[open_abs..].find("</flame>") {
                let close_abs = open_abs + close;
                let flame_xml = &contents[start_abs..=close_abs + 7]; // include </flame>
                
                // Try to parse this flame
                match parse_flame_xml(flame_xml) {
                    Ok((name, flame)) => {
                        result.push((name, flame));
                    }
                    Err(e) => {
                        eprintln!("Warning: Failed to parse a flame in file: {}", e);
                    }
                }
                
                pos = close_abs + 8; // Move past </flame>
            } else {
                break;
            }
        } else {
            break;
        }
    }
    
    if result.is_empty() {
        return Err(ParseError::InvalidData("No valid flames found in file".to_string()));
    }

    Ok(result)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_floats() {
        let floats = parse_floats("1.5 2.5 3.0").unwrap();
        assert_eq!(floats.len(), 3);
        assert!((floats[0] - 1.5).abs() < 0.001);
    }

    #[test]
    fn test_parse_affine_coefs() {
        let mat = parse_affine_coefs("0.5 0 0 0.5 0.433 -0.25").unwrap();
        assert!((mat.x_axis.x - 0.5).abs() < 0.001);
        assert!((mat.y_axis.y - 0.5).abs() < 0.001);
    }

    #[test]
    fn test_parse_simple_flame() {
        let xml = r#"<flame name="test" version="Fractron9000 2.0" brightness="1.5" gamma="2.0" vibrancy="1.0">
            <xform weight="1" color="0.5" f9k_color2="0.5" linear="1" coefs="0.5 0 0 0.5 0 0" />
        </flame>"#;

        let result = parse_flame_xml(xml);
        assert!(result.is_ok(), "Failed to parse: {:?}", result.err());

        let (name, flame) = result.unwrap();
        assert_eq!(name, "test");
        assert!((flame.brightness - 1.5).abs() < 0.001);
        assert_eq!(flame.branches.len(), 1);
    }
}
