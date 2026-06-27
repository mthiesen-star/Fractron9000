// Shared structures and utilities for branch data handling
// This file is concatenated with individual shaders at compile time.

struct Affine {
    row0: vec4<f32>,  // [a, b, e, padding]
    row1: vec4<f32>,  // [c, d, f, padding]
}

struct FlameData {
    camera_transform: Affine,
    brightness: f32,
    gamma: f32,
    vibrancy: f32,
    background: vec4<f32>,
    branch_count: u32,
    // TODO: vps_transform is a derived rendering param (flame + resolution), not a pure flame
    // property. Move it to a dedicated SceneData buffer in a future refactor.
    vps_transform: Affine,
}

struct RenderParams {
    hist_width: u32,
    hist_height: u32,
    total_iterations: u32,
}

struct BranchData {
    pre_affine: Affine,
    post_affine: Affine,
    chroma: vec2<f32>,
    weight: f32,
    color_weight: f32,
    var_count: u32,
    var_offset: u32,
}

/// Apply 2D affine transform: [a, b, e; c, d, f] * [x, y, 1]^T
fn apply_affine(p: vec2<f32>, t: Affine) -> vec2<f32> {
    return vec2<f32>(
        t.row0.x * p.x + t.row0.y * p.y + t.row0.z,
        t.row1.x * p.x + t.row1.y * p.y + t.row1.z,
    );
}

/// Read flame parameters from flat f32 array (18 elements).
/// Each shader must declare: @group(0) @binding(0) var<storage, read> flame_data: array<f32>;
/// Layout:
/// [0-3]:   camera_transform.row0 (a, b, e, padding)
/// [4-7]:   camera_transform.row1 (c, d, f, padding)
/// [8]:     brightness
/// [9]:     gamma
/// [10]:    vibrancy
/// [11]:    _params_padding
/// [12-15]: background (r, g, b, a)
/// [16]:    branch_count (bitcast as f32)
/// [17]:    reserved
/// [18-21]: vps_transform.row0 (a, b, e, padding)  — fractal→pixel mapping
/// [22-25]: vps_transform.row1 (c, d, f, padding)
fn read_flame() -> FlameData {
    var flame: FlameData;
    
    // camera_transform [0-7]
    flame.camera_transform.row0 = vec4<f32>(
        flame_data[0u],  // a
        flame_data[1u],  // b
        flame_data[2u],  // e
        flame_data[3u]   // padding
    );
    flame.camera_transform.row1 = vec4<f32>(
        flame_data[4u],  // c
        flame_data[5u],  // d
        flame_data[6u],  // f
        flame_data[7u]   // padding
    );
    
    // params [8-11]
    flame.brightness = flame_data[8u];
    flame.gamma = flame_data[9u];
    flame.vibrancy = flame_data[10u];
    
    // background [12-15]
    flame.background = vec4<f32>(
        flame_data[12u],
        flame_data[13u],
        flame_data[14u],
        flame_data[15u]
    );
    
    // counters [16-17] (bitcast from f32)
    flame.branch_count = bitcast<u32>(flame_data[16u]);
    
    // vps_transform [18-25]: fractal → pixel-space mapping (screenTransform × cameraTransform)
    flame.vps_transform.row0 = vec4<f32>(
        flame_data[18u], flame_data[19u], flame_data[20u], flame_data[21u]
    );
    flame.vps_transform.row1 = vec4<f32>(
        flame_data[22u], flame_data[23u], flame_data[24u], flame_data[25u]
    );
    
    return flame;
}

/// Unpack runtime render parameters from a compact u32 payload.
/// Layout:
/// [0]: hist_width
/// [1]: hist_height
/// [2]: total_iterations
fn unpack_render_params(raw: vec4<u32>) -> RenderParams {
    var params: RenderParams;
    params.hist_width = max(raw.x, 1u);
    params.hist_height = max(raw.y, 1u);
    params.total_iterations = max(raw.z, 1u);
    return params;
}

/// Read a branch from the flat f32 buffer (18 elements per branch).
/// Each shader must declare: @group(0) @binding(0) var<storage, read> branch_data: array<f32>;
fn read_branch(idx: u32) -> BranchData {
    let offset = idx * 18u;
    var branch: BranchData;
    
    // pre_affine [0-5]
    branch.pre_affine.row0 = vec4<f32>(
        branch_data[offset + 0u],  // a
        branch_data[offset + 1u],  // b
        branch_data[offset + 2u],  // e
        0.0
    );
    branch.pre_affine.row1 = vec4<f32>(
        branch_data[offset + 3u],  // c
        branch_data[offset + 4u],  // d
        branch_data[offset + 5u],  // f
        0.0
    );
    
    // post_affine [6-11]
    branch.post_affine.row0 = vec4<f32>(
        branch_data[offset + 6u],  // a
        branch_data[offset + 7u],  // b
        branch_data[offset + 8u],  // e
        0.0
    );
    branch.post_affine.row1 = vec4<f32>(
        branch_data[offset + 9u],  // c
        branch_data[offset + 10u], // d
        branch_data[offset + 11u], // f
        0.0
    );
    
    // metadata [12-17]
    branch.chroma = vec2<f32>(
        branch_data[offset + 12u],
        branch_data[offset + 13u]
    );
    branch.weight = branch_data[offset + 14u];
    branch.color_weight = branch_data[offset + 15u];
    branch.var_count = bitcast<u32>(branch_data[offset + 16u]);
    branch.var_offset = bitcast<u32>(branch_data[offset + 17u]);
    
    return branch;
}
