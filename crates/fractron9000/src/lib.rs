mod gpu;
mod camera_math;
// Force rebuild marker 7

use gpu::GpuRenderer;
use fractal_core::flame::Flame;
use fractal_core::io::parse_flame_file;
use glam::{Mat3, Vec2};
use camera_math::*;

/// Load a named flame from a .flame file.
fn load_flame_from_file(file_path: &str, flame_name: &str) -> Result<Flame, String> {
    // Read the file
    let contents = std::fs::read_to_string(file_path)
        .map_err(|e| format!("Failed to read file '{}': {}", file_path, e))?;
    
    // Parse all flames from the file
    let flames = parse_flame_file(&contents)
        .map_err(|e| format!("Failed to parse flame file: {}", e))?;
    
    // Collect available names for error reporting
    let available_names: Vec<String> = flames.iter().map(|(n, _)| n.clone()).collect();
    
    // Find the requested flame by name
    for (name, flame) in flames {
        if name == flame_name {
            return Ok(flame);
        }
    }
    
    // If not found, list available flames for debugging
    Err(format!(
        "Flame '{}' not found in file. Available: {}",
        flame_name,
        available_names.join(", ")
    ))
}


const ZOOM_SCROLL_SENSITIVITY: f32 = 0.0050;
const TRIAD_LINE_STROKE: f32 = 1.0;
const TRIAD_POINT_RADIUS: f32 = 5.0;
const TRIAD_HOVER_RADIUS: f32 = 8.0;
const TRIAD_COLOR: egui::Color32 = egui::Color32::from_rgb(220, 220, 220);
const TRIAD_HOVER_COLOR: egui::Color32 = egui::Color32::from_rgb(255, 255, 255);

pub struct FractronApp {
    flame: Flame,
    gpu_renderer: Option<GpuRenderer>,
    output_texture_id: Option<egui::TextureId>,
    pan_camera_start: Option<Mat3>,
    pan_anchor_fractal: Option<Vec2>,
    origin_drag_branch: Option<usize>,
    origin_drag_offset: Option<Vec2>,
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
                    println!("Successfully loaded flame '{}' from {}", name, file_path);
                    flame
                }
                Err(e) => {
                    eprintln!("Failed to load flame: {}", e);
                    Flame::demo()
                }
            }
        } else {
            Flame::demo()
        };

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
            pan_camera_start: None,
            pan_anchor_fractal: None,
            origin_drag_branch: None,
            origin_drag_offset: None,
            left_panel_width: 128.0,
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

        ui.ctx().request_repaint();
    }

    #[cfg(target_arch = "wasm32")]
    fn as_any_mut(&mut self) -> Option<&mut dyn std::any::Any> {
        Some(self)
    }
}

impl FractronApp {
    fn ui_regions(&self, full_rect: egui::Rect) -> (egui::Rect, egui::Rect, egui::Rect) {
        let menu_height = 26.0;
        let menu_gap = 2.0;
        let status_height = 28.0;
        let status_gap = 4.0;

        let menu_bottom = (full_rect.top() + menu_height).min(full_rect.bottom());
        let status_top = (full_rect.bottom() - status_height).max(menu_bottom);
        let content_top = (menu_bottom + menu_gap).min(status_top);
        let content_bottom = (status_top - status_gap).max(content_top);

        let menu_rect = egui::Rect::from_min_max(
            full_rect.min,
            egui::pos2(full_rect.right(), menu_bottom),
        );

        let content_rect = egui::Rect::from_min_max(
            egui::pos2(full_rect.left(), content_top),
            egui::pos2(full_rect.right(), content_bottom),
        );
        let status_rect = egui::Rect::from_min_max(
            egui::pos2(full_rect.left(), status_top),
            full_rect.right_bottom(),
        );

        (menu_rect, content_rect, status_rect)
    }

    fn render_menu_bar(&self, ui: &mut egui::Ui, menu_rect: egui::Rect) {
        ui.scope_builder(egui::UiBuilder::new().max_rect(menu_rect), |ui| {
            let frame = egui::Frame::new()
                .fill(egui::Color32::from_rgb(14, 16, 20))
                .inner_margin(egui::Margin::symmetric(6, 2))
                .stroke(egui::Stroke::new(1.0, egui::Color32::from_rgb(38, 42, 48)));

            frame.show(ui, |ui| {
                egui::MenuBar::new().ui(ui, |ui| {
                    ui.menu_button("File", |_ui| {});
                    ui.menu_button("Edit", |_ui| {});
                });
            });
        });
    }

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

