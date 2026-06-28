// Fractron 9000 - Iterate Kernel
// GPU chaos game loop: pick random branch → apply variations → scatter to histogram
// This shader is concatenated with branch_common.wgsl at compile time.

// ============================================================================
// TYPES AND CONSTANTS
// ============================================================================

struct VariEntry {
    var_id: u32,
    weight: f32,
}

// ============================================================================
// PARAMETERS (passed in via bind groups)
// ============================================================================

@group(0) @binding(0) var<storage, read> flame_data: array<f32>;
@group(0) @binding(1) var<storage, read> branch_data: array<f32>;
@group(0) @binding(2) var<storage, read> variations: array<VariEntry>;
@group(0) @binding(3) var<storage, read_write> histogram: array<atomic<u32>>;
@group(0) @binding(4) var palette_texture: texture_2d<f32>;
@group(0) @binding(5) var palette_sampler: sampler;
@group(0) @binding(6) var<storage, read> render_params: array<u32>;
@group(0) @binding(7) var<storage, read_write> dot_count: array<atomic<u32>>;  // Per-frame dot counter at index [0]

const MAX_ITERATIONS_PER_THREAD: u32 = 1000u;

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

/// Sample chroma (u, v) from the 2D palette texture.
/// Chroma coordinates are directly used as normalized texture coordinates.
/// This matches Legacy Fractron's palette sampling: color = texture(palette, chroma).
fn sample_palette(chroma: vec2<f32>) -> vec3<f32> {
    let color = textureSampleLevel(palette_texture, palette_sampler, chroma, 0.0);
    return color.rgb;
}

/// Pack a float in [0, 1] into a u32 for accumulation.
/// Scale of 16 gives 4-bit color precision per hit, with overflow threshold at ~268M hits/channel.
fn pack_color_channel(val: f32) -> u32 {
    let clamped = clamp(val, 0.0, 1.0);
    return u32(clamped * 16.0);
}

fn pcg_random(state: ptr<function, u32>) -> f32 {
    let x = *state;
    *state = x * 1664525u + 1013904223u;
    let word = ((x >> ((x >> 28u) + 4u)) ^ x) * 277803737u;
    return f32((word >> 22u) ^ word) / f32(0xffffffff);
}

// ============================================================================
// VARIATIONS (30 functions matching Legacy IDs)
// ============================================================================

fn var_linear(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    return p;
}

fn var_sinusoidal(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    return vec2<f32>(sin(p.x), sin(p.y));
}

fn var_spherical(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r2 = dot(p, p);
    return p / (r2 + 1e-6);
}

fn var_swirl(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r2 = dot(p, p);
    let c = cos(r2);
    let s = sin(r2);
    return vec2<f32>(c * p.x - s * p.y, s * p.x + c * p.y);
}

fn var_horseshoe(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    let t = atan2(p.y, p.x);
    return vec2<f32>((1.0 / (r + 1e-6)) * cos(t), (1.0 / (r + 1e-6)) * sin(t));
}

fn var_polar(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    let t = atan2(p.y, p.x);
    return vec2<f32>(t / 3.14159265, r - 1.0);
}

fn var_handkerchief(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    let t = atan2(p.y, p.x);
    return vec2<f32>(r * sin(t + r), r * cos(t - r));
}

fn var_heart(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    let t = atan2(p.y, p.x);
    let h = -cos(t);
    return vec2<f32>(r * h * sin(t), r * h * cos(t));
}

fn var_disc(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    let t = atan2(p.y, p.x);
    let nt = t / 3.14159265;
    return vec2<f32>(nt * sin(3.14159265 * r), nt * cos(3.14159265 * r));
}

fn var_spiral(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    let t = atan2(p.y, p.x);
    return vec2<f32>((1.0 / (r + 1e-6)) * cos(t + r), (1.0 / (r + 1e-6)) * sin(t + r));
}

fn var_hyperbolic(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p) + 1e-6;
    let t = atan2(p.y, p.x);
    return vec2<f32>(sin(t) / r, r * cos(t));
}

fn var_diamond(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    let t = atan2(p.y, p.x);
    return vec2<f32>(sin(t) * cos(r), cos(t) * sin(r));
}

fn var_ex(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    let t = atan2(p.y, p.x);
    let p0 = cos(t);
    let p1 = sin(t + r);
    return vec2<f32>(r * (p0 * p0 * p0 + p1 * p1 * p1), r * (p0 * p0 * p0 - p1 * p1 * p1));
}

