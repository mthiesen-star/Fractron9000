mod gpu;
mod camera_math;
mod app_helpers;
mod app_layout;
mod app_panel;
mod app_viewport;
mod platform_bootstrap;

use gpu::GpuRenderer;
use fractal_core::flame::Flame;
use glam::Vec2;
use camera_math::*;
use app_helpers::load_flame_from_file;


const ZOOM_SCROLL_SENSITIVITY: f32 = 0.0050;
const TRIAD_LINE_STROKE: f32 = 1.0;
const TRIAD_POINT_RADIUS: f32 = 5.0;
const TRIAD_HOVER_RADIUS: f32 = 8.0;
const TRIAD_COLOR: egui::Color32 = egui::Color32::from_rgb(220, 220, 220);
const TRIAD_HOVER_COLOR: egui::Color32 = egui::Color32::from_rgb(255, 255, 255);

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
enum TriadHandle {
    Origin,
    XAxis,
    YAxis,
}

pub struct FractronApp {
    flame: Flame,
    gpu_renderer: Option<GpuRenderer>,
    output_texture_id: Option<egui::TextureId>,
    drag_start_state: Option<Flame>,
    pan_anchor_fractal: Option<Vec2>,
    selected_branch: Option<usize>,
    triad_drag_handle: Option<TriadHandle>,
    triad_drag_handle_offset_ui: Option<egui::Vec2>,
    left_panel_width: f32,
    last_flame: Flame,  // Track complete flame state to detect any parameter changes
}

impl FractronApp {
    pub fn new(
        cc: &eframe::CreationContext<'_>,
        load_flame_file: Option<String>,
        load_flame_name: Option<String>,
    ) -> Self {
        // Try to load flame from file if specified
        let flame = if let (Some(file_path), Some(name)) = (load_flame_file, load_flame_name) {
            match load_flame_from_file(&file_path, &name) {
                Ok(flame) => {
                    log::info!("Successfully loaded flame '{}' from {}", name, file_path);
                    flame
                }
                Err(e) => {
                    log::warn!("Failed to load flame: {}", e);
                    Flame::demo()
                }
            }
        } else {
            Flame::demo()
        };

        log::info!("FractronApp::new: wgpu_render_state available = {}", cc.wgpu_render_state.is_some());
        
        // Debug: log flame structure
        log::debug!("Flame created: name={}, branches={}", flame.name, flame.branches.len());
        for (i, branch) in flame.branches.iter().enumerate() {
            log::debug!("  Branch {}: weight={}, pre_affine translation=({}, {})", 
                i, 
                branch.weight,
                branch.pre_affine.z_axis.x,
                branch.pre_affine.z_axis.y
            );
        }
        
        let gpu_renderer = if let Some(render_state) = cc.wgpu_render_state.as_ref() {
            match GpuRenderer::new(
                render_state.device.clone(),
                render_state.queue.clone(),
                &flame,
                1024,
                768,
            ) {
                Ok(r) => Some(r),
                Err(e) => {
                    log::error!("Failed to initialize GPU renderer: {}", e);
                    None
                }
            }
        } else {
            log::error!("No wgpu render state available");
            None
        };
        
        Self {
            flame: flame.clone(),
            gpu_renderer,
            output_texture_id: None,
            drag_start_state: None,
            pan_anchor_fractal: None,
            selected_branch: None,
            triad_drag_handle: None,
            triad_drag_handle_offset_ui: None,
            left_panel_width: 256.0,
            last_flame: flame.clone(),  // Initialize with current flame state
        }
    }
}

impl eframe::App for FractronApp {
    fn ui(&mut self, ui: &mut egui::Ui, _frame: &mut eframe::Frame) {
        let (menu_rect, content_rect, status_rect) = self.ui_regions(ui.max_rect());
        self.render_menu_bar(ui, menu_rect);
        let status_right = self.render_content_area(ui, content_rect, _frame);
        self.render_status_bar(ui, status_rect, status_right);

        if let Some(renderer) = &mut self.gpu_renderer && renderer.frame_count() < 128 {
            ui.ctx().request_repaint();
        }
    }

    #[cfg(target_arch = "wasm32")]
    fn as_any_mut(&mut self) -> Option<&mut dyn std::any::Any> {
        Some(self)
    }
}

impl FractronApp {
    fn render_content_area(
        &mut self,
        ui: &mut egui::Ui,
        content_rect: egui::Rect,
        _frame: &mut eframe::Frame,
    ) -> &'static str {
        let splitter_width: f32 = 6.0;
        let min_panel_width: f32 = 96.0;
        let min_viewport_width: f32 = 160.0;

        let max_panel_width = (content_rect.width() - splitter_width - min_viewport_width).max(0.0);
        let clamped_width = self.left_panel_width.clamp(min_panel_width.min(max_panel_width), max_panel_width);
        self.left_panel_width = clamped_width;