        let dump_state_requested = ui.input(|i| {
            i.modifiers.ctrl && i.modifiers.shift && i.key_pressed(egui::Key::S)
        });
        if dump_state_requested {
            let pointer_pos = ui.input(|i| i.pointer.interact_pos());
            self.dump_debug_state(viewport_rect, target_width, target_height, pointer_pos);
        }

        let mut status_right = "Ready";

        ui.scope_builder(egui::UiBuilder::new().max_rect(left_panel_rect), |ui| {
            let frame = egui::Frame::new()
                .fill(egui::Color32::from_rgb(18, 20, 25))
                .inner_margin(egui::Margin::symmetric(8, 8))
                .stroke(egui::Stroke::new(1.0, egui::Color32::from_rgb(44, 48, 56)));

            frame.show(ui, |ui| {
                ui.label(egui::RichText::new("Palette + Parameters").color(egui::Color32::from_gray(200)));
                ui.add_space(4.0);
                ui.label(egui::RichText::new("(placeholder)").color(egui::Color32::from_gray(140)));
            });
        });

        ui.scope_builder(egui::UiBuilder::new().max_rect(splitter_rect), |ui| {
            let stroke_color = if splitter_response.dragged() || splitter_response.hovered() {
                egui::Color32::from_rgb(110, 120, 140)
            } else {
                egui::Color32::from_rgb(58, 62, 72)
            };
            let center_x = splitter_rect.center().x;
            ui.painter().line_segment(
                [
                    egui::pos2(center_x, splitter_rect.top()),
                    egui::pos2(center_x, splitter_rect.bottom()),
                ],
                egui::Stroke::new(2.0, stroke_color),
            );
            if splitter_response.hovered() || splitter_response.dragged() {
                ui.output_mut(|o| o.cursor_icon = egui::CursorIcon::ResizeHorizontal);
            }
        });

