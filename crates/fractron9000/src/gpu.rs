// GPU rendering pipeline: compute shaders + buffer management

use wgpu::*;
use wgpu::util::DeviceExt;
use glam::Mat3;
use fractal_core::flame::Flame;

const DEFAULT_TOTAL_ITERATIONS: u32 = 100000000;

// ============================================================================
// GPU BUFFER STRUCTURES (internal helpers only - no repr(C) needed)
// ============================================================================

/// GPU-friendly affine transform: row-major layout (internal helper, not sent to GPU as struct)
#[derive(Copy, Clone)]
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
pub struct GpuVariEntry {
    pub var_id: u32,
    pub weight: f32,
}

// ============================================================================
// GPU RENDERER
// ============================================================================

#[allow(dead_code)]
pub struct GpuRenderer {
    // Pipelines
    iterate_pipeline: ComputePipeline,
    tonemap_pipeline: ComputePipeline,
    
    // Buffers
    flame_buffer: Buffer,
    render_params_buffer: Buffer,
    branches_buffer: Buffer,
    variations_buffer: Buffer,
    histogram_buffer: Buffer,
    
    // Palette texture for chroma->RGB mapping (2D Legacy palette)
    palette_texture: Texture,
    palette_texture_view: TextureView,
    palette_sampler: Sampler,
    
    // Output texture
    output_texture: Texture,
    output_texture_view: TextureView,
    output_width: u32,
    output_height: u32,
    
    // Bind groups
    iterate_bind_group: BindGroup,
    tonemap_bind_group: BindGroup,
}