fn var_julia(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = sqrt(length(p));
    let t = atan2(p.y, p.x);
    return vec2<f32>(r * cos(t * 0.5), r * sin(t * 0.5));
}

fn var_bent(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    var x = p.x;
    var y = p.y;
    if x < 0.0 { x = x * 2.0; }
    if y < 0.0 { y = y * 0.5; }
    return vec2<f32>(x, y);
}

fn var_waves(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    return vec2<f32>(p.x + 0.25 * sin(p.y), p.y + 0.25 * sin(p.x));
}

fn var_fisheye(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = 2.0 / (length(p) + 1.0);
    return vec2<f32>(r * p.y, r * p.x);
}

fn var_popcorn(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let c = 0.3;
    return vec2<f32>(p.x + c * sin(tan(3.0 * p.y)), p.y + c * sin(tan(3.0 * p.x)));
}

fn var_exponential(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let e = exp(p.x - 1.0);
    return vec2<f32>(e * cos(3.14159265 * p.y), e * sin(3.14159265 * p.y));
}

fn var_power(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    let t = atan2(p.y, p.x);
    let rp = pow(r, sin(t));
    return vec2<f32>(rp * cos(t), rp * sin(t));
}

fn var_cosine(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    return vec2<f32>(cos(3.14159265 * p.x) * cosh(p.y), -sin(3.14159265 * p.x) * sinh(p.y));
}

fn var_eyefish(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = 2.0 / (length(p) + 1.0);
    return vec2<f32>(r * p.x, r * p.y);
}

fn var_bubble(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = 4.0 / (dot(p, p) + 4.0);
    return vec2<f32>(r * p.x, r * p.y);
}

fn var_cylinder(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    return vec2<f32>(sin(p.x), p.y);
}

fn var_noise(p: vec2<f32>, rand: f32) -> vec2<f32> {
    return vec2<f32>(p.x * rand, p.y * rand);
}

fn var_blur(p: vec2<f32>, rand: f32) -> vec2<f32> {
    let t = 6.28318530718 * rand;
    return vec2<f32>(cos(t) * rand, sin(t) * rand);
}

fn var_gaussian_blur(p: vec2<f32>, rand: f32) -> vec2<f32> {
    var sum_x = 0.0;
    var sum_y = 0.0;
    for (var i = 0u; i < 4u; i++) {
        let r = rand * f32(i + 1u) / 4.0;
        sum_x = sum_x + r - 0.5;
        sum_y = sum_y + r - 0.5;
    }
    return vec2<f32>(sum_x, sum_y);
}

// Placeholder Fractron-specific variations
fn var_orb9000(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = pow(length(p), 0.5);
    return p * r;
}

fn var_ripple9000(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r = length(p);
    return vec2<f32>(p.x + 0.1 * sin(r * 10.0), p.y + 0.1 * cos(r * 10.0));
}

fn var_bulge9000(p: vec2<f32>, _weight: f32) -> vec2<f32> {
    let r2 = dot(p, p);
    let s = 1.0 + r2 * 0.1;
    return p * s;
}

// ============================================================================
// VARIATION DISPATCH (VAR_ID -> function)
// ============================================================================

fn apply_variation(var_id: u32, p: vec2<f32>, weight: f32, rand: f32) -> vec2<f32> {
    var result = vec2<f32>(0.0);
    
    switch var_id {
        case 0u: { result = var_linear(p, weight); }
        case 1u: { result = var_sinusoidal(p, weight); }
        case 2u: { result = var_spherical(p, weight); }
        case 3u: { result = var_swirl(p, weight); }
        case 4u: { result = var_horseshoe(p, weight); }
        case 5u: { result = var_polar(p, weight); }
        case 6u: { result = var_handkerchief(p, weight); }
        case 7u: { result = var_heart(p, weight); }
        case 8u: { result = var_disc(p, weight); }
        case 9u: { result = var_spiral(p, weight); }
        case 10u: { result = var_hyperbolic(p, weight); }
        case 11u: { result = var_diamond(p, weight); }
        case 12u: { result = var_ex(p, weight); }
        case 13u: { result = var_julia(p, weight); }
        case 14u: { result = var_bent(p, weight); }
        case 15u: { result = var_waves(p, weight); }
        case 16u: { result = var_fisheye(p, weight); }
        case 17u: { result = var_popcorn(p, weight); }
        case 18u: { result = var_exponential(p, weight); }
        case 19u: { result = var_power(p, weight); }
        case 20u: { result = var_cosine(p, weight); }
        case 21u: { result = var_eyefish(p, weight); }
        case 22u: { result = var_bubble(p, weight); }
        case 23u: { result = var_cylinder(p, weight); }
        case 24u: { result = var_noise(p, rand); }
        case 25u: { result = var_blur(p, rand); }
        case 26u: { result = var_gaussian_blur(p, rand); }
        case 27u: { result = var_orb9000(p, weight); }
        case 28u: { result = var_ripple9000(p, weight); }
        case 29u: { result = var_bulge9000(p, weight); }
        default: { result = p; }
    }
    
    return result * weight;
}

