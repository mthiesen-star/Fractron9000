mod gpu;
// Force rebuild marker 7

use gpu::GpuRenderer;
use fractal_core::flame::Flame;
use wgpu::{Device, Queue};
use glam::Mat3;

#[allow(dead_code)]
pub struct FractronApp {
    flame: Flame,
    gpu_renderer: Option<GpuRenderer>,
    device: Option<Device>,
    queue: Option<Queue>,
    iter_count: u32,
    rendered_image: Option<egui::ColorImage>,
    texture_handle: Option<egui::TextureHandle>,
    output_texture_id: Option<egui::TextureId>,
    is_panning: bool,
    pan_start: Option<egui::Pos2>,
    pan_camera_start: Option<Mat3>,
}

impl FractronApp {
    pub fn new(cc: &eframe::CreationContext<'_>) -> Self {
        let flame = Flame::demo();

        log::info!("FractronApp::new: wgpu_render_state available = {}", cc.wgpu_render_state.is_some());
        
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
        
        let (gpu_renderer, device, queue) = {
            if let Some(render_state) = cc.wgpu_render_state.as_ref() {
                let device = render_state.device.clone();
                let queue = render_state.queue.clone();
                match GpuRenderer::new(
                    &device,
                    &queue,
                    &flame,
                ) {
                    Ok(r) => (Some(r), Some(device), Some(queue)),
                    Err(e) => {
                        log::error!("Failed to initialize GPU renderer: {}", e);
                        (None, None, None)
                    }
                }
            } else {
                log::error!("No wgpu render state available");
                (None, None, None)
            }
        };
        
        Self {
            flame,
            gpu_renderer,
            device,
            queue,
            iter_count: 0,
            rendered_image: None,
            texture_handle: None,
            output_texture_id: None,
            is_panning: false,
            pan_start: None,
            pan_camera_start: None,
        }
    }
}

impl eframe::App for FractronApp {
    fn ui(&mut self, ui: &mut egui::Ui, _frame: &mut eframe::Frame) {
        ui.heading("Fractron 9000");



        if let Some(renderer) = &self.gpu_renderer {
            if let (Some(device), Some(queue)) = (&self.device, &self.queue) {
                renderer.iterate(queue, device, 65536);
                renderer.tonemap(queue, device);

                self.iter_count += 1;
                ui.label(format!("Iterations dispatched: {}", self.iter_count));

                #[cfg(target_arch = "wasm32")]
                {
                    if let Some(render_state) = _frame.wgpu_render_state() {
                        let texture_id = if let Some(id) = self.output_texture_id {
                            id
                        } else {
                            let id = render_state.renderer.write().register_native_texture(
                                device,
                                renderer.output_texture_view(),
                                wgpu::FilterMode::Linear,
                            );
                            self.output_texture_id = Some(id);
                            id
                        };

                        let (width, height) = renderer.output_size();
                        ui.image((texture_id, egui::vec2(width as f32, height as f32)));
                        ui.label(format!("Iterations: {}", self.iter_count));
                        return;
                    }
                }

                let output_data = renderer.read_output_to_vec(device, queue);
                let (width, height) = renderer.output_size();

                let pixels = output_data
                    .chunks_exact(4)
                    .map(|pixel| {
                        egui::Color32::from_rgba_unmultiplied(
                            pixel[0],
                            pixel[1],
                            pixel[2],
                            pixel[3],
                        )
                    })
                    .collect();
                let color_image = egui::ColorImage::new([width as usize, height as usize], pixels);

                self.texture_handle = Some(ui.ctx().load_texture(
                    "fractal_output",
                    color_image,
                    Default::default(),
                ));

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

        ui.ctx().request_repaint();
    }

    #[cfg(target_arch = "wasm32")]
    fn as_any_mut(&mut self) -> Option<&mut dyn std::any::Any> {
        Some(self)
    }
}

// WASM entry point — called by the browser via wasm-bindgen.
#[cfg(target_arch = "wasm32")]
use eframe::wasm_bindgen::{self, prelude::*};

#[cfg(target_arch = "wasm32")]
#[wasm_bindgen(start)]
pub async fn start() {
    use eframe::wasm_bindgen::JsCast;

    let _ = eframe::WebLogger::init(log::LevelFilter::Debug);
    log::info!("Starting Fractron9000 web app");

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