impl GpuRenderer {
    /// Initialize GPU renderer with output dimensions in physical pixels.
    pub fn new(
        device: &Device,
        queue: &Queue,
        flame: &Flame,
        output_width: u32,
        output_height: u32,
    ) -> Result<Self, String> {
        let output_width = output_width.clamp(32, 8192);
        let output_height = output_height.clamp(32, 8192);

        // Compile shaders with shared branch_common utilities concatenated
        let branch_common = include_str!("shaders/branch_common.wgsl");
        let iterate_src = include_str!("shaders/iterate.wgsl");
        let tonemap_src = include_str!("shaders/tonemap.wgsl");
        
        // Concatenate: common utilities first, then individual shader
        let iterate_code = format!("{}\n{}", branch_common, iterate_src);
        
        let iterate_module = device.create_shader_module(ShaderModuleDescriptor {
            label: Some("iterate_kernel"),
            source: ShaderSource::Wgsl(std::borrow::Cow::Owned(iterate_code)),
        });
        
        let tonemap_code = format!("{}\n{}", branch_common, tonemap_src);
        let tonemap_module = device.create_shader_module(ShaderModuleDescriptor {
            label: Some("tonemap_kernel"),
            source: ShaderSource::Wgsl(std::borrow::Cow::Owned(tonemap_code)),
        });
        
        // Build GPU buffer data from Rust structures
        let gpu_flame_flat = Self::flame_to_gpu_flat(flame);
        let (gpu_branches, gpu_variations) = Self::flame_to_gpu_branches(flame);
        
        // DEBUG: Print buffer layout
        eprintln!("\n=== BUFFER LAYOUT DIAGNOSTICS ===");
        eprintln!("Flame buffer: {} f32s ({} bytes) - flat array packing", 
                 gpu_flame_flat.len(), gpu_flame_flat.len() * 4);
        eprintln!("  [0-3]:   camera_transform.row0");
        eprintln!("  [4-7]:   camera_transform.row1");
        eprintln!("  [8-11]:  params (brightness, gamma, vibrancy, padding)");
        eprintln!("  [12-15]: background (r, g, b, a)");
        eprintln!("  [16-17]: branch_count, reserved (as bitcast f32)");
        eprintln!();
        eprintln!("Branches buffer: {} f32s ({} bytes) - {} branches", 
                 gpu_branches.len(), gpu_branches.len() * 4, gpu_branches.len() / 18);
        eprintln!("Variations buffer: {} entries ({} bytes)", 
                 gpu_variations.len(), gpu_variations.len() * 8);
        
        // Create buffers
        let flame_buffer = device.create_buffer_init(&util::BufferInitDescriptor {
            label: Some("flame_ssbo"),
            contents: bytemuck::cast_slice(&gpu_flame_flat),
            usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
        });

        let render_params = [output_width, output_height, DEFAULT_TOTAL_ITERATIONS];
        let render_params_buffer = device.create_buffer_init(&util::BufferInitDescriptor {
            label: Some("render_params_ssbo"),
            contents: bytemuck::cast_slice(&render_params),
            usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
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
            // 4 u32s per pixel: R, G, B, count (total 16 bytes per pixel)
            size: (output_width * output_height * 16) as u64,
            usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
            mapped_at_creation: true,
        });
        
        // Initialize histogram to zeros
        {
            let mut mapping = histogram_buffer.slice(..).get_mapped_range_mut();
            let zeros = vec![0u8; mapping.len()];
            mapping.copy_from_slice(&zeros);
        }
        histogram_buffer.unmap();
        
        // Create output texture
        let output_texture = device.create_texture(&TextureDescriptor {
            label: Some("output_texture"),
            size: Extent3d {
                width: output_width,
                height: output_height,
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
        
        // Generate and upload 2D palette texture (Legacy ChromaToColor 6-sector mapping)
        let palette = fractal_core::palette::Palette::generate_2d_palette();
        let palette_width = palette.width;
        let palette_height = palette.height;
        
        // Convert palette Vec3 colors to RGBA u32 for GPU texture
        let mut palette_rgba = Vec::with_capacity((palette_width * palette_height) as usize);
        for color in palette.colors {
            let r = (color.x.clamp(0.0, 1.0) * 255.0) as u8;
            let g = (color.y.clamp(0.0, 1.0) * 255.0) as u8;
            let b = (color.z.clamp(0.0, 1.0) * 255.0) as u8;
            let a = 255u8;
            let rgba = ((a as u32) << 24) | ((b as u32) << 16) | ((g as u32) << 8) | (r as u32);
            palette_rgba.push(rgba);
        }
        
        let palette_texture = device.create_texture(&TextureDescriptor {
            label: Some("palette_texture"),
            size: Extent3d {
                width: palette_width,
                height: palette_height,
                depth_or_array_layers: 1,
            },
            mip_level_count: 1,
            sample_count: 1,
            dimension: TextureDimension::D2,
            format: TextureFormat::Rgba8Unorm,
            usage: TextureUsages::TEXTURE_BINDING | TextureUsages::COPY_DST,
            view_formats: &[],
        });
        
        queue.write_texture(
            wgpu::TexelCopyTextureInfo {
                texture: &palette_texture,
                mip_level: 0,
                origin: wgpu::Origin3d::ZERO,
                aspect: wgpu::TextureAspect::All,
            },
            bytemuck::cast_slice(&palette_rgba),
            wgpu::TexelCopyBufferLayout {
                offset: 0,
                bytes_per_row: Some(palette_width * 4),
                rows_per_image: Some(palette_height),
            },
            Extent3d {
                width: palette_width,
                height: palette_height,
                depth_or_array_layers: 1,
            },
        );
        
        let palette_texture_view = palette_texture.create_view(&TextureViewDescriptor::default());
        let palette_sampler = device.create_sampler(&SamplerDescriptor {
            label: Some("palette_sampler"),
            address_mode_u: AddressMode::ClampToEdge,
            address_mode_v: AddressMode::ClampToEdge,
            address_mode_w: AddressMode::ClampToEdge,
            mag_filter: FilterMode::Linear,
            min_filter: FilterMode::Linear,
            mipmap_filter: MipmapFilterMode::Nearest,
            ..Default::default()
        });
        
        // Create iterate bind group
        let iterate_bind_group_layout = device.create_bind_group_layout(&BindGroupLayoutDescriptor {
            label: Some("iterate_bgl"),
            entries: &[
                BindGroupLayoutEntry {
                    binding: 0,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
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
                BindGroupLayoutEntry {
                    binding: 4,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Texture {
                        sample_type: TextureSampleType::Float { filterable: true },
                        view_dimension: TextureViewDimension::D2,
                        multisampled: false,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 5,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Sampler(SamplerBindingType::Filtering),
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 6,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
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
                BindGroupEntry { binding: 4, resource: BindingResource::TextureView(&palette_texture_view) },
                BindGroupEntry { binding: 5, resource: BindingResource::Sampler(&palette_sampler) },
                BindGroupEntry { binding: 6, resource: render_params_buffer.as_entire_binding() },
            ],
        });
        
        let iterate_pipeline = device.create_compute_pipeline(&ComputePipelineDescriptor {
            label: Some("iterate_pipeline"),
            layout: Some(&device.create_pipeline_layout(&PipelineLayoutDescriptor {
                label: Some("iterate_layout"),
                bind_group_layouts: &[Some(&iterate_bind_group_layout)],
                immediate_size: 0,
            })),
            module: &iterate_module,
            entry_point: Some("main"),
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
                        ty: BufferBindingType::Storage { read_only: true },
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
                    ty: BindingType::StorageTexture {
                        access: StorageTextureAccess::WriteOnly,
                        format: TextureFormat::Rgba8Unorm,
                        view_dimension: TextureViewDimension::D2,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 4,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
                        has_dynamic_offset: false,
                        min_binding_size: None,
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
                BindGroupEntry { binding: 2, resource: branches_buffer.as_entire_binding() },
                BindGroupEntry { binding: 3, resource: BindingResource::TextureView(&output_texture_view) },
                BindGroupEntry { binding: 4, resource: render_params_buffer.as_entire_binding() },
            ],
        });
        
        let tonemap_pipeline = device.create_compute_pipeline(&ComputePipelineDescriptor {
            label: Some("tonemap_pipeline"),
            layout: Some(&device.create_pipeline_layout(&PipelineLayoutDescriptor {
                label: Some("tonemap_layout"),
                bind_group_layouts: &[Some(&tonemap_bind_group_layout)],
                immediate_size: 0,
            })),
            module: &tonemap_module,
            entry_point: Some("main"),
            cache: None,
            compilation_options: Default::default(),
        });
        
        Ok(Self {
            iterate_pipeline,
            tonemap_pipeline,
            flame_buffer,
            render_params_buffer,
            branches_buffer,
            variations_buffer,
            histogram_buffer,
            palette_texture,
            palette_texture_view,
            palette_sampler,
            output_texture,
            output_texture_view,
            output_width,
            output_height,
            iterate_bind_group,
            tonemap_bind_group,
        })
    }
    
    pub fn iterate(&self, queue: &Queue, device: &Device, num_threads: u32) {
        // Clear histogram to zeros for this frame (4 u32s per pixel: R, G, B, count)
        queue.write_buffer(
            &self.histogram_buffer,
            0,
            &vec![0u8; (self.output_width * self.output_height * 16) as usize],
        );
        
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
            cpass.dispatch_workgroups((self.output_width + 15) / 16, (self.output_height + 15) / 16, 1);
        }
        
        queue.submit(std::iter::once(encoder.finish()));
    }
    

    
    #[allow(dead_code)]
    pub fn output_texture(&self) -> &Texture {
        &self.output_texture
    }

    pub fn output_texture_view(&self) -> &TextureView {
        &self.output_texture_view
    }
    
    /// Read the output texture back to CPU as RGBA8 data (synchronous with device.poll)
    #[allow(dead_code)]
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
            TexelCopyTextureInfo {
                texture: &self.output_texture,
                mip_level: 0,
                origin: Origin3d::ZERO,
                aspect: TextureAspect::All,
            },
            TexelCopyBufferInfo {
                buffer: &staging_buffer,
                layout: TexelCopyBufferLayout {
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
        let _ = device.poll(wgpu::PollType::wait_indefinitely());
        
        // Now request map - it should succeed immediately since work is done
        let mut mapped_successfully = false;
        for _ in 0..5 {
            staging_buffer.slice(..).map_async(MapMode::Read, |_| {});
            
            // Poll again to process the map request
            let _ = device.poll(wgpu::PollType::wait_indefinitely());
            
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
    
    pub fn needs_resize(&self, target_width: u32, target_height: u32) -> bool {
        let target_width = target_width.clamp(32, 8192);
        let target_height = target_height.clamp(32, 8192);
        self.output_width != target_width || self.output_height != target_height
    }

    pub fn resize(
        &mut self,
        device: &Device,
        queue: &Queue,
        new_width: u32,
        new_height: u32,
    ) -> Result<(), String> {
        let new_width = new_width.clamp(32, 8192);
        let new_height = new_height.clamp(32, 8192);

        if !self.needs_resize(new_width, new_height) {
            return Ok(());
        }

        eprintln!(
            "Resizing renderer output from {}x{} to {}x{}",
            self.output_width, self.output_height, new_width, new_height
        );

        let histogram_buffer = device.create_buffer(&BufferDescriptor {
            label: Some("histogram_ssbo"),
            size: (new_width * new_height * 16) as u64,
            usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
            mapped_at_creation: true,
        });

        {
            let mut mapping = histogram_buffer.slice(..).get_mapped_range_mut();
            let zeros = vec![0u8; mapping.len()];
            mapping.copy_from_slice(&zeros);
        }
        histogram_buffer.unmap();

        let output_texture = device.create_texture(&TextureDescriptor {
            label: Some("output_texture"),
            size: Extent3d {
                width: new_width,
                height: new_height,
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

        let iterate_bind_group_layout = device.create_bind_group_layout(&BindGroupLayoutDescriptor {
            label: Some("iterate_bgl"),
            entries: &[
                BindGroupLayoutEntry {
                    binding: 0,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
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
                BindGroupLayoutEntry {
                    binding: 4,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Texture {
                        sample_type: TextureSampleType::Float { filterable: true },
                        view_dimension: TextureViewDimension::D2,
                        multisampled: false,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 5,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Sampler(SamplerBindingType::Filtering),
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 6,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
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
                BindGroupEntry { binding: 0, resource: self.flame_buffer.as_entire_binding() },
                BindGroupEntry { binding: 1, resource: self.branches_buffer.as_entire_binding() },
                BindGroupEntry { binding: 2, resource: self.variations_buffer.as_entire_binding() },
                BindGroupEntry { binding: 3, resource: histogram_buffer.as_entire_binding() },
                BindGroupEntry { binding: 4, resource: BindingResource::TextureView(&self.palette_texture_view) },
                BindGroupEntry { binding: 5, resource: BindingResource::Sampler(&self.palette_sampler) },
                BindGroupEntry { binding: 6, resource: self.render_params_buffer.as_entire_binding() },
            ],
        });

        let tonemap_bind_group_layout = device.create_bind_group_layout(&BindGroupLayoutDescriptor {
            label: Some("tonemap_bgl"),
            entries: &[
                BindGroupLayoutEntry {
                    binding: 0,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
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
                    ty: BindingType::StorageTexture {
                        access: StorageTextureAccess::WriteOnly,
                        format: TextureFormat::Rgba8Unorm,
                        view_dimension: TextureViewDimension::D2,
                    },
                    count: None,
                },
                BindGroupLayoutEntry {
                    binding: 4,
                    visibility: ShaderStages::COMPUTE,
                    ty: BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
            ],
        });

        let tonemap_bind_group = device.create_bind_group(&BindGroupDescriptor {
            label: Some("tonemap_bg"),
            layout: &tonemap_bind_group_layout,
            entries: &[
                BindGroupEntry { binding: 0, resource: self.flame_buffer.as_entire_binding() },
                BindGroupEntry { binding: 1, resource: histogram_buffer.as_entire_binding() },
                BindGroupEntry { binding: 2, resource: self.branches_buffer.as_entire_binding() },
                BindGroupEntry { binding: 3, resource: BindingResource::TextureView(&output_texture_view) },
                BindGroupEntry { binding: 4, resource: self.render_params_buffer.as_entire_binding() },
            ],
        });

        let resized_render_params = [new_width, new_height, DEFAULT_TOTAL_ITERATIONS];
        queue.write_buffer(&self.render_params_buffer, 0, bytemuck::cast_slice(&resized_render_params));

        self.histogram_buffer = histogram_buffer;
        self.output_texture = output_texture;
        self.output_texture_view = output_texture_view;
        self.output_width = new_width;
        self.output_height = new_height;
        self.iterate_bind_group = iterate_bind_group;
        self.tonemap_bind_group = tonemap_bind_group;

        Ok(())
    }
    
    // ========================================================================
    // CONVERSION HELPERS
    // ========================================================================
    
    /// Pack Flame into flat f32 array (18 elements) for storage buffer transmission.
    /// 
    /// Layout (18 f32 elements):
    /// [0-3]:   camera_transform.row0 (a, b, e, _padding)
    /// [4-7]:   camera_transform.row1 (c, d, f, _padding)
    /// [8]:     brightness
    /// [9]:     gamma
    /// [10]:    vibrancy
    /// [11]:    _params_padding
    /// [12-15]: background (r, g, b, a)
    /// [16]:    branch_count (bitcast as f32)
    /// [17]:    reserved (bitcast as f32)
    fn flame_to_gpu_flat(flame: &Flame) -> Vec<f32> {
        let camera_transform = GpuAffine::from_mat3(flame.camera_transform);
        let branch_count = flame.branches.len() as u32;
        
        vec![
            // camera_transform.row0 (4 f32s)
            camera_transform.row0[0],
            camera_transform.row0[1],
            camera_transform.row0[2],
            camera_transform.row0[3],
            
            // camera_transform.row1 (4 f32s)
            camera_transform.row1[0],
            camera_transform.row1[1],
            camera_transform.row1[2],
            camera_transform.row1[3],
            
            // params (4 f32s)
            flame.brightness,
            flame.gamma,
            flame.vibrancy,
            0.0,  // _params_padding
            
            // background (4 f32s)
            flame.background.x,
            flame.background.y,
            flame.background.z,
            flame.background.w,
            
            // counters (2 f32s with bitcast u32s)
            f32::from_bits(branch_count),
            0.0,
        ]
    }
    
    fn flame_to_gpu_branches(flame: &Flame) -> (Vec<f32>, Vec<GpuVariEntry>) {
        let mut branch_data = Vec::new();  // Flat array of f32
        let mut gpu_variations = Vec::new();
        
        eprintln!("Converting {} branches to GPU format (flat array)", flame.branches.len());
        
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
            
            // Pack branch into flat array: 18 f32 elements per branch
            // [0-2]: pre_affine row0 (a, b, e)
            // [3-5]: pre_affine row1 (c, d, f)
            // [6-8]: post_affine row0 (a, b, e)
            // [9-11]: post_affine row1 (c, d, f)
            // [12-13]: chroma (x, y)
            // [14]: weight
            // [15]: color_weight
            // [16]: var_count (bitcast as f32)
            // [17]: var_offset (bitcast as f32)
            
            branch_data.push(pre_affine_gpu.row0[0]);  // pre_affine.a
            branch_data.push(pre_affine_gpu.row0[1]);  // pre_affine.b
            branch_data.push(pre_affine_gpu.row0[2]);  // pre_affine.e
            
            branch_data.push(pre_affine_gpu.row1[0]);  // pre_affine.c
            branch_data.push(pre_affine_gpu.row1[1]);  // pre_affine.d
            branch_data.push(pre_affine_gpu.row1[2]);  // pre_affine.f
            
            branch_data.push(post_affine_gpu.row0[0]); // post_affine.a
            branch_data.push(post_affine_gpu.row0[1]); // post_affine.b
            branch_data.push(post_affine_gpu.row0[2]); // post_affine.e
            
            branch_data.push(post_affine_gpu.row1[0]); // post_affine.c
            branch_data.push(post_affine_gpu.row1[1]); // post_affine.d
            branch_data.push(post_affine_gpu.row1[2]); // post_affine.f
            
            branch_data.push(branch.chroma.x);
            branch_data.push(branch.chroma.y);
            
            branch_data.push(branch.weight);
            branch_data.push(branch.color_weight);
            
            // Bitcast u32 to f32
            branch_data.push(f32::from_bits(var_count));
            branch_data.push(f32::from_bits(var_offset));
        }
        
        eprintln!("  Created {} GPU branches ({} total f32s), {} total variations", 
                 flame.branches.len(), branch_data.len(), gpu_variations.len());
        
        (branch_data, gpu_variations)
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

    // ====================================================================
    // GPU SHADER TESTS (requires wgpu device)
    // ====================================================================

    /// Test shader that validates read_flame() and read_branch() functions
    /// Each test case writes a vec4 result to the results buffer
    const GPU_TEST_SHADER: &str = r#"
        @group(0) @binding(0) var<storage, read> flame_data: array<f32>;
        @group(0) @binding(1) var<storage, read> branch_data: array<f32>;
        @group(0) @binding(2) var<storage, read_write> results: array<vec4<f32>>;

        @compute @workgroup_size(1)
        fn main() {
            var result_idx = 0u;
            
            // ============================================================
            // TEST SUITE: read_flame() and read_branch() functions
            // ============================================================
            
            // Read flame once to validate packing
            let flame = read_flame();
            
            // Test 1: read_branch(0) - Branch 0 pre_affine row0
            var b0 = read_branch(0u);
            results[result_idx] = vec4<f32>(
                b0.pre_affine.row0.x,    // should be 0.5
                b0.pre_affine.row0.y,    // should be 0.0
                b0.pre_affine.row0.z,    // should be 0.0 (translation)
                1.0
            );
            result_idx += 1u;
            
            // Test 2: read_branch(0) - Branch 0 post_affine row0
            results[result_idx] = vec4<f32>(
                b0.post_affine.row0.x,   // should be 1.0 (identity)
                b0.post_affine.row0.y,   // should be 0.0
                b0.post_affine.row0.z,   // should be 0.0
                1.0
            );
            result_idx += 1u;
            
            // Test 3: read_branch(0) - Branch 0 chroma + weight
            results[result_idx] = vec4<f32>(
                b0.chroma.x,             // should be 0.0 (red channel)
                b0.chroma.y,             // should be 0.0
                b0.weight,               // should be 1.0/3
                b0.color_weight          // should be 0.5
            );
            result_idx += 1u;
            
            // Test 4: read_branch(1) - Branch 1 pre_affine translation
            var b1 = read_branch(1u);
            results[result_idx] = vec4<f32>(
                b1.pre_affine.row0.x,    // should be 0.5 (scale)
                b1.pre_affine.row0.z,    // should be -0.5 (translation x)
                b1.pre_affine.row1.z,    // should be -0.5 (translation y)
                1.0
            );
            result_idx += 1u;
            
            // Test 5: read_branch(2) - Branch 2 pre_affine translation
            var b2 = read_branch(2u);
            results[result_idx] = vec4<f32>(
                b2.pre_affine.row0.x,    // should be 0.5
                b2.pre_affine.row0.z,    // should be 0.5 (translation x)
                b2.pre_affine.row1.z,    // should be -0.5 (translation y)
                1.0
            );
            result_idx += 1u;
            
            // Test 6: apply_affine identity transform
            let identity = Affine(
                vec4<f32>(1.0, 0.0, 0.0, 0.0),
                vec4<f32>(0.0, 1.0, 0.0, 0.0)
            );
            let p1 = apply_affine(vec2<f32>(2.0, 3.0), identity);
            results[result_idx] = vec4<f32>(p1.x, p1.y, 0.0, 1.0);  // should be (2.0, 3.0)
            result_idx += 1u;
            
            // Test 7: apply_affine with translation
            let translate = Affine(
                vec4<f32>(1.0, 0.0, 5.0, 0.0),  // [1, 0, 5]
                vec4<f32>(0.0, 1.0, -2.0, 0.0)  // [0, 1, -2]
            );
            let p2 = apply_affine(vec2<f32>(1.0, 1.0), translate);
            results[result_idx] = vec4<f32>(p2.x, p2.y, 0.0, 1.0);  // should be (6.0, -1.0)
            result_idx += 1u;
            
            // Test 8: apply_affine with scale
            let scale = Affine(
                vec4<f32>(2.0, 0.0, 0.0, 0.0),  // [2, 0, 0]
                vec4<f32>(0.0, 3.0, 0.0, 0.0)   // [0, 3, 0]
            );
            let p3 = apply_affine(vec2<f32>(1.0, 1.0), scale);
            results[result_idx] = vec4<f32>(p3.x, p3.y, 0.0, 1.0);  // should be (2.0, 3.0)
            result_idx += 1u;
            
            // Test 9: Branch 0 as Sierpinski corner (origin)
            let sierpinski_p = vec2<f32>(0.0, 0.0);
            let sierpinski_transformed = apply_affine(sierpinski_p, b0.pre_affine);
            results[result_idx] = vec4<f32>(sierpinski_transformed.x, sierpinski_transformed.y, 0.0, 1.0);
            // should be (0.0, 0.0) since scale(0.5) at origin = origin
            result_idx += 1u;
        }
    "#;

    #[tokio::test]
    async fn test_branch_common_gpu() {
        // Initialize wgpu with headless backend
        let instance = wgpu::Instance::new(wgpu::InstanceDescriptor {
            backends: wgpu::Backends::PRIMARY,
            flags: wgpu::InstanceFlags::empty(),
            memory_budget_thresholds: Default::default(),
            backend_options: Default::default(),
            display: None,
        });

        let adapter = match pollster::block_on(instance.request_adapter(&wgpu::RequestAdapterOptions {
            power_preference: wgpu::PowerPreference::HighPerformance,
            compatible_surface: None,
            force_fallback_adapter: false,
        })) {
            Ok(adapter) => adapter,
            Err(e) => panic!("No suitable GPU adapter found for testing: {}", e),
        };

        let (device, queue) = match pollster::block_on(adapter.request_device(
            &wgpu::DeviceDescriptor {
                label: Some("test_device"),
                required_features: wgpu::Features::empty(),
                required_limits: wgpu::Limits::downlevel_defaults(),
                memory_hints: wgpu::MemoryHints::default(),
                experimental_features: wgpu::ExperimentalFeatures::disabled(),
                trace: wgpu::Trace::default(),
            }
        )) {
            Ok((device, queue)) => (device, queue),
            Err(e) => panic!("Failed to create device: {}", e),
        };

        // Create a real Sierpinski flame and pack it using the actual renderer code
        let flame = Flame::demo();
        let (branch_data, _gpu_variations) = GpuRenderer::flame_to_gpu_branches(&flame);
        
        eprintln!("\n=== TEST: Using real Sierpinski flame + packing ===");
        eprintln!("Flame has {} branches, packed to {} f32 elements", 
                 flame.branches.len(), branch_data.len());

        // Create GPU buffers
        let branch_data_buffer = device.create_buffer_init(&wgpu::util::BufferInitDescriptor {
            label: Some("test_branch_data"),
            contents: bytemuck::cast_slice(&branch_data),
            usage: wgpu::BufferUsages::STORAGE | wgpu::BufferUsages::COPY_SRC,
        });

        // Results buffer: 10 test cases × vec4
        let results_buffer = device.create_buffer(&wgpu::BufferDescriptor {
            label: Some("test_results"),
            size: (10 * 16) as u64, // 10 × vec4<f32> = 10 × 16 bytes
            usage: wgpu::BufferUsages::STORAGE | wgpu::BufferUsages::COPY_SRC,
            mapped_at_creation: true,
        });

        {
            let mut mapping = results_buffer.slice(..).get_mapped_range_mut();
            mapping.copy_from_slice(&vec![0u8; mapping.len()]);
        }
        results_buffer.unmap();

        // Staging buffer to read back results
        let staging_buffer = device.create_buffer(&wgpu::BufferDescriptor {
            label: Some("test_staging"),
            size: (10 * 16) as u64,
            usage: wgpu::BufferUsages::COPY_DST | wgpu::BufferUsages::MAP_READ,
            mapped_at_creation: false,
        });

        // Create shader module with branch_common concatenated
        let branch_common = include_str!("shaders/branch_common.wgsl");
        let full_shader = format!("{}\n{}", branch_common, GPU_TEST_SHADER);
        
        let shader_module = device.create_shader_module(wgpu::ShaderModuleDescriptor {
            label: Some("gpu_test_shader"),
            source: wgpu::ShaderSource::Wgsl(std::borrow::Cow::Owned(full_shader)),
        });

        // Create a flame_data buffer for the test
        let flame_data_for_test = GpuRenderer::flame_to_gpu_flat(&flame);
        let flame_data_buffer = device.create_buffer_init(&wgpu::util::BufferInitDescriptor {
            label: Some("test_flame_data"),
            contents: bytemuck::cast_slice(&flame_data_for_test),
            usage: wgpu::BufferUsages::STORAGE | wgpu::BufferUsages::COPY_SRC,
        });

        // Create bind group layout
        let bind_group_layout = device.create_bind_group_layout(&wgpu::BindGroupLayoutDescriptor {
            label: Some("test_bind_group_layout"),
            entries: &[
                wgpu::BindGroupLayoutEntry {
                    binding: 0,
                    visibility: wgpu::ShaderStages::COMPUTE,
                    ty: wgpu::BindingType::Buffer {
                        ty: wgpu::BufferBindingType::Storage { read_only: true },
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
                wgpu::BindGroupLayoutEntry {
                    binding: 1,
                    visibility: wgpu::ShaderStages::COMPUTE,
                    ty: wgpu::BindingType::Buffer {
                        ty: wgpu::BufferBindingType::Storage { read_only: true },
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
                wgpu::BindGroupLayoutEntry {
                    binding: 2,
                    visibility: wgpu::ShaderStages::COMPUTE,
                    ty: wgpu::BindingType::Buffer {
                        ty: wgpu::BufferBindingType::Storage { read_only: false },
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    count: None,
                },
            ],
        });

        let bind_group = device.create_bind_group(&wgpu::BindGroupDescriptor {
            label: Some("test_bind_group"),
            layout: &bind_group_layout,
            entries: &[
                wgpu::BindGroupEntry {
                    binding: 0,
                    resource: flame_data_buffer.as_entire_binding(),
                },
                wgpu::BindGroupEntry {
                    binding: 1,
                    resource: branch_data_buffer.as_entire_binding(),
                },
                wgpu::BindGroupEntry {
                    binding: 2,
                    resource: results_buffer.as_entire_binding(),
                },
            ],
        });

        let pipeline_layout = device.create_pipeline_layout(&wgpu::PipelineLayoutDescriptor {
            label: Some("test_pipeline_layout"),
            bind_group_layouts: &[Some(&bind_group_layout)],
            immediate_size: 0,
        });

        let compute_pipeline = device.create_compute_pipeline(&wgpu::ComputePipelineDescriptor {
            label: Some("test_compute_pipeline"),
            layout: Some(&pipeline_layout),
            module: &shader_module,
            entry_point: Some("main"),
            cache: None,
            compilation_options: wgpu::PipelineCompilationOptions::default(),
        });

        // Run the compute shader
        let mut encoder = device.create_command_encoder(&wgpu::CommandEncoderDescriptor {
            label: Some("test_encoder"),
        });

        {
            let mut cpass = encoder.begin_compute_pass(&wgpu::ComputePassDescriptor {
                label: Some("test_compute_pass"),
                timestamp_writes: None,
            });
            cpass.set_pipeline(&compute_pipeline);
            cpass.set_bind_group(0, &bind_group, &[]);
            cpass.dispatch_workgroups(1, 1, 1);
        }

        encoder.copy_buffer_to_buffer(&results_buffer, 0, &staging_buffer, 0, (10 * 16) as u64);
        queue.submit(std::iter::once(encoder.finish()));

        // Map staging buffer and read results
        let buffer_slice = staging_buffer.slice(..);
        let (tx, rx) = std::sync::mpsc::channel();
        buffer_slice.map_async(wgpu::MapMode::Read, move |result| {
            tx.send(result).ok();
        });
        let _ = device.poll(wgpu::PollType::wait_indefinitely());

        rx.recv().expect("failed to map buffer").expect("buffer mapping failed");

        let data = buffer_slice.get_mapped_range();
        let results: &[f32] = bytemuck::cast_slice(&data);

        // Verify test results
        eprintln!("\n=== GPU SHADER TEST RESULTS ===");
        
        // Extract expected values from the flame we packed
        let b0 = &flame.branches[0];
        let b1 = &flame.branches[1];
        let b2 = &flame.branches[2];
        
        let b0_pre_gpu = GpuAffine::from_mat3(b0.pre_affine);
        let b1_pre_gpu = GpuAffine::from_mat3(b1.pre_affine);
        let b2_pre_gpu = GpuAffine::from_mat3(b2.pre_affine);
        
        // Test 1: Branch 0 pre_affine row0 (results at indices 0, 1, 2, 3)
        eprintln!("Test 1: Branch 0 pre_affine.row0");
        eprintln!("  GPU read: ({}, {}, {})", results[0], results[1], results[2]);
        eprintln!("  Expected: ({}, {}, {})", b0_pre_gpu.row0[0], b0_pre_gpu.row0[1], b0_pre_gpu.row0[2]);
        assert!((results[0] - b0_pre_gpu.row0[0]).abs() < 0.001, "Branch 0 pre_affine.row0.x mismatch");
        assert!((results[1] - b0_pre_gpu.row0[1]).abs() < 0.001, "Branch 0 pre_affine.row0.y mismatch");
        assert!((results[2] - b0_pre_gpu.row0[2]).abs() < 0.001, "Branch 0 pre_affine.row0.z mismatch");

        // Test 2: Branch 0 post_affine row0 (results at indices 4, 5, 6, 7)
        let b0_post_gpu = GpuAffine::from_mat3(b0.post_affine);
        eprintln!("Test 2: Branch 0 post_affine.row0");
        eprintln!("  GPU read: ({}, {}, {})", results[4], results[5], results[6]);
        eprintln!("  Expected: ({}, {}, {})", b0_post_gpu.row0[0], b0_post_gpu.row0[1], b0_post_gpu.row0[2]);
        assert!((results[4] - b0_post_gpu.row0[0]).abs() < 0.001, "Branch 0 post_affine.row0.x mismatch");
        assert!((results[5] - b0_post_gpu.row0[1]).abs() < 0.001, "Branch 0 post_affine.row0.y mismatch");

        // Test 3: Branch 0 chroma and weight (results at indices 8-11)
        eprintln!("Test 3: Branch 0 chroma and weight");
        eprintln!("  GPU read: chroma=({}, {}), weight={}, color_weight={}", 
                 results[8], results[9], results[10], results[11]);
        eprintln!("  Expected: chroma=({}, {}), weight={}, color_weight={}", 
                 b0.chroma.x, b0.chroma.y, b0.weight, b0.color_weight);
        assert!((results[8] - b0.chroma.x).abs() < 0.001, "Branch 0 chroma.x mismatch");
        assert!((results[9] - b0.chroma.y).abs() < 0.001, "Branch 0 chroma.y mismatch");
        assert!((results[10] - b0.weight).abs() < 0.001, "Branch 0 weight mismatch");
        assert!((results[11] - b0.color_weight).abs() < 0.001, "Branch 0 color_weight mismatch");

        // Test 4: Branch 1 translation (results at indices 12-15)
        eprintln!("Test 4: Branch 1 pre_affine translation");
        eprintln!("  GPU read: ({}, {}, {})", results[12], results[13], results[14]);
        eprintln!("  Expected: ({}, {}, {})", b1_pre_gpu.row0[0], b1_pre_gpu.row0[2], b1_pre_gpu.row1[2]);
        assert!((results[12] - b1_pre_gpu.row0[0]).abs() < 0.001, "Branch 1 pre_affine.row0.x mismatch");
        assert!((results[13] - b1_pre_gpu.row0[2]).abs() < 0.001, "Branch 1 pre_affine.row0.z mismatch");
        assert!((results[14] - b1_pre_gpu.row1[2]).abs() < 0.001, "Branch 1 pre_affine.row1.z mismatch");

        // Test 5: Branch 2 translation (results at indices 16-19)
        eprintln!("Test 5: Branch 2 pre_affine translation");
        eprintln!("  GPU read: ({}, {}, {})", results[16], results[17], results[18]);
        eprintln!("  Expected: ({}, {}, {})", b2_pre_gpu.row0[0], b2_pre_gpu.row0[2], b2_pre_gpu.row1[2]);
        assert!((results[16] - b2_pre_gpu.row0[0]).abs() < 0.001, "Branch 2 pre_affine.row0.x mismatch");
        assert!((results[17] - b2_pre_gpu.row0[2]).abs() < 0.001, "Branch 2 pre_affine.row0.z mismatch");
        assert!((results[18] - b2_pre_gpu.row1[2]).abs() < 0.001, "Branch 2 pre_affine.row1.z mismatch");

        // Test 6: Identity transform (results at indices 20-23)
        eprintln!("Test 6: Identity transform (hardcoded verification)");
        eprintln!("  GPU read: ({}, {})", results[20], results[21]);
        eprintln!("  Expected: (2.0, 3.0)");
        assert!((results[20] - 2.0).abs() < 0.001, "apply_affine identity should preserve x");
        assert!((results[21] - 3.0).abs() < 0.001, "apply_affine identity should preserve y");

        // Test 7: Translation transform (results at indices 24-27)
        eprintln!("Test 7: Translation transform (hardcoded verification)");
        eprintln!("  GPU read: ({}, {})", results[24], results[25]);
        eprintln!("  Expected: (6.0, -1.0)");
        assert!((results[24] - 6.0).abs() < 0.001, "apply_affine translate should give correct x");
        assert!((results[25] - (-1.0)).abs() < 0.001, "apply_affine translate should give correct y");

        // Test 8: Scale transform (results at indices 28-31)
        eprintln!("Test 8: Scale transform (hardcoded verification)");
        eprintln!("  GPU read: ({}, {})", results[28], results[29]);
        eprintln!("  Expected: (2.0, 3.0)");
        assert!((results[28] - 2.0).abs() < 0.001, "apply_affine scale should give correct x");
        assert!((results[29] - 3.0).abs() < 0.001, "apply_affine scale should give correct y");

        eprintln!("\n✓ All GPU tests passed!");
        eprintln!("  Data pipeline: Flame::demo() → GpuRenderer::flame_to_gpu_branches() → GPU shader ✓");
    }
}
