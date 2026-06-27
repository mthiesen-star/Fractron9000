// Fractron 9000 - Tonemap Kernel
// Converts raw histogram hits to tone-mapped RGBA texture with log scaling, gamma, vibrancy
// (Affine struct and read_flame() provided by branch_common.wgsl)

@group(0) @binding(0) var<storage, read> flame_data: array<f32>;
@group(0) @binding(1) var<storage, read> histogram: array<u32>;
@group(0) @binding(2) var<storage, read> branch_data: array<f32>;  // Needed by branch_common, not used by tonemap
@group(0) @binding(3) var output_texture: texture_storage_2d<rgba8unorm, write>;
@group(0) @binding(4) var<storage, read> render_params: array<u32>;  // [0]=width, [1]=height, [2]=frame_count, [3]=total_iters_low, [4]=total_iters_high, [5]=reserved

const PIXEL_AREA: f32 = 1.0;
const C1: f32 = 1.0;

const TONE_C1: f32 = 0.5;
const TONE_C2: f32 = 64.0;
const SUB_PIXEL_SAMPLES: f32 = 4.0;  // 2x2 sub-pixel sampling

/// Log base 10
fn log_b10(x: f32) -> f32 {
    return log(x) / 2.30258509;
}

fn tone_map(r: f32, g: f32, b: f32, count: f32, flame_params: vec3<f32>, total_iteration_count: f32) -> vec3<f32> {
    if count < 0.5 {
        return vec3<f32>(0.0);
    }
    
    let brightness = flame_params.x;
    let gamma = flame_params.y;
    let vibrancy = flame_params.z;
    
    // Legacy tone mapping formula (from kernels.cl):
    // scale_constant = TONE_C2 * invPixArea * SUB_PIXEL_SAMPLES / totalIterationCount
    // Compute invPixArea as the absolute value of the camera transform determinant
    let flame = read_flame();
    let cam_a = flame.camera_transform.row0.x;
    let cam_b = flame.camera_transform.row0.y;
    let cam_c = flame.camera_transform.row1.x;
    let cam_d = flame.camera_transform.row1.y;
    let det = cam_a * cam_d - cam_b * cam_c;
    let inv_pixel_area = abs(det);
    
    // Formula: scale_constant = TONE_C2 * invPixArea * SUB_PIXEL_SAMPLES / totalIterationCount
    let scale_constant = TONE_C2 * inv_pixel_area * SUB_PIXEL_SAMPLES / (total_iteration_count + 1e-6);
    
    // Compute log-intensity for this pixel: log_a = TONE_C1 * brightness * log10(1 + count * scale_constant)
    let log_term = 1.0 + count * scale_constant;
    let log_a = TONE_C1 * brightness * log_b10(log_term);
    
    // Normalize colors by dividing RGB by count (average color per hit)
    let r_avg = r / (count + 1e-6);
    let g_avg = g / (count + 1e-6);
    let b_avg = b / (count + 1e-6);
    
    // Apply the log intensity to each channel
    let log_r = log_a * r_avg;
    let log_g = log_a * g_avg;
    let log_b_val = log_a * b_avg;
    
    // Apply gamma correction: z = pow(log_a, 1/gamma), gamma_factor = z / log_a
    let inv_gamma = 1.0 / gamma;
    let z = pow(log_a, inv_gamma);
    let gamma_factor = z / (log_a + 1e-6);
    
    // Apply vibrancy (blend between pure gamma-corrected and scaled color)
    let result_r = clamp(mix(pow(log_r, inv_gamma), gamma_factor * log_r, vibrancy), 0.0, 1.0);
    let result_g = clamp(mix(pow(log_g, inv_gamma), gamma_factor * log_g, vibrancy), 0.0, 1.0);
    let result_b = clamp(mix(pow(log_b_val, inv_gamma), gamma_factor * log_b_val, vibrancy), 0.0, 1.0);
    
    return vec3<f32>(result_r, result_g, result_b);
}

// Debug version of tone map. Each pixel is either ON if count >= 0.5 or off otherwise
fn tone_map_binary_debug(r: f32, g: f32, b: f32, count: f32, flame_params: vec3<f32>, total_dot_count: f32) -> vec3<f32> {
    if count < 0.5 {
        return vec3<f32>(0.0);
    }
    return vec3<f32>(1.0);
}


@compute @workgroup_size(16, 16)
fn main(@builtin(global_invocation_id) gid: vec3<u32>) {
    let pixel_x = gid.x;
    let pixel_y = gid.y;
    let flame = read_flame();
    let params = unpack_render_params(vec4<u32>(render_params[0u], render_params[1u], render_params[2u], 0u));
    let hist_width = params.hist_width;
    let hist_height = params.hist_height;
    
    if pixel_x >= hist_width || pixel_y >= hist_height {
        return;
    }
    
    let pixel_idx_base = (pixel_y * hist_width + pixel_x) * 4u;
    
    // Read accumulated R, G, B, count from histogram (4 u32s per pixel)
    var r_accum = f32(histogram[pixel_idx_base + 0u]);
    var g_accum = f32(histogram[pixel_idx_base + 1u]);
    var b_accum = f32(histogram[pixel_idx_base + 2u]);
    var count_accum = f32(histogram[pixel_idx_base + 3u]);
    
    // Apply upscaling factor to raw accumulated values (like Legacy does)
    // This expands the range to work well with the tone mapping formula
    let up_scale_factor = 4.0;
    r_accum *= up_scale_factor;
    g_accum *= up_scale_factor;
    b_accum *= up_scale_factor;
    count_accum *= up_scale_factor;
    
    // Extract total iteration count from render_params (elements [3] and [4] are low and high u32s of u64)
    let total_iters_low = f32(render_params[3u]);
    let total_iters_high = f32(render_params[4u]);
    let total_iteration_count_f32 = total_iters_low + total_iters_high * 4294967296.0;  // 2^32
    
    // Apply tone mapping
    let flame_params = vec3<f32>(flame.brightness, flame.gamma, flame.vibrancy);
    let mapped = tone_map(r_accum, g_accum, b_accum, count_accum, flame_params, total_iteration_count_f32);
    
    // Blend with background
    let bg = flame.background.xyz;
    let final_color = mix(bg, mapped, f32(count_accum > 0.5));
    
    textureStore(output_texture, vec2<i32>(i32(pixel_x), i32(pixel_y)), vec4<f32>(final_color, 1.0));
}
