// GPU rendering pipeline: compute shaders + buffer management

use wgpu::*;
use wgpu::util::DeviceExt;
use glam::Mat3;
use fractal_core::flame::Flame;
use std::num::NonZeroU32;

// ============================================================================
// GPU BUFFER STRUCTURES (matching WGSL)
// ============================================================================

#[repr(C)]
#[derive(Copy, Clone, bytemuck::Pod, bytemuck::Zeroable)]
pub struct GpuAffine {
    pub row0: [f32; 4],
    pub row1: [f32; 4],
}

impl GpuAffine {
    pub fn from_mat3(m: Mat3) -> Self {
        // Convert column-major Mat3 to row-based GPU format
        // Mat3 columns are: [a,c,0], [b,d,0], [e,f,1]
        // We need rows: [a,b,e] and [c,d,f]
        Self {
            row0: [m.x_axis.x, m.y_axis.x, m.z_axis.x, 0.0],
            row1: [m.x_axis.y, m.y_axis.y, m.z_axis.y, 0.0],
        }
    }
}

#[repr(C)]
#[derive(Copy, Clone, bytemuck::Pod, bytemuck::Zeroable)]
pub struct GpuBranch {
    pub pre_affine: GpuAffine,
    pub post_affine: GpuAffine,
    pub chroma: [f32; 2],
    pub weight: f32,
    pub color_weight: f32,
    pub var_count: u32,
    pub var_offset: u32,
    pub _padding: u32,
}

#[repr(C)]
#[derive(Copy, Clone, bytemuck::Pod, bytemuck::Zeroable)]
pub struct GpuVariEntry {
    pub var_id: u32,
    pub weight: f32,
}

#[repr(C)]
#[derive(Copy, Clone, bytemuck::Pod, bytemuck::Zeroable)]
pub struct GpuFlame {
    pub camera_transform: GpuAffine,
    pub params: [f32; 4],      // [brightness, gamma, vibrancy, _padding]
    pub background: [f32; 4],  // [bg_r, bg_g, bg_b, bg_a]
    pub branch_count: u32,
    pub total_iterations: u32,
    pub _padding: [u32; 2],
}

const HIST_WIDTH: u32 = 1024;
const HIST_HEIGHT: u32 = 768;

// ============================================================================
// GPU RENDERER
// ============================================================================

pub struct GpuRenderer {
    // Pipelines
    iterate_pipeline: ComputePipeline,
    tonemap_pipeline: ComputePipeline,
    
    // Buffers
    flame_buffer: Buffer,
    branches_buffer: Buffer,
    variations_buffer: Buffer,
    histogram_buffer: Buffer,
    
    // Output texture
    output_texture: Texture,
    output_texture_view: TextureView,
    
    // Bind groups
    iterate_bind_group: BindGroup,
    tonemap_bind_group: BindGroup,
}