// ============================================================================
// MAIN COMPUTE KERNEL
// ============================================================================

@compute @workgroup_size(256)
fn main(@builtin(global_invocation_id) gid: vec3<u32>) {
    let thread_id = gid.x;
    let frame_count = render_params[3u];  // Use frame counter for RNG seed variation
    // Each frame gets a different random sequence by varying the seed with frame_count
    var state = thread_id + 12345u + frame_count * 654321u;
    
    // Read flame parameters from flat array
    let flame = read_flame();
    
    // Unpack histogram dimensions once — constant for all iterations in this invocation
    let hist_params = unpack_render_params(vec4<u32>(render_params[0u], render_params[1u], render_params[2u], 0u));
    let hist_width = hist_params.hist_width;
    let hist_height = hist_params.hist_height;
    
    // Random starting point in [-1, 1] x [-1, 1]
    var p = vec2<f32>(
        pcg_random(&state) * 2.0 - 1.0,
        pcg_random(&state) * 2.0 - 1.0,
    );
    var color = vec2<f32>(0.5, 0.5);
    
    // Iterate
    for (var iter = 0u; iter < MAX_ITERATIONS_PER_THREAD; iter++) {
        // Pick random branch
        let branch_idx = u32(pcg_random(&state) * f32(flame.branch_count));
        let safe_branch_idx = min(branch_idx, flame.branch_count - 1u);
        let branch = read_branch(safe_branch_idx);
        
        // Apply pre-affine transform
        let t = apply_affine(p, branch.pre_affine);
        
        // Precompute polar coordinates for variations
        // TODO: Verify atan2(y, x) matches Legacy behavior—may need adjustment once UI is stable for testing
        let theta = atan2(t.y, t.x);
        let rsq = dot(t, t);
        let r = sqrt(rsq);
        
        // Apply variations and accumulate weighted results
        var result = vec2<f32>(0.0);
        for (var vi = 0u; vi < branch.var_count; vi++) {
            let var_idx = branch.var_offset + vi;
            let var_entry = variations[var_idx];
            
            if var_entry.weight > 0.0 {
                let random_val = pcg_random(&state);
                let var_result = apply_variation(var_entry.var_id, t, var_entry.weight, random_val);
                result = result + var_result;
            }
        }
        
        // Apply post-affine transform to the accumulated variation result
        p = apply_affine(result, branch.post_affine);
        
        // Update color
        let new_color = branch.chroma + (color - branch.chroma) * (1.0 - branch.color_weight);
        color = new_color;
        
        // Skip first few points (transient)
        if iter < 20u {
            continue;
        }
        
        // Apply vps_transform: single matrix multiply maps fractal space → histogram pixel coords
        let pixel_pos = apply_affine(p, flame.vps_transform);
        let hist_x_i = i32(pixel_pos.x);
        let hist_y_i = i32(pixel_pos.y);

        if hist_x_i >= 0 && hist_x_i < i32(hist_width) && hist_y_i >= 0 && hist_y_i < i32(hist_height) {
            let hist_x = u32(hist_x_i);
            let hist_y = u32(hist_y_i);
            let pixel_idx_base = (hist_y * hist_width + hist_x) * 4u;
            
            // Convert chroma to RGB and accumulate
            let rgb = sample_palette(color);
            let r_packed = pack_color_channel(rgb.x);
            let g_packed = pack_color_channel(rgb.y);
            let b_packed = pack_color_channel(rgb.z);
            
            // Accumulate R, G, B, and hit count to separate u32 channels
            atomicAdd(&histogram[pixel_idx_base + 0u], r_packed);
            atomicAdd(&histogram[pixel_idx_base + 1u], g_packed);
            atomicAdd(&histogram[pixel_idx_base + 2u], b_packed);
            atomicAdd(&histogram[pixel_idx_base + 3u], 1u);
            
            // Increment the per-frame dot counter
            atomicAdd(&dot_count[0u], 1u);
        }
    }
}
