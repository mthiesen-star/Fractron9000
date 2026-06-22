mod gpu;
// Force rebuild marker 5

use gpu::GpuRenderer;
use fractal_core::flame::Flame;
use std::sync::Arc;
use wgpu::{Device, Queue};

pub struct FractronApp {
    flame: Flame,
    gpu_renderer: Option<GpuRenderer>,
    device: Option<Arc<Device>>,
    queue: Option<Arc<Queue>>,
    iter_count: u32,
    rendered_image: Option<egui::ColorImage>,
    texture_handle: Option<egui::TextureHandle>,
}

impl FractronApp {
    pub fn new(cc: &eframe::CreationContext<'_>) -> Self {
        let flame = Flame::demo();
        
        // Debug: log flame structure
        eprintln!("Flame created: name={}, branches={}", flame.name, flame.branches.len());
        for (i, branch) in flame.branches.iter().enumerate() {
            eprintln!("  Branch {}: weight={}, pre_affine translation=({}, {})", 
                i, 
                branch.weight,
                branch.pre_affine.z_axis.x,
                branch.pre_affine.z_axis.y
            );
        }
        
        #[cfg(not(target_arch = "wasm32"))]
        let (gpu_renderer, device, queue) = {
            if let Some(render_state) = cc.wgpu_render_state.as_ref() {
                let device = render_state.device.clone();
                let queue = render_state.queue.clone();
                match pollster::block_on(GpuRenderer::new(
                    &device,
                    &queue,
                    &flame,
                )) {
                    Ok(r) => (Some(r), Some(device), Some(queue)),
                    Err(e) => {
                        eprintln!("Failed to initialize GPU renderer: {}", e);
                        (None, None, None)
                    }
                }
            } else {
                eprintln!("No wgpu render state available");
                (None, None, None)
            }
        };
        
        #[cfg(target_arch = "wasm32")]
        let (gpu_renderer, device, queue) = (None, None, None);
        
        Self {
            flame,
            gpu_renderer,
            device,
            queue,
            iter_count: 0,
            rendered_image: None,
            texture_handle: None,
        }
    }
}

impl eframe::App for FractronApp {
    fn update(&mut self, ctx: &egui::Context, _frame: &mut eframe::Frame) {
        egui::CentralPanel::default().show(ctx, |ui| {
            ui.heading("Fractron 9000");
            
            if let Some(renderer) = &self.gpu_renderer {
                if let (Some(device), Some(queue)) = (&self.device, &self.queue) {
                    // Dispatch compute shaders
                    renderer.iterate(queue, device, 65536);
                    renderer.tonemap(queue, device);
                    
                    self.iter_count += 1;
                    ui.label(format!("Iterations dispatched: {}", self.iter_count));
                    
                    // Read output texture to CPU and display
                    let output_data = renderer.read_output_to_vec(device, queue);
                    let (width, height) = renderer.output_size();
                    
                    // Convert byte data to ColorImage
                    let mut color_image = egui::ColorImage::new(
                        [width as usize, height as usize],
                        egui::Color32::BLACK,
                    );
                    
                    for (i, pixel) in output_data.chunks(4).enumerate() {
                        if i < color_image.pixels.len() && pixel.len() >= 4 {
                            color_image.pixels[i] = egui::Color32::from_rgba_unmultiplied(
                                pixel[0],
                                pixel[1],
                                pixel[2],
                                pixel[3],
                            );
                        }
                    }
                    
                    // Register or update texture
                    self.texture_handle = Some(ctx.load_texture(
                        "fractal_output",
                        color_image,
                        Default::default(),
                    ));
                    
                    // Display the texture
                    if let Some(handle) = &self.texture_handle {
                        ui.image(handle);
                    }
                    
                    ui.label(format!("Iterations: {}", self.iter_count));
                } else {
                    ui.label("GPU device/queue not available");
                }
            } else {
                ui.label("GPU renderer not initialized. Check console for errors.");
            }
        });
        
        // Request continuous repaint to keep running iterations
        ctx.request_repaint();
    }
}

// WASM entry point — called by the browser via wasm-bindgen.
#[cfg(target_arch = "wasm32")]
use eframe::wasm_bindgen::{self, prelude::*};

#[cfg(target_arch = "wasm32")]
#[wasm_bindgen(start)]
pub async fn start() {
    use eframe::wasm_bindgen::JsCast;

    let canvas = web_sys::window()
        .expect("no window")
        .document()
        .expect("no document")
        .get_element_by_id("the_canvas_id")
        .expect("no element with id 'the_canvas_id'")
        .dyn_into::<web_sys::HtmlCanvasElement>()
        .expect("the_canvas_id is not a canvas element");

    let web_options = eframe::WebOptions::default();
    eframe::WebRunner::new()
        .start(
            canvas,
            web_options,
            Box::new(|cc| Ok(Box::new(FractronApp::new(cc)))),
        )
        .await
        .expect("Failed to start eframe");
}