impl GpuRenderer {
    /// Initialize GPU renderer (must be called with initialized wgpu device/queue)
    pub async fn new(device: &Device, _queue: &Queue, flame: &Flame) -> Result<Self, String> {
        // Compile shaders
        let iterate_module = device.create_shader_module(ShaderModuleDescriptor {
            label: Some("iterate_kernel"),
            source: ShaderSource::Wgsl(std::borrow::Cow::Borrowed(include_str!("shaders/iterate.wgsl"))),
        });
        
        let tonemap_module = device.create_shader_module(ShaderModuleDescriptor {
            label: Some("tonemap_kernel"),
            source: ShaderSource::Wgsl(std::borrow::Cow::Borrowed(include_str!("shaders/tonemap.wgsl"))),
        });
        
        // Build GPU buffer data from Rust structures
        let gpu_flame = Self::flame_to_gpu(flame);
        let (gpu_branches, gpu_variations) = Self::flame_to_gpu_branches(flame);
        
        // Create buffers
        let flame_buffer = device.create_buffer_init(&util::BufferInitDescriptor {
            label: Some("flame_uniform"),
            contents: bytemuck::cast_slice(&[gpu_flame]),
            usage: BufferUsages::UNIFORM | BufferUsages::COPY_DST,
        });
        
        let branches_buffer = device.create_buffer_init(&util::BufferInitDescriptor {
            label: Some("branches_ssbo"),
            contents: bytemuck::cast_slice(&gpu_branches),
            usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
        });
        
        let variations_buffer = device.create_buffer_init(&util::BufferInitDescriptor {
            label: Some("variations_ssbo"),
            contents: bytemuck::cast_slice(&gpu_variations),
            usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
        });
        
        let histogram_buffer = device.create_buffer(&BufferDescriptor {
            label: Some("histogram_ssbo"),
            size: (HIST_WIDTH * HIST_HEIGHT * 4) as u64,
            usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
            mapped_at_creation: true,
        });
        
        // Initialize histogram to zeros
        {
            let mut mapping = histogram_buffer.slice(..).get_mapped_range_mut();
            mapping.fill(0u8);
        }
        histogram_buffer.unmap();
        
        // Create output texture
        let output_texture = device.create_texture(&TextureDescriptor {
            label: Some("output_texture"),
            size: Extent3d {
                width: HIST_WIDTH,
                height: HIST_HEIGHT,
                depth_or_array_layers: 1,
            },
            mip_level_count: 1,
            sample_count: 1,
            dimension: TextureDimension::D2,
            format: TextureFormat::Rgba8Unorm,
            usage: TextureUsages::STORAGE_BINDING | TextureUsages::TEXTURE_BINDING | TextureUsages::COPY_SRC,
            view_formats: &[],
        });
        
        let output_texture_view = output_texture.create_view(&TextureViewDescriptor::default());
        
        // Create iterate bind group
        let iterate_bind_group_layout = device.create_bind_group_layout(&BindGroupLayoutDescriptor {
            label: Some("iterate_bgl"),
            entries: &[
                BindGroupLayoutEntry {
                    binding: 0,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Uniform,
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 1,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 2,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 3,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: false },
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
            ],
        });
        
        let iterate_bind_group = device.create_bind_group(&BindGroupDescriptor {
            label: Some("iterate_bg"),
            layout: &iterate_bind_group_layout,
            entries: &[
                BindGroupEntry { binding: 0, resource: flame_buffer.as_entire_binding() },
                BindGroupEntry { binding: 1, resource: branches_buffer.as_entire_binding() },
                BindGroupEntry { binding: 2, resource: variations_buffer.as_entire_binding() },
                BindGroupEntry { binding: 3, resource: histogram_buffer.as_entire_binding() },
            ],
        });
        
        let iterate_pipeline = device.create_compute_pipeline(&ComputePipelineDescriptor {
            label: Some("iterate_pipeline"),
            layout: Some(&device.create_pipeline_layout(&PipelineLayoutDescriptor {
                label: Some("iterate_layout"),
                bind_group_layouts: &[&iterate_bind_group_layout],
                push_constant_ranges: &[],
            })),
            module: &iterate_module,
            entry_point: "main",
            cache: None,
            compilation_options: Default::default(),
        });
        
        // Create tonemap bind group
        let tonemap_bind_group_layout = device.create_bind_group_layout(&BindGroupLayoutDescriptor {
            label: Some("tonemap_bgl"),
            entries: &[
                BindGroupLayoutEntry {
                    binding: 0,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Uniform,
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 1,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 2,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::StorageTexture {
                        access: StorageTextureAccess::WriteOnly,
                        format: TextureFormat::Rgba8Unorm,
                        view_dimension: TextureViewDimension::D2,
                    },
                    count: None,
                },
            ],
        });
        
        let tonemap_bind_group = device.create_bind_group(&BindGroupDescriptor {
            label: Some("tonemap_bg"),
            layout: &tonemap_bind_group_layout,
            entries: &[
                BindGroupEntry { binding: 0, resource: flame_buffer.as_entire_binding() },
                BindGroupEntry { binding: 1, resource: histogram_buffer.as_entire_binding() },
                BindGroupEntry { binding: 2, resource: BindingResource::TextureView(&output_texture_view) },
            ],
        });
        
        let tonemap_pipeline = device.create_compute_pipeline(&ComputePipelineDescriptor {
            label: Some("tonemap_pipeline"),
            layout: Some(&device.create_pipeline_layout(&PipelineLayoutDescriptor {
                label: Some("tonemap_layout"),
                bind_group_layouts: &[&tonemap_bind_group_layout],
                push_constant_ranges: &[],
            })),
            module: &tonemap_module,
            entry_point: "main",
            cache: None,
            compilation_options: Default::default(),
        });
        
        Ok(Self {
            iterate_pipeline,
            tonemap_pipeline,
            flame_buffer,
            branches_buffer,
            variations_buffer,
            histogram_buffer,
            output_texture,
            output_texture_view,
            iterate_bind_group,
            tonemap_bind_group,
        })
    }
    
