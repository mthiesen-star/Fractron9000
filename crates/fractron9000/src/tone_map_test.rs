/// Tone-map formula test — validate against Legacy implementation
/// 
/// Test data from comet fractal final render:
/// - Total dots: 4,098M
/// - Flame params: brightness=3.11, gamma=3.9, vibrancy=1
/// - invPixArea: ~884 (from scale 29.7302, det = scale²)

use glam::FloatExt;

fn log_b10(x: f32) -> f32 {
    x.log10()
}

/// Current Rust implementation in tonemap.wgsl
fn tone_map_current(
    r: f32, g: f32, b: f32,
    count: f32,
    brightness: f32, gamma: f32, vibrancy: f32,
    total_dot_count: f32,
    inv_pixel_area: f32,
) -> (f32, f32, f32) {
    if count < 0.5 {
        return (0.0, 0.0, 0.0);
    }
    
    let tone_c1 = 0.5;
    let tone_c2 = 64.0;
    
    let scale_constant = tone_c2 * inv_pixel_area / (total_dot_count + 1e-6);
    let log_term = 1.0 + count * scale_constant;
    let log_a = tone_c1 * brightness * log_b10(log_term);
    
    println!("  [Current] scale_constant={:.6e}, log_term={:.6e}, log_a={:.6e}", scale_constant, log_term, log_a);
    
    let r_avg = r / (count + 1e-6);
    let g_avg = g / (count + 1e-6);
    let b_avg = b / (count + 1e-6);
    
    let log_r = log_a * r_avg;
    let log_g = log_a * g_avg;
    let log_b_val = log_a * b_avg;
    
    let inv_gamma = 1.0 / gamma;
    let z = log_a.powf(inv_gamma);
    let gamma_factor = z / (log_a + 1e-6);
    
    let result_r = (log_r.powf(inv_gamma).lerp(gamma_factor * log_r, vibrancy)).clamp(0.0, 1.0);
    let result_g = (log_g.powf(inv_gamma).lerp(gamma_factor * log_g, vibrancy)).clamp(0.0, 1.0);
    let result_b = (log_b_val.powf(inv_gamma).lerp(gamma_factor * log_b_val, vibrancy)).clamp(0.0, 1.0);
    
    (result_r, result_g, result_b)
}

/// Legacy formula (best interpretation from code review):
/// scale = C1 * brightness / (pixel_area * total_iterations)
/// ka = log10(1 + raw * scale) / raw
/// logPix = raw * ka
/// result = lerp(logPix^(1/gamma), logPix^(1/gamma) / logPix * logPix, vibrancy)
fn tone_map_legacy(
    r: f32, g: f32, b: f32,
    count: f32,
    brightness: f32, gamma: f32, vibrancy: f32,
    total_dot_count: f32,
    pixel_area: f32,
) -> (f32, f32, f32) {
    if count < 0.5 {
        return (0.0, 0.0, 0.0);
    }
    
    let c1 = 0.5;
    
    // scale = C1 * brightness / (pixel_area * total_iterations)
    let scale = c1 * brightness / (pixel_area * total_dot_count + 1e-6);
    
    // ka = log10(1 + raw * scale) / raw
    let log_term = 1.0 + count * scale;
    let ka = log_b10(log_term) / (count + 1e-6);
    
    // logPix = raw * ka
    let log_pix = count * ka;
    
    println!("  [Legacy]  scale={:.6e}, log_term={:.6e}, ka={:.6e}, log_pix={:.6e}", scale, log_term, ka, log_pix);
    
    // Normalize colors
    let r_avg = r / (count + 1e-6);
    let g_avg = g / (count + 1e-6);
    let b_avg = b / (count + 1e-6);
    
    // Apply log intensity to channels
    let log_r = log_pix * r_avg;
    let log_g = log_pix * g_avg;
    let log_b_val = log_pix * b_avg;
    
    // Gamma correction
    let inv_gamma = 1.0 / gamma;
    let z = log_pix.powf(inv_gamma);
    let gamma_factor = z / (log_pix + 1e-6);
    
    // Apply vibrancy
    let result_r = (log_r.powf(inv_gamma).lerp(gamma_factor * log_r, vibrancy)).clamp(0.0, 1.0);
    let result_g = (log_g.powf(inv_gamma).lerp(gamma_factor * log_g, vibrancy)).clamp(0.0, 1.0);
    let result_b = (log_b_val.powf(inv_gamma).lerp(gamma_factor * log_b_val, vibrancy)).clamp(0.0, 1.0);
    
    (result_r, result_g, result_b)
}

