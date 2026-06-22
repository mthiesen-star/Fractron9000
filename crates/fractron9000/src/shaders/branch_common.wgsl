// Shared structures and utilities for branch data handling
// This file is concatenated with individual shaders at compile time.

struct Affine {
    row0: vec4<f32>,  // [a, b, e, padding]
    row1: vec4<f32>,  // [c, d, f, padding]
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
