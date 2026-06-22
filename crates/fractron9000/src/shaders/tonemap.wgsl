// Fractron 9000 - Tonemap Kernel
// Converts raw histogram hits to tone-mapped RGBA texture with log scaling, gamma, vibrancy
// (Affine struct and read_flame() provided by branch_common.wgsl)

@group(0) @binding(0) var<storage, read> flame_data: array<f32>;
@group(0) @binding(1) var<storage, read> histogram: array<u32>;
@group(0) @binding(2) var<storage, read> branch_data: array<f32>;  // Needed by branch_common, not used by tonemap
@group(0) @binding(3) var output_texture: texture_storage_2d<rgba8unorm, write>;

const HIST_WIDTH: u32 = 1024u;
const HIST_HEIGHT: u32 = 768u;
const PIXEL_AREA: f32 = 1.0;
const C1: f32 = 1.0;

fn tone_map(raw: u32, flame_params: vec3<f32>, total_iterations: u32) -> vec3<f32> {
    let raw_f = f32(raw);
    
    if raw_f < 1.0 {
        return vec3<f32>(0.0);
    }
    
    let brightness = flame_params.x;
    let gamma = flame_params.y;
    let vibrancy = flame_params.z;
    
    // Scale hit count by brightness and iteration count
    // Lower scale = darker, needs more hits to brighten
    let scale = brightness / (f32(total_iterations) + 1e-6);
    let scaled = raw_f * scale;
    
    // Log scale for better dynamic range
    let log_val = log(scaled + 1.0);
    
    // Apply gamma correction
    let gamma_corrected = pow(log_val, 1.0 / gamma);
    
    // Apply vibrancy (saturation boost)
    let with_vibrancy = mix(gamma_corrected, gamma_corrected * 1.5, vibrancy);
    
    // Use a blue-shifted color palette (Apophysis style)
    // Vary RGB based on hit intensity
    let color = vec3<f32>(
        sin(with_vibrancy * 3.14159 * 0.5),                   // Red channel
        sin(with_vibrancy * 3.14159 * 0.5 + 2.0) * 0.7,       // Green channel
        sin(with_vibrancy * 3.14159 * 0.5 + 4.0) * 1.0        // Blue channel
    );
    
    return clamp(color * with_vibrancy, vec3<f32>(0.0), vec3<f32>(1.0));
}

@compute @workgroup_size(16, 16)
fn main(@builtin(global_invocation_id) gid: vec3<u32>) {
    let pixel_x = gid.x;
    let pixel_y = gid.y;
    
    if pixel_x >= HIST_WIDTH || pixel_y >= HIST_HEIGHT {
        return;
    }
    
    let pixel_idx = pixel_y * HIST_WIDTH + pixel_x;
    let hit_count = histogram[pixel_idx];
    
    // Read flame parameters from flat array
    let flame = read_flame();
    
    // Apply tone mapping
    let flame_params = vec3<f32>(flame.brightness, flame.gamma, flame.vibrancy);
    let mapped = tone_map(hit_count, flame_params, flame.total_iterations);
    
    // Blend with background
    let bg = flame.background.xyz;
    let final_color = mix(bg, mapped, f32(hit_count > 0u));
    
    textureStore(output_texture, vec2<i32>(i32(pixel_x), i32(pixel_y)), vec4<f32>(final_color, 1.0));
}