        let (_, splitter_rect, _) = Self::split_content_rects(content_rect, self.left_panel_width, splitter_width);
        let splitter_id = ui.make_persistent_id("left_panel_splitter");
        let splitter_response = ui.interact(splitter_rect, splitter_id, egui::Sense::drag());
        if splitter_response.dragged() {
            self.left_panel_width = (self.left_panel_width + splitter_response.drag_delta().x)
                .clamp(min_panel_width.min(max_panel_width), max_panel_width);
        }

        let (left_panel_rect, splitter_rect, viewport_rect) =
            Self::split_content_rects(content_rect, self.left_panel_width, splitter_width);

        let pixels_per_point = ui.ctx().pixels_per_point();
        let target_width = (viewport_rect.width() * pixels_per_point).round().max(1.0) as u32;
        let target_height = (viewport_rect.height() * pixels_per_point).round().max(1.0) as u32;

        self.handle_debug_dump_shortcut(ui, viewport_rect, target_width, target_height);

        let mut status_right = "Ready";

        let left_panel_dirty = self.render_left_panel(ui, left_panel_rect, _frame);
        Self::render_splitter(ui, splitter_rect, splitter_response.hovered(), splitter_response.dragged());

        ui.scope_builder(egui::UiBuilder::new().max_rect(viewport_rect), |ui| {
            let mut flame_dirty = left_panel_dirty;

            let viewport_aspect = viewport_rect.width() / viewport_rect.height().max(1e-6);
            if let Some(aspect_camera) = solve_aspect_camera_transform(self.flame.camera_transform, viewport_aspect)
            {
                if aspect_camera != self.flame.camera_transform {
                    self.flame.camera_transform = aspect_camera;
                    flame_dirty = true;
                }
            }

            {
                let Some(renderer) = self.gpu_renderer.as_mut() else {
                    Self::report_renderer_unavailable(ui, &mut status_right);
                    return;
                };
                if renderer.needs_resize(target_width, target_height) {
                    if let Err(e) = renderer.resize(target_width, target_height) {
                        log::error!("Failed to resize renderer output: {}", e);
                        ui.label("Resize failed. See console for details.");
                        status_right = "Resize error";
                        return;
                    }
                    self.output_texture_id = None;
                }
            }

            let Some(renderer) = self.gpu_renderer.as_ref() else {
                Self::report_renderer_unavailable(ui, &mut status_right);
                return;
            };
            let (histogram_width, histogram_height) = renderer.histogram_size();

            let hovered_triad_handle = Self::pick_hovered_triad_handle(
                viewport_rect,
                &self.flame,
                histogram_width,
                histogram_height,
                ui.input(|i| i.pointer.hover_pos()),
            );

            if self.handle_viewport_input(
                ui,
                viewport_rect,
                histogram_width,
                histogram_height,
                hovered_triad_handle,
            ) {
                flame_dirty = true;
            }

            let flame_changed = self.flame != self.last_flame;
            let should_clear_histogram = flame_dirty || flame_changed;
            if flame_changed {
                self.last_flame = self.flame.clone();
            }

            let Some(renderer) = self.gpu_renderer.as_mut() else {
                Self::report_renderer_unavailable(ui, &mut status_right);
                return;
            };
            if flame_dirty {
                renderer.update_flame(&self.flame);
            }
            renderer.advance_frame(should_clear_histogram);
            status_right = Self::present_output_texture(
                ui,
                renderer,
                viewport_rect.size(),
                _frame,
                &mut self.output_texture_id,
            );

            Self::render_affine_triads(
                ui,
                viewport_rect,
                &self.flame,
                histogram_width,
                histogram_height,
                hovered_triad_handle,
                self.selected_branch,
                self.triad_drag_handle,
            );
        });

        status_right
    }

    fn handle_debug_dump_shortcut(
        &self,
        ui: &egui::Ui,
        viewport_rect: egui::Rect,
        target_width: u32,
        target_height: u32,
    ) {
        let dump_state_requested = ui.input(|i| {
            i.modifiers.ctrl && i.modifiers.shift && i.key_pressed(egui::Key::S)
        });
        if dump_state_requested {
            let pointer_pos = ui.input(|i| i.pointer.interact_pos());
            self.dump_debug_state(viewport_rect, target_width, target_height, pointer_pos);
        }
    }

    pub fn selected_branch(&self) -> Option<usize> {
        self.selected_branch
    }

}

// WASM entry point — called by the browser via wasm-bindgen.
#[cfg(target_arch = "wasm32")]
use eframe::wasm_bindgen::{self, prelude::*};

#[cfg(target_arch = "wasm32")]
#[wasm_bindgen(start)]
pub async fn start() {
    platform_bootstrap::start_wasm().await;
}