    pub fn iterate(&self, queue: &Queue, device: &Device, num_threads: u32) {
        // Clear histogram to zeros for this frame
        queue.write_buffer(&self.histogram_buffer, 0, &vec![0u8; (HIST_WIDTH * HIST_HEIGHT * 4) as usize]);
        
        let mut encoder = device.create_command_encoder(&CommandEncoderDescriptor {
            label: Some("iterate_encoder"),
        });
        
        {
            let mut cpass = encoder.begin_compute_pass(&ComputePassDescriptor {
                label: Some("iterate_pass"),
                timestamp_writes: None,
            });
            cpass.set_pipeline(&self.iterate_pipeline);
            cpass.set_bind_group(0, &self.iterate_bind_group, &[]);
            cpass.dispatch_workgroups((num_threads + 255) / 256, 1, 1);
        }
        
        queue.submit(std::iter::once(encoder.finish()));
    }
    
    pub fn tonemap(&self, queue: &Queue, device: &Device) {
        let mut encoder = device.create_command_encoder(&CommandEncoderDescriptor {
            label: Some("tonemap_encoder"),
        });
        
        {
            let mut cpass = encoder.begin_compute_pass(&ComputePassDescriptor {
                label: Some("tonemap_pass"),
                timestamp_writes: None,
            });
            cpass.set_pipeline(&self.tonemap_pipeline);
            cpass.set_bind_group(0, &self.tonemap_bind_group, &[]);
            cpass.dispatch_workgroups((HIST_WIDTH + 15) / 16, (HIST_HEIGHT + 15) / 16, 1);
        }
        
        queue.submit(std::iter::once(encoder.finish()));
    }
    
    pub fn output_texture(&self) -> &Texture {
        &self.output_texture
    }
    
    /// Read the output texture back to CPU as RGBA8 data (synchronous with device.poll)
    pub fn read_output_to_vec(&self, device: &Device, queue: &Queue) -> Vec<u8> {
        let extent = self.output_texture.size();
        let width = extent.width as usize;
        let height = extent.height as usize;
        let bytes_per_pixel = 4;
        
        // Rows must be 256-byte aligned for GPU copies
        let bytes_per_row = (width * bytes_per_pixel).next_multiple_of(256);
        let total_bytes = bytes_per_row * height;
        
        // Create staging buffer
        let staging_buffer = device.create_buffer(&BufferDescriptor {
            label: Some("output_readback_staging"),
            size: total_bytes as u64,
            usage: BufferUsages::COPY_DST | BufferUsages::MAP_READ,
            mapped_at_creation: false,
        });
        
        // Copy texture to staging buffer
        let mut encoder = device.create_command_encoder(&CommandEncoderDescriptor {
            label: Some("readback_encoder"),
        });
        
        encoder.copy_texture_to_buffer(
            ImageCopyTexture {
                texture: &self.output_texture,
                mip_level: 0,
                origin: Origin3d::ZERO,
                aspect: TextureAspect::All,
            },
            ImageCopyBuffer {
                buffer: &staging_buffer,
                layout: ImageDataLayout {
                    offset: 0,
                    bytes_per_row: Some(bytes_per_row as u32),
                    rows_per_image: None,
                },
            },
            extent,
        );
        
        // Submit the copy command
        queue.submit(std::iter::once(encoder.finish()));
        
        // Force GPU to complete all submitted work
        device.poll(wgpu::Maintain::Wait);
        
        // Now request map - it should succeed immediately since work is done
        let mut mapped_successfully = false;
        for _ in 0..5 {
            staging_buffer.slice(..).map_async(MapMode::Read, |_| {});
            
            // Poll again to process the map request
            device.poll(wgpu::Maintain::Wait);
            
            // Try to get the mapped range
            if let Ok(_) = std::panic::catch_unwind(std::panic::AssertUnwindSafe(|| {
                let _range = staging_buffer.slice(..).get_mapped_range();
                // If we got here without panicking, mapping succeeded
                drop(_range);
            })) {
                mapped_successfully = true;
                break;
            }
            
            // Unmap and try again
            staging_buffer.unmap();
        }
        
        if !mapped_successfully {
            eprintln!("Failed to map staging buffer after GPU work");
            return vec![0u8; width * height * bytes_per_pixel];
        }
        
        // Read mapped data, skipping row padding
        let mapped_range = staging_buffer.slice(..).get_mapped_range();
        let mut result = Vec::with_capacity(width * height * bytes_per_pixel);
        for y in 0..height {
            let row_start = y * bytes_per_row;
            let row_data = &mapped_range[row_start..row_start + width * bytes_per_pixel];
            result.extend_from_slice(row_data);
        }
        
        drop(mapped_range);
        staging_buffer.unmap();
        
        result
    }
    