/// Legacy formula (CORRECTED HYPOTHESIS):
/// If pixel_area is actually the DETERMINANT (not inverted):
/// scale = C1 * brightness / (determinant * total_iterations)
fn tone_map_legacy_v2(
    r: f32, g: f32, b: f32,
    count: f32,
    brightness: f32, gamma: f32, vibrancy: f32,
    total_dot_count: f32,
    pixel_area: f32,  // Actually the determinant
) -> (f32, f32, f32) {
    if count < 0.5 {
        return (0.0, 0.0, 0.0);
    }
    
    let c1 = 0.5;
    
    // scale = C1 * brightness / (det * total_iterations)
    let scale = c1 * brightness / (pixel_area * total_dot_count + 1e-6);
    
    // ka = log10(1 + raw * scale) / raw
    let log_term = 1.0 + count * scale;
    let ka = log_b10(log_term) / (count + 1e-6);
    
    // logPix = raw * ka
    let log_pix = count * ka;
    
    println!("  [Legacy v2] scale={:.6e}, log_term={:.6e}, ka={:.6e}, log_pix={:.6e}", scale, log_term, ka, log_pix);
    
    // Normalize colors
    let r_avg = r / (count + 1e-6);
    let g_avg = g / (count + 1e-6);
    let b_avg = b / (count + 1e-6);
    
    // Apply log intensity to channels
    let log_r = log_pix * r_avg;
    let log_g = log_pix * g_avg;
    let log_b_val = log_pix * b_avg;
    
    // Gamma correction
    let inv_gamma = 1.0 / gamma;
    let z = log_pix.powf(inv_gamma);
    let gamma_factor = z / (log_pix + 1e-6);
    
    // Apply vibrancy
    let result_r = (log_r.powf(inv_gamma).lerp(gamma_factor * log_r, vibrancy)).clamp(0.0, 1.0);
    let result_g = (log_g.powf(inv_gamma).lerp(gamma_factor * log_g, vibrancy)).clamp(0.0, 1.0);
    let result_b = (log_b_val.powf(inv_gamma).lerp(gamma_factor * log_b_val, vibrancy)).clamp(0.0, 1.0);
    
    (result_r, result_g, result_b)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_tone_map_formulas() {
        // Test data from comet final render
        let total_dots = 4098.0e6;  // 4,098M
        let brightness = 3.11;
        let gamma = 3.9;
        let vibrancy = 1.0;
        let inv_pixel_area = 884.0;  // scale² = 29.7302²
        let pixel_area = 1.0 / inv_pixel_area;
        
        // Test cases: (name, count, r_accum, g_accum, b_accum)
        // Accumulated RGB should be COUNT * (average_color_per_dot in range [0, 1])
        // So for 1000 dots with avg color 0.7, r_accum = 1000 * 0.7 = 700
        let test_cases = vec![
            ("Very Sparse", 10.0, 5.0, 4.0, 3.0),       // 10 hits, avg color (0.5, 0.4, 0.3)
            ("Sparse", 100.0, 60.0, 50.0, 40.0),        // 100 hits, avg color (0.6, 0.5, 0.4)
            ("Medium", 1000.0, 700.0, 600.0, 500.0),    // 1000 hits, avg color (0.7, 0.6, 0.5)
            ("Dense", 10000.0, 8000.0, 7000.0, 6000.0), // 10000 hits, avg color (0.8, 0.7, 0.6)
        ];
        
        for (name, count, r, g, b) in test_cases {
            println!("\n=== Test: {} ({} hits) ===", name, count as i32);
            let (r_current, g_current, b_current) = tone_map_current(
                r, g, b, count, brightness, gamma, vibrancy, total_dots, inv_pixel_area
            );
            
            let (r_legacy, g_legacy, b_legacy) = tone_map_legacy(
                r, g, b, count, brightness, gamma, vibrancy, total_dots, pixel_area
            );
            
            let (r_legacy_v2, g_legacy_v2, b_legacy_v2) = tone_map_legacy_v2(
                r, g, b, count, brightness, gamma, vibrancy, total_dots, inv_pixel_area  // Pass det, not 1/det
            );
            
            println!("Current:  R={:.6} G={:.6} B={:.6}", r_current, g_current, b_current);
            println!("Legacy:   R={:.6} G={:.6} B={:.6}", r_legacy, g_legacy, b_legacy);
            println!("Legacy v2:R={:.6} G={:.6} B={:.6}", r_legacy_v2, g_legacy_v2, b_legacy_v2);
            
            let delta_r = (r_current - r_legacy_v2).abs();
            let delta_g = (g_current - g_legacy_v2).abs();
            let delta_b = (b_current - b_legacy_v2).abs();
            if delta_r < 0.01 && delta_g < 0.01 && delta_b < 0.01 {
                println!("✓ Current matches Legacy v2 closely!");
            } else if delta_r > 0.01 || delta_g > 0.01 || delta_b > 0.01 {
                println!("⚠️  Current vs Legacy v2: ΔR={:.4} ΔG={:.4} ΔB={:.4}", delta_r, delta_g, delta_b);
            }
        }
    }
}
