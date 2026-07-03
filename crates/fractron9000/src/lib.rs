mod gpu;
mod camera_math;
mod app_helpers;
mod app_layout;

use gpu::GpuRenderer;
use fractal_core::flame::{Flame, VariEntry};
use fractal_core::variations::Variation;
use glam::{Vec2, Vec4};
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

    fn render_left_panel(&mut self, ui: &mut egui::Ui, left_panel_rect: egui::Rect, frame: &mut eframe::Frame) -> bool {
        let mut panel_dirty = false;
        ui.scope_builder(egui::UiBuilder::new().max_rect(left_panel_rect), |ui| {
            let frame_ui = egui::Frame::new()
                .fill(egui::Color32::from_rgb(18, 20, 25))
                .inner_margin(egui::Margin::symmetric(8, 8))
                .stroke(egui::Stroke::new(1.0, egui::Color32::from_rgb(44, 48, 56)));

            frame_ui.show(ui, |ui| {
                ui.label(egui::RichText::new("Tone Mapping").color(egui::Color32::from_gray(200)));
                ui.add_space(4.0);

                if self.render_tone_map_controls(ui) {
                    panel_dirty = true;
                }

                ui.add_space(12.0);
                ui.separator();
                ui.add_space(8.0);

                ui.label(egui::RichText::new("Palette + Parameters").color(egui::Color32::from_gray(200)));
                ui.add_space(4.0);

                if let Some(branch_index) = self.selected_branch {
                    if self.render_branch_parameter_controls(ui, branch_index) {
                        panel_dirty = true;
                    }

                    ui.add_space(8.0);

                    if self.render_branch_variation_controls(ui, branch_index) {
                        panel_dirty = true;
                    }

                    ui.add_space(8.0);

                    if let Some(branch) = self.flame.branches.get(branch_index) {
                        let chroma = branch.chroma;  // Copy the chroma value
                        if self.render_palette_picker(ui, frame, chroma) {
                            panel_dirty = true;
                        }
                    }
                } else {
                    ui.label(egui::RichText::new("(no branch selected)").color(egui::Color32::from_gray(140)));
                }
            });
        });
        panel_dirty
    }

    fn render_tone_map_controls(&mut self, ui: &mut egui::Ui) -> bool {
        let mut changed = false;

        // Brightness control
        let old_brightness = self.flame.brightness;
        ui.horizontal(|ui| {
            ui.label("Brightness:");
            ui.add(
                egui::DragValue::new(&mut self.flame.brightness)
                    .speed(0.01)
                    .range(0.0..=5.0)
                    .fixed_decimals(2),
            );
        });
        if (self.flame.brightness - old_brightness).abs() > f32::EPSILON {
            changed = true;
        }

        // Gamma control
        let old_gamma = self.flame.gamma;
        ui.horizontal(|ui| {
            ui.label("Gamma:");
            ui.add(
                egui::DragValue::new(&mut self.flame.gamma)
                    .speed(0.01)
                    .range(0.1..=10.0)
                    .fixed_decimals(2),
            );
        });
        if (self.flame.gamma - old_gamma).abs() > f32::EPSILON {
            changed = true;
        }

        // Vibrancy control
        let old_vibrancy = self.flame.vibrancy;
        ui.horizontal(|ui| {
            ui.label("Vibrancy:");
            ui.add(
                egui::DragValue::new(&mut self.flame.vibrancy)
                    .speed(0.01)
                    .range(0.0..=1.0)
                    .fixed_decimals(2),
            );
        });
        if (self.flame.vibrancy - old_vibrancy).abs() > f32::EPSILON {
            changed = true;
        }

        // Background color control
        let old_background = self.flame.background;
        ui.horizontal(|ui| {
            ui.label("Background:");
            let mut background = [
                self.flame.background.x,
                self.flame.background.y,
                self.flame.background.z,
                self.flame.background.w,
            ];
            if ui
                .color_edit_button_rgba_unmultiplied(&mut background)
                .changed()
            {
                self.flame.background = Vec4::new(
                    background[0],
                    background[1],
                    background[2],
                    background[3],
                );
            }
        });
        if self.flame.background != old_background {
            changed = true;
        }

        changed
    }

    fn render_branch_parameter_controls(&mut self, ui: &mut egui::Ui, branch_index: usize) -> bool {
        let Some(branch) = self.flame.branches.get_mut(branch_index) else {
            return false;
        };

        let mut changed = false;

        ui.horizontal(|ui| {
            ui.label("Weight:");
            if ui
                .add(
                    egui::DragValue::new(&mut branch.weight)
                        .speed(0.01)
                        .range(0.0..=10.0)
                        .fixed_decimals(2),
                )
                .changed()
            {
                changed = true;
            }
        });

        ui.horizontal(|ui| {
            ui.label("Color Weight:");
            if ui
                .add(
                    egui::DragValue::new(&mut branch.color_weight)
                        .speed(0.01)
                        .range(0.0..=1.0)
                        .fixed_decimals(2),
                )
                .changed()
            {
                changed = true;
            }
        });

        changed
    }

    fn render_branch_variation_controls(&mut self, ui: &mut egui::Ui, branch_index: usize) -> bool {
        const MAX_VISIBLE_VARIATIONS: usize = 4;

        let Some(branch) = self.flame.branches.get_mut(branch_index) else {
            return false;
        };

        let mut changed = false;

        ui.label(egui::RichText::new("Variations").color(egui::Color32::from_gray(180)));
        ui.add_space(4.0);

        for slot in 0..MAX_VISIBLE_VARIATIONS {
            let current = branch
                .variations
                .get(slot)
                .copied()
                .unwrap_or(VariEntry::new(Variation::Linear, 0.0));

            ui.horizontal(|ui| {
                let mut selected_variation = current.variation;
                egui::ComboBox::from_id_salt(("branch-variation", branch_index, slot))
                    .width(130.0)
                    .selected_text(selected_variation.name())
                    .show_ui(ui, |ui| {
                        for variation in Variation::all() {
                            ui.selectable_value(&mut selected_variation, *variation, variation.name());
                        }
                    });

                let mut weight = current.weight;
                let weight_changed = ui
                    .add(
                        egui::DragValue::new(&mut weight)
                            .speed(0.01)
                            .range(0.0..=1.0)
                            .fixed_decimals(2),
                    )
                    .changed();

                if selected_variation != current.variation || weight_changed {
                    if branch.variations.len() <= slot {
                        branch
                            .variations
                            .resize_with(slot + 1, || VariEntry::new(Variation::Linear, 0.0));
                    }

                    branch.variations[slot] = VariEntry::new(selected_variation, weight.clamp(0.0, 1.0));
                    changed = true;
                }
            });
        }

        changed
    }


    fn render_palette_picker(&mut self, ui: &mut egui::Ui, frame: &mut eframe::Frame, chroma: Vec2) -> bool {
        const PALETTE_SIZE: f32 = 200.0;  // Square palette widget size in UI points
        const CROSSHAIR_SIZE: f32 = 8.0;
        const CROSSHAIR_THICKNESS: f32 = 1.0;

        // Allocate a clickable rect for the palette widget
        let (palette_rect, palette_response) = ui.allocate_exact_size(
            egui::Vec2::splat(PALETTE_SIZE),
            egui::Sense::click(),
        );

        // Register palette texture if not already registered
        let palette_texture_id = if let Some(render_state) = frame.wgpu_render_state() {
            if let Some(renderer) = self.gpu_renderer.as_ref() {
                let _ = renderer.palette_size();  // Ensure palette is available
                let texture_id = render_state.renderer.write().register_native_texture(
                    renderer.device(),
                    renderer.palette_texture_view(),
                    wgpu::FilterMode::Linear,
                );
                Some(texture_id)
            } else {
                None
            }
        } else {
            None
        };

        if let Some(texture_id) = palette_texture_id {
            // Draw the palette texture in the allocated rect
            let painter = ui.painter_at(palette_rect);
            painter.image(
                texture_id,
                palette_rect,
                egui::Rect::from_min_max(egui::pos2(0.0, 0.0), egui::pos2(1.0, 1.0)),
                egui::Color32::WHITE,
            );

            // Handle click to update chroma
            let mut palette_changed = false;
            if palette_response.clicked() {
                if let Some(click_pos) = palette_response.interact_pointer_pos() {
                    // Map click position within palette rect to chroma [0, 1]
                    let click_rel = click_pos - palette_rect.min;
                    let u = (click_rel.x / palette_rect.width()).clamp(0.0, 1.0);
                    let v = (click_rel.y / palette_rect.height()).clamp(0.0, 1.0);
                    
                    if let Some(branch_mut) = self.flame.branches.get_mut(self.selected_branch.unwrap()) {
                        if (branch_mut.chroma.x - u).abs() > f32::EPSILON || (branch_mut.chroma.y - v).abs() > f32::EPSILON {
                            branch_mut.chroma = Vec2::new(u, v);
                            palette_changed = true;
                        }
                    }
                }
            }

            // Draw crosshair at current chroma position
            let chroma_ui = palette_rect.min + egui::Vec2::new(
                chroma.x * palette_rect.width(),
                chroma.y * palette_rect.height(),
            );

            let crosshair_color = egui::Color32::WHITE;
            let h_offset = CROSSHAIR_SIZE / 2.0;

            // Horizontal line
            painter.line_segment(
                [
                    chroma_ui - egui::Vec2::new(h_offset, 0.0),
                    chroma_ui + egui::Vec2::new(h_offset, 0.0),
                ],
                egui::Stroke::new(CROSSHAIR_THICKNESS, crosshair_color),
            );

            // Vertical line
            painter.line_segment(
                [
                    chroma_ui - egui::Vec2::new(0.0, h_offset),
                    chroma_ui + egui::Vec2::new(0.0, h_offset),
                ],
                egui::Stroke::new(CROSSHAIR_THICKNESS, crosshair_color),
            );

            ui.add_space(8.0);
            ui.label(format!("Chroma: ({:.2}, {:.2})", chroma.x, chroma.y));
            palette_changed
        } else {
            ui.label(egui::RichText::new("(palette unavailable)").color(egui::Color32::from_gray(140)));
            false
        }
    }


    fn handle_viewport_input(
        &mut self,
        ui: &egui::Ui,
        viewport_rect: egui::Rect,
        histogram_width: u32,
        histogram_height: u32,
        hovered_triad_handle: Option<(usize, TriadHandle)>,
    ) -> bool {
        let mut flame_dirty = false;

        ui.input(|i| {
            self.process_pointer_button_transitions(
                i,
                viewport_rect,
                histogram_width,
                histogram_height,
                hovered_triad_handle,
            );

            flame_dirty |= self.process_zoom_and_pan(i, viewport_rect, histogram_width, histogram_height);
            flame_dirty |=
                self.process_triad_drag_update(i, viewport_rect, histogram_width, histogram_height);
        });

        flame_dirty
    }

    fn process_pointer_button_transitions(
        &mut self,
        i: &egui::InputState,
        viewport_rect: egui::Rect,
        histogram_width: u32,
        histogram_height: u32,
        hovered_triad_handle: Option<(usize, TriadHandle)>,
    ) {
        if i.pointer.button_released(egui::PointerButton::Primary) {
            self.triad_drag_handle = None;
            self.triad_drag_handle_offset_ui = None;
            if self.pan_anchor_fractal.is_none() {
                self.drag_start_state = None;
            }
        }

        if i.pointer.button_pressed(egui::PointerButton::Primary)
            && let Some((branch_index, handle)) = hovered_triad_handle
        {
            self.selected_branch = Some(branch_index);
            self.triad_drag_handle = Some(handle);
            self.drag_start_state = Some(self.flame.clone());

            // Record the offset from mouse to handle center so dragging doesn't snap.
            if let Some(mouse_pos) = i.pointer.interact_pos()
                && let Some(branch) = self.flame.branches.get(branch_index)
            {
                let pre = branch.pre_affine;
                let handle_fractal = match handle {
                    TriadHandle::Origin => pre.transform_point2(Vec2::ZERO),
                    TriadHandle::XAxis  => pre.transform_point2(Vec2::X),
                    TriadHandle::YAxis  => pre.transform_point2(Vec2::Y),
                };
                self.triad_drag_handle_offset_ui = fractal_to_ui_space(
                    viewport_rect,
                    handle_fractal,
                    self.flame.camera_transform,
                    histogram_width,
                    histogram_height,
                ).map(|handle_ui| handle_ui - mouse_pos);
            }
        }

        if i.pointer.button_pressed(egui::PointerButton::Middle)
            && let Some(pos) = i.pointer.interact_pos()
            && viewport_rect.contains(pos)
        {
            self.drag_start_state = Some(self.flame.clone());
            self.pan_anchor_fractal = ui_to_fractal_space(
                viewport_rect,
                pos,
                self.flame.camera_transform,
                histogram_width,
                histogram_height,
            );
        }

        if i.pointer.button_released(egui::PointerButton::Middle) {
            self.pan_anchor_fractal = None;
            if self.triad_drag_handle.is_none() {
                self.drag_start_state = None;
            }
        }
    }

    fn process_zoom_and_pan(
        &mut self,
        i: &egui::InputState,
        viewport_rect: egui::Rect,
        histogram_width: u32,
        histogram_height: u32,
    ) -> bool {
        let mut flame_dirty = false;

        let scroll_y = i.smooth_scroll_delta.y;
        if scroll_y.abs() > f32::EPSILON
            && let Some(cursor_pos) = i.pointer.hover_pos()
            && viewport_rect.contains(cursor_pos)
        {
            let zoom_factor = (scroll_y * ZOOM_SCROLL_SENSITIVITY).exp();
            if let (Some(anchor_fractal), Some(target_screen)) = (
                ui_to_fractal_space(
                    viewport_rect,
                    cursor_pos,
                    self.flame.camera_transform,
                    histogram_width,
                    histogram_height,
                ),
                ui_to_screen_space(viewport_rect, cursor_pos),
            ) {
                if let Some(next_camera) = solve_zoom_camera_transform(
                    self.flame.camera_transform,
                    anchor_fractal,
                    target_screen,
                    zoom_factor,
                ) {
                    self.flame.camera_transform = next_camera;
                    flame_dirty = true;
                }
            }
        }

        if let Some(next_camera) = solve_pan_camera_transform(
            self.drag_start_state.as_ref().map(|s| s.camera_transform),
            self.pan_anchor_fractal,
            i.pointer.interact_pos(),
            viewport_rect,
        ) {
            self.flame.camera_transform = next_camera;
            flame_dirty = true;
        }

        flame_dirty
    }

    fn process_triad_drag_update(
        &mut self,
        i: &egui::InputState,
        viewport_rect: egui::Rect,
        histogram_width: u32,
        histogram_height: u32,
    ) -> bool {
        let mut flame_dirty = false;

        if let (Some(branch_index), Some(handle), Some(pos)) = (
            self.selected_branch,
            self.triad_drag_handle,
            i.pointer.interact_pos(),
        )
            && let Some(pre_affine_start) = self
                .drag_start_state
                .as_ref()
                .and_then(|s| s.branches.get(branch_index))
                .map(|b| b.pre_affine)
            && let Some(pointer_fractal) = ui_to_fractal_space(
                viewport_rect,
                pos + self.triad_drag_handle_offset_ui.unwrap_or(egui::Vec2::ZERO),
                self.flame.camera_transform,
                histogram_width,
                histogram_height,
            )
            && let Some(branch) = self.flame.branches.get_mut(branch_index)
        {
            let rotation_only = i.modifiers.shift;
            let fixed_angle_no_skew = i.modifiers.alt;
            let next_pre_affine = match handle {
                TriadHandle::Origin => {
                    solve_pre_affine_origin_translation(pre_affine_start, pointer_fractal)
                }
                TriadHandle::XAxis => {
                    if rotation_only {
                        solve_pre_affine_x_axis_rotate_only(
                            pre_affine_start,
                            pointer_fractal,
                        )
                    } else if fixed_angle_no_skew {
                        solve_pre_affine_x_axis_endpoint(pre_affine_start, pointer_fractal)
                    } else {
                        solve_pre_affine_x_axis_rotate_scale_only(
                            pre_affine_start,
                            pointer_fractal,
                        )
                    }
                }
                TriadHandle::YAxis => {
                    if rotation_only {
                        solve_pre_affine_y_axis_rotate_only(
                            pre_affine_start,
                            pointer_fractal,
                        )
                    } else if fixed_angle_no_skew {
                        solve_pre_affine_y_axis_endpoint(pre_affine_start, pointer_fractal)
                    } else {
                        solve_pre_affine_y_axis_rotate_scale_only(
                            pre_affine_start,
                            pointer_fractal,
                        )
                    }
                }
            };
            if branch.pre_affine != next_pre_affine {
                branch.pre_affine = next_pre_affine;
                flame_dirty = true;
            }
        }

        flame_dirty
    }

    fn dump_debug_state(
        &self,
        viewport_rect: egui::Rect,
        target_width: u32,
        target_height: u32,
        pointer_pos: Option<egui::Pos2>,
    ) {
        let camera = self.flame.camera_transform;
        let viewport_aspect = viewport_rect.width() / viewport_rect.height().max(1e-6);
        let camera_x_scale = Vec2::new(camera.x_axis.x, camera.x_axis.y).length();
        let camera_y_scale = Vec2::new(camera.y_axis.x, camera.y_axis.y).length();
        let pointer = pointer_pos
            .map(|p| format!("{:.1},{:.1}", p.x, p.y))
            .unwrap_or_else(|| "none".to_string());
        let pan_camera_start = self
            .drag_start_state
            .as_ref()
            .map(|s| s.camera_transform)
            .map(|m| format!(
                "[{:.4},{:.4},{:.4};{:.4},{:.4},{:.4}]",
                m.x_axis.x, m.y_axis.x, m.z_axis.x, m.x_axis.y, m.y_axis.y, m.z_axis.y
            ))
            .unwrap_or_else(|| "none".to_string());
        let pan_anchor_fractal = self
            .pan_anchor_fractal
            .map(|p| format!("{:.4},{:.4}", p.x, p.y))
            .unwrap_or_else(|| "none".to_string());

        let frame_count = self.gpu_renderer.as_ref().map(|r| r.frame_count()).unwrap_or(0);
        log::info!(
            "STATE_DUMP frame_count={} pointer={} pan_camera_start={} pan_anchor_fractal={} camera=[{:.4},{:.4},{:.4};{:.4},{:.4},{:.4}] camera_scale=[{:.6},{:.6}] viewport=[{:.1},{:.1},{:.1},{:.1}] viewport_aspect={:.6} target={}x{}",
            frame_count,
            pointer,
            pan_camera_start,
            pan_anchor_fractal,
            camera.x_axis.x,
            camera.y_axis.x,
            camera.z_axis.x,
            camera.x_axis.y,
            camera.y_axis.y,
            camera.z_axis.y,
            camera_x_scale,
            camera_y_scale,
            viewport_rect.left(),
            viewport_rect.top(),
            viewport_rect.width(),
            viewport_rect.height(),
            viewport_aspect,
            target_width,
            target_height,
        );
    }

    fn present_output_texture(
        ui: &mut egui::Ui,
        renderer: &GpuRenderer,
        viewport_size: egui::Vec2,
        frame: &mut eframe::Frame,
        output_texture_id: &mut Option<egui::TextureId>,
    ) -> &'static str {
        if let Some(render_state) = frame.wgpu_render_state() {
            let texture_id = if let Some(id) = *output_texture_id {
                id
            } else {
                let id = render_state.renderer.write().register_native_texture(
                    renderer.device(),
                    renderer.output_texture_view(),
                    wgpu::FilterMode::Linear,
                );
                *output_texture_id = Some(id);
                id
            };

            // Display orientation is handled in UI by flipping V coordinates.
            let uv = egui::Rect::from_min_max(egui::pos2(0.0, 1.0), egui::pos2(1.0, 0.0));
            ui.add(egui::Image::new((texture_id, viewport_size)).uv(uv));
            "Rendering"
        } else {
            ui.label("Render state unavailable");
            "Render state unavailable"
        }
    }

    fn pick_hovered_triad_handle(
        viewport_rect: egui::Rect,
        flame: &Flame,
        histogram_width: u32,
        histogram_height: u32,
        hover_pos: Option<egui::Pos2>,
    ) -> Option<(usize, TriadHandle)> {
        let hover_pos = hover_pos?;
        let mut best: Option<(usize, TriadHandle, f32)> = None;

        for (branch_index, branch) in flame.branches.iter().enumerate() {
            let pre = branch.pre_affine;
            let origin = pre.transform_point2(Vec2::ZERO);
            let x_point = pre.transform_point2(Vec2::X);
            let y_point = pre.transform_point2(Vec2::Y);

            let (Some(origin_ui), Some(x_ui), Some(y_ui)) = (
                fractal_to_ui_space(
                    viewport_rect,
                    origin,
                    flame.camera_transform,
                    histogram_width,
                    histogram_height,
                ),
                fractal_to_ui_space(
                    viewport_rect,
                    x_point,
                    flame.camera_transform,
                    histogram_width,
                    histogram_height,
                ),
                fractal_to_ui_space(
                    viewport_rect,
                    y_point,
                    flame.camera_transform,
                    histogram_width,
                    histogram_height,
                ),
            ) else {
                continue;
            };

            let candidates = [
                (TriadHandle::Origin, origin_ui),
                (TriadHandle::XAxis, x_ui),
                (TriadHandle::YAxis, y_ui),
            ];

            for (handle, ui_pos) in candidates {
                let distance = hover_pos.distance(ui_pos);
                if distance <= TRIAD_HOVER_RADIUS {
                    match best {
                        Some((_, _, best_distance)) if distance >= best_distance => {}
                        _ => best = Some((branch_index, handle, distance)),
                    }
                }
            }
        }

        best.map(|(branch_index, handle, _)| (branch_index, handle))
    }

    fn render_affine_triads(
        ui: &mut egui::Ui,
        viewport_rect: egui::Rect,
        flame: &Flame,
        histogram_width: u32,
        histogram_height: u32,
        hovered_triad_handle: Option<(usize, TriadHandle)>,
        selected_branch: Option<usize>,
        drag_handle: Option<TriadHandle>,
    ) {
        let painter = ui.painter_at(viewport_rect);
        let pointer_down = ui.input(|i| i.pointer.primary_down());

        for (branch_index, branch) in flame.branches.iter().enumerate() {
            let pre = branch.pre_affine;
            let origin = pre.transform_point2(Vec2::ZERO);
            let x_point = pre.transform_point2(Vec2::X);
            let y_point = pre.transform_point2(Vec2::Y);

            let (Some(o_ui), Some(x_ui), Some(y_ui)) = (
                fractal_to_ui_space(
                    viewport_rect,
                    origin,
                    flame.camera_transform,
                    histogram_width,
                    histogram_height,
                ),
                fractal_to_ui_space(
                    viewport_rect,
                    x_point,
                    flame.camera_transform,
                    histogram_width,
                    histogram_height,
                ),
                fractal_to_ui_space(
                    viewport_rect,
                    y_point,
                    flame.camera_transform,
                    histogram_width,
                    histogram_height,
                ),
            ) else {
                continue;
            };

            let is_selected = selected_branch == Some(branch_index);
            let is_origin_hovered = hovered_triad_handle == Some((branch_index, TriadHandle::Origin));
            let is_x_hovered = hovered_triad_handle == Some((branch_index, TriadHandle::XAxis));
            let is_y_hovered = hovered_triad_handle == Some((branch_index, TriadHandle::YAxis));

            let is_origin_dragged = is_selected
                && drag_handle == Some(TriadHandle::Origin)
                && pointer_down;
            let is_x_dragged = is_selected
                && drag_handle == Some(TriadHandle::XAxis)
                && pointer_down;
            let is_y_dragged = is_selected
                && drag_handle == Some(TriadHandle::YAxis)
                && pointer_down;


            let origin_color = if is_origin_hovered || is_origin_dragged { TRIAD_HOVER_COLOR } else { TRIAD_COLOR };
            let x_color = if is_x_hovered || is_x_dragged { TRIAD_HOVER_COLOR } else { TRIAD_COLOR };
            let y_color = if is_y_hovered || is_y_dragged { TRIAD_HOVER_COLOR } else { TRIAD_COLOR };

            let origin_radius = if is_origin_hovered || is_origin_dragged {
                TRIAD_POINT_RADIUS * 1.4
            } else {
                TRIAD_POINT_RADIUS
            };
            let x_radius = if is_x_hovered || is_x_dragged { TRIAD_POINT_RADIUS * 1.4 } else { TRIAD_POINT_RADIUS };
            let y_radius = if is_y_hovered || is_y_dragged { TRIAD_POINT_RADIUS * 1.4 } else { TRIAD_POINT_RADIUS };
            let hover_stroke = egui::Stroke::new(1.0, egui::Color32::from_rgb(30, 30, 30));

            let line_thickness = if is_selected { TRIAD_LINE_STROKE * 2.0 } else { TRIAD_LINE_STROKE };
            painter.line_segment([o_ui, x_ui], egui::Stroke::new(line_thickness, TRIAD_COLOR));
            painter.line_segment([o_ui, y_ui], egui::Stroke::new(line_thickness, TRIAD_COLOR));

            painter.circle_filled(o_ui, origin_radius, origin_color);
            painter.circle_filled(x_ui, x_radius, x_color);
            painter.circle_filled(y_ui, y_radius, y_color);

            if is_origin_hovered || is_origin_dragged {
                painter.circle_stroke(o_ui, origin_radius, hover_stroke);
            }
            if is_x_hovered || is_x_dragged {
                painter.circle_stroke(x_ui, x_radius, hover_stroke);
            }
            if is_y_hovered || is_y_dragged {
                painter.circle_stroke(y_ui, y_radius, hover_stroke);
            }
        }

        if selected_branch.is_some() && pointer_down {
            ui.output_mut(|o| o.cursor_icon = egui::CursorIcon::Grabbing);
        } else if hovered_triad_handle.is_some() {
            ui.output_mut(|o| o.cursor_icon = egui::CursorIcon::Grab);
        }
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
            Box::new(|cc| Ok(Box::new(FractronApp::new(cc, None, None)))),
        )
        .await
        .expect("Failed to start eframe");
}