    pub fn output_size(&self) -> (u32, u32) {
        let extent = self.output_texture.size();
        (extent.width, extent.height)
    }
    
    // ========================================================================
    // CONVERSION HELPERS
    // ========================================================================
    
    fn flame_to_gpu(flame: &Flame) -> GpuFlame {
        let result = GpuFlame {
            camera_transform: GpuAffine::from_mat3(flame.camera_transform),
            params: [flame.brightness, flame.gamma, flame.vibrancy, 0.0],
            background: [flame.background.x, flame.background.y, flame.background.z, flame.background.w],
            branch_count: flame.branches.len() as u32,
            total_iterations: 100000000, // Scale based on ~65M iterations/frame at 60 FPS
            _padding: [0; 2],
        };
        eprintln!("GPU Flame: branch_count={}, brightness={}, gamma={}", 
            result.branch_count, result.params[0], result.params[1]);
        result
    }
    
    fn flame_to_gpu_branches(flame: &Flame) -> (Vec<GpuBranch>, Vec<GpuVariEntry>) {
        let mut gpu_branches = Vec::new();
        let mut gpu_variations = Vec::new();
        
        eprintln!("Converting {} branches to GPU format", flame.branches.len());
        
        for (idx, branch) in flame.branches.iter().enumerate() {
            let var_offset = gpu_variations.len() as u32;
            let var_count = branch.variations.len() as u32;
            
            for var_entry in &branch.variations {
                gpu_variations.push(GpuVariEntry {
                    var_id: var_entry.variation.id() as u32,
                    weight: var_entry.weight,
                });
                eprintln!("      var_id={}, weight={}", var_entry.variation.id(), var_entry.weight);
            }
            
            // LOG THE RAW MATRIX BEFORE CONVERSION
            eprintln!("  Branch {} RAW MATRIX:", idx);
            eprintln!("    x_axis: {:?}", branch.pre_affine.x_axis);
            eprintln!("    y_axis: {:?}", branch.pre_affine.y_axis);
            eprintln!("    z_axis: {:?}", branch.pre_affine.z_axis);
            
            let pre_affine_gpu = GpuAffine::from_mat3(branch.pre_affine);
            let post_affine_gpu = GpuAffine::from_mat3(branch.post_affine);
            
            eprintln!("  Branch {}: chroma=[{}, {}]", idx, branch.chroma.x, branch.chroma.y);
            eprintln!("    pre_affine row0: {:?}", pre_affine_gpu.row0);
            eprintln!("    pre_affine row1: {:?}", pre_affine_gpu.row1);
            eprintln!("    weight={}, color_weight={}", branch.weight, branch.color_weight);
            eprintln!("    var_count={}, var_offset={}", var_count, var_offset);
            
            gpu_branches.push(GpuBranch {
                pre_affine: pre_affine_gpu,
                post_affine: post_affine_gpu,
                chroma: [branch.chroma.x, branch.chroma.y],
                weight: branch.weight,
                color_weight: branch.color_weight,
                var_count,
                var_offset,
                _padding: 0,
            });
        }
        
        eprintln!("  Created {} GPU branches, {} total variations", gpu_branches.len(), gpu_variations.len());
        
        (gpu_branches, gpu_variations)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_gpu_affine_identity() {
        let mat = Mat3::IDENTITY;
        let gpu = GpuAffine::from_mat3(mat);
        
        // Identity: rows should be [1, 0, 0] and [0, 1, 0]
        assert_eq!(gpu.row0[0], 1.0, "row0[0] should be 1.0 (x_axis.x)");
        assert_eq!(gpu.row0[1], 0.0, "row0[1] should be 0.0 (y_axis.x)");
        assert_eq!(gpu.row0[2], 0.0, "row0[2] should be 0.0 (z_axis.x)");
        
        assert_eq!(gpu.row1[0], 0.0, "row1[0] should be 0.0 (x_axis.y)");
        assert_eq!(gpu.row1[1], 1.0, "row1[1] should be 1.0 (y_axis.y)");
        assert_eq!(gpu.row1[2], 0.0, "row1[2] should be 0.0 (z_axis.y)");
    }

    #[test]
    fn test_gpu_affine_translation() {
        // Create a mat3 with translation (3, 4)
        let mat = Mat3::from_cols(
            glam::Vec3::new(1.0, 0.0, 0.0),   // x_axis
            glam::Vec3::new(0.0, 1.0, 0.0),   // y_axis
            glam::Vec3::new(3.0, 4.0, 1.0),   // z_axis: [tx, ty, 1]
        );
        let gpu = GpuAffine::from_mat3(mat);
        
        // Should have rows: [1, 0, 3] and [0, 1, 4]
        assert_eq!(gpu.row0[0], 1.0);
        assert_eq!(gpu.row0[1], 0.0);
        assert_eq!(gpu.row0[2], 3.0, "row0[2] should be 3.0 (translation x)");
        
        assert_eq!(gpu.row1[0], 0.0);
        assert_eq!(gpu.row1[1], 1.0);
        assert_eq!(gpu.row1[2], 4.0, "row1[2] should be 4.0 (translation y)");
    }

    #[test]
    fn test_gpu_affine_scale() {
        // Create a mat3 with scale 2x
        let mat = Mat3::from_scale(glam::Vec2::new(2.0, 2.0));
        let gpu = GpuAffine::from_mat3(mat);
        
        // Should have rows: [2, 0, 0] and [0, 2, 0]
        assert_eq!(gpu.row0[0], 2.0, "row0[0] should be 2.0 (x scale)");
        assert_eq!(gpu.row0[1], 0.0);
        assert_eq!(gpu.row1[0], 0.0);
        assert_eq!(gpu.row1[1], 2.0, "row1[1] should be 2.0 (y scale)");
    }

    #[test]
    fn test_gpu_affine_scale_and_translate() {
        // scale(0.5) + translate(-0.8, 0)
        let mat = Mat3::from_cols(
            glam::Vec3::new(0.5, 0.0, 0.0),
            glam::Vec3::new(0.0, 0.5, 0.0),
            glam::Vec3::new(-0.8, 0.0, 1.0),
        );
        let gpu = GpuAffine::from_mat3(mat);
        
        // Should have rows: [0.5, 0, -0.8] and [0, 0.5, 0]
        assert_eq!(gpu.row0[0], 0.5);
        assert_eq!(gpu.row0[1], 0.0);
        assert_eq!(gpu.row0[2], -0.8, "row0[2] should be -0.8");
        
        assert_eq!(gpu.row1[0], 0.0);
        assert_eq!(gpu.row1[1], 0.5);
        assert_eq!(gpu.row1[2], 0.0);
    }

    #[test]
    fn test_histogram_coordinate_mapping() {
        // Test the coordinate mapping formula: (world_pos + 2.0) / 4.0 * HIST_WIDTH
        let world_pos_x = -2.0; // left edge
        let norm_x = (world_pos_x + 2.0) / 4.0;  // (-2 + 2) / 4 = 0
        let hist_x = (norm_x * 1024.0) as u32;
        assert_eq!(hist_x, 0, "world_pos x=-2.0 should map to pixel 0");
        
        let world_pos_x = 0.0; // center
        let norm_x = (world_pos_x + 2.0) / 4.0;  // (0 + 2) / 4 = 0.5
        let hist_x = (norm_x * 1024.0) as u32;
        assert_eq!(hist_x, 512, "world_pos x=0.0 should map to pixel 512");
        
        let world_pos_x = 2.0; // right edge
        let norm_x = (world_pos_x + 2.0) / 4.0;  // (2 + 2) / 4 = 1.0
        let hist_x = (norm_x * 1024.0) as u32;
        assert_eq!(hist_x, 1024, "world_pos x=2.0 should map to pixel 1024");
    }
}