        ui.scope_builder(egui::UiBuilder::new().max_rect(viewport_rect), |ui| {
            let mut flame_dirty = false;

            let viewport_aspect = viewport_rect.width() / viewport_rect.height().max(1e-6);
            if let Some(aspect_camera) = solve_aspect_camera_transform(self.flame.camera_transform, viewport_aspect)
            {
                if aspect_camera != self.flame.camera_transform {
                    self.flame.camera_transform = aspect_camera;
                    flame_dirty = true;
                }
            }

            if let Some(renderer) = &mut self.gpu_renderer {
                if renderer.needs_resize(target_width, target_height) {
                    if let Err(e) = renderer.resize(target_width, target_height) {
                        eprintln!("Failed to resize renderer output: {}", e);
                        ui.label("Resize failed. See console for details.");
                        status_right = "Resize error";
                        return;
                    }
                    self.output_texture_id = None;
                }

                let (histogram_width, histogram_height) = renderer.histogram_size();
                let origin_hovered_branch = Self::pick_hovered_origin_branch(
                    viewport_rect,
                    &self.flame,
                    histogram_width,
                    histogram_height,
                    ui.input(|i| i.pointer.hover_pos()),
                );

                ui.input(|i| {
                    if i.pointer.button_released(egui::PointerButton::Primary) {
                        self.origin_drag_branch = None;
                        self.origin_drag_offset = None;
                    }

                    if i.pointer.button_pressed(egui::PointerButton::Primary)
                        && let Some(pos) = i.pointer.interact_pos()
                        && let Some((branch_index, origin_fractal)) = origin_hovered_branch
                        && let Some(pointer_fractal) = ui_to_fractal_space(
                            viewport_rect,
                            pos,
                            self.flame.camera_transform,
                            histogram_width,
                            histogram_height,
                        )
                    {
                        self.origin_drag_branch = Some(branch_index);
                        self.origin_drag_offset = Some(origin_fractal - pointer_fractal);
                    }

                    if i.pointer.button_pressed(egui::PointerButton::Middle)
                        && let Some(pos) = i.pointer.interact_pos()
                        && viewport_rect.contains(pos)
                    {
                        let camera_start = self.flame.camera_transform;
                        self.pan_camera_start = Some(camera_start);
                        self.pan_anchor_fractal = ui_to_fractal_space(
                            viewport_rect,
                            pos,
                            camera_start,
                            histogram_width,
                            histogram_height,
                        );
                    }

                    if i.pointer.button_released(egui::PointerButton::Middle) {
                        self.pan_camera_start = None;
                        self.pan_anchor_fractal = None;
                    }

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
                        self.pan_camera_start,
                        self.pan_anchor_fractal,
                        i.pointer.interact_pos(),
                        viewport_rect,
                    ) {
                        self.flame.camera_transform = next_camera;
                        flame_dirty = true;
                    }

                    if let (Some(branch_index), Some(origin_offset), Some(pos)) = (
                        self.origin_drag_branch,
                        self.origin_drag_offset,
                        i.pointer.interact_pos(),
                    )
                        && let Some(pointer_fractal) = ui_to_fractal_space(
                            viewport_rect,
                            pos,
                            self.flame.camera_transform,
                            histogram_width,
                            histogram_height,
                        )
                        && let Some(branch) = self.flame.branches.get_mut(branch_index)
                    {
                        let next_origin = pointer_fractal + origin_offset;
                        if branch.pre_affine.z_axis.x != next_origin.x
                            || branch.pre_affine.z_axis.y != next_origin.y
                        {
                            branch.pre_affine.z_axis.x = next_origin.x;
                            branch.pre_affine.z_axis.y = next_origin.y;
                            flame_dirty = true;
                        }
                    }
                });

                if flame_dirty {
                    renderer.update_flame(&self.flame);
                }

                // Detect if any flame parameters have changed to clear histogram
                let flame_changed = self.flame != self.last_flame;
                let should_clear_histogram = flame_dirty || flame_changed;
                
                // Update last_flame for next frame comparison
                if flame_changed {
                    self.last_flame = self.flame.clone();
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
                    origin_hovered_branch.map(|(branch_index, _)| branch_index),
                    self.origin_drag_branch,
                );
            } else {
                ui.label("GPU renderer not initialized. Check console for errors.");
                status_right = "Renderer unavailable";
            }
        });

        status_right
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
            .pan_camera_start
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
        println!(
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

    fn split_content_rects(
        content_rect: egui::Rect,
        left_panel_width: f32,
        splitter_width: f32,
    ) -> (egui::Rect, egui::Rect, egui::Rect) {
        let panel_right = (content_rect.left() + left_panel_width).min(content_rect.right());
        let splitter_right = (panel_right + splitter_width).min(content_rect.right());

        let left_panel_rect = egui::Rect::from_min_max(
            content_rect.left_top(),
            egui::pos2(panel_right, content_rect.bottom()),
        );
        let splitter_rect = egui::Rect::from_min_max(
            egui::pos2(panel_right, content_rect.top()),
            egui::pos2(splitter_right, content_rect.bottom()),
        );
        let viewport_rect = egui::Rect::from_min_max(
            egui::pos2(splitter_right, content_rect.top()),
            content_rect.right_bottom(),
        );

        (left_panel_rect, splitter_rect, viewport_rect)
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

    fn pick_hovered_origin_branch(
        viewport_rect: egui::Rect,
        flame: &Flame,
        histogram_width: u32,
        histogram_height: u32,
        hover_pos: Option<egui::Pos2>,
    ) -> Option<(usize, Vec2)> {
        let hover_pos = hover_pos?;

        for (branch_index, branch) in flame.branches.iter().enumerate() {
            let origin = branch.pre_affine.transform_point2(Vec2::ZERO);
            let Some(origin_ui) = fractal_to_ui_space(
                viewport_rect,
                origin,
                flame.camera_transform,
                histogram_width,
                histogram_height,
            ) else {
                continue;
            };

            if hover_pos.distance(origin_ui) <= TRIAD_HOVER_RADIUS {
                return Some((branch_index, origin));
            }
        }

        None
    }

    fn render_affine_triads(
        ui: &mut egui::Ui,
        viewport_rect: egui::Rect,
        flame: &Flame,
        histogram_width: u32,
        histogram_height: u32,
        origin_hovered_branch: Option<usize>,
        origin_drag_branch: Option<usize>,
    ) {
        let painter = ui.painter_at(viewport_rect);
        let hover_pos = ui.input(|i| i.pointer.hover_pos());
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

            let is_origin_hovered = origin_hovered_branch == Some(branch_index);
            let is_origin_dragged = origin_drag_branch == Some(branch_index) && pointer_down;
            let is_x_hovered = hover_pos.is_some_and(|pos| pos.distance(x_ui) <= TRIAD_HOVER_RADIUS);
            let is_y_hovered = hover_pos.is_some_and(|pos| pos.distance(y_ui) <= TRIAD_HOVER_RADIUS);

            let origin_color = if is_origin_hovered || is_origin_dragged { TRIAD_HOVER_COLOR } else { TRIAD_COLOR };
            let x_color = if is_x_hovered { TRIAD_HOVER_COLOR } else { TRIAD_COLOR };
            let y_color = if is_y_hovered { TRIAD_HOVER_COLOR } else { TRIAD_COLOR };

            let origin_radius = if is_origin_hovered || is_origin_dragged {
                TRIAD_POINT_RADIUS * 1.4
            } else {
                TRIAD_POINT_RADIUS
            };
            let x_radius = if is_x_hovered { TRIAD_POINT_RADIUS * 1.4 } else { TRIAD_POINT_RADIUS };
            let y_radius = if is_y_hovered { TRIAD_POINT_RADIUS * 1.4 } else { TRIAD_POINT_RADIUS };
            let hover_stroke = egui::Stroke::new(1.0, egui::Color32::from_rgb(30, 30, 30));

            painter.line_segment([o_ui, x_ui], egui::Stroke::new(TRIAD_LINE_STROKE, TRIAD_COLOR));
            painter.line_segment([o_ui, y_ui], egui::Stroke::new(TRIAD_LINE_STROKE, TRIAD_COLOR));

            painter.circle_filled(o_ui, origin_radius, origin_color);
            painter.circle_filled(x_ui, x_radius, x_color);
            painter.circle_filled(y_ui, y_radius, y_color);

            if is_origin_hovered || is_origin_dragged {
                painter.circle_stroke(o_ui, origin_radius, hover_stroke);
            }
            if is_x_hovered {
                painter.circle_stroke(x_ui, x_radius, hover_stroke);
            }
            if is_y_hovered {
                painter.circle_stroke(y_ui, y_radius, hover_stroke);
            }
        }

        if origin_drag_branch.is_some() && pointer_down {
            ui.output_mut(|o| o.cursor_icon = egui::CursorIcon::Grabbing);
        } else if origin_hovered_branch.is_some() {
            ui.output_mut(|o| o.cursor_icon = egui::CursorIcon::Grab);
        }
    }

    fn render_status_bar(
        &self,
        ui: &mut egui::Ui,
        status_rect: egui::Rect,
        status_right: &str,
    ) {
        let frame_count = self.gpu_renderer.as_ref().map(|r| r.frame_count()).unwrap_or(0);
        let status_left = format!("Frame Count: {}", frame_count);

        ui.scope_builder(egui::UiBuilder::new().max_rect(status_rect), |ui| {
            let frame = egui::Frame::new()
                .fill(egui::Color32::from_rgb(28, 30, 34))
                .inner_margin(egui::Margin::symmetric(8, 4))
                .stroke(egui::Stroke::new(1.0, egui::Color32::from_rgb(55, 58, 64)));

            frame.show(ui, |ui| {
                ui.set_height(20.0);
                ui.horizontal(|ui| {
                    ui.spacing_mut().item_spacing.x = 10.0;
                    ui.label(egui::RichText::new(&status_left).color(egui::Color32::from_gray(220)));
                    ui.separator();
                    ui.label(egui::RichText::new("Renderer: GPU").color(egui::Color32::from_gray(200)));
                    ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                        ui.label(
                            egui::RichText::new(status_right)
                                .color(egui::Color32::from_rgb(150, 210, 170)),
                        );
                    });
                });
            });
        });
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
