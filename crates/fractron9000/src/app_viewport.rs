use crate::camera_math::*;
use crate::gpu::GpuRenderer;
use crate::{
    FractronApp, TriadHandle, TRIAD_COLOR, TRIAD_HOVER_COLOR, TRIAD_HOVER_RADIUS,
    TRIAD_LINE_STROKE, TRIAD_POINT_RADIUS, ZOOM_SCROLL_SENSITIVITY,
};
use fractal_core::flame::Flame;
use glam::Vec2;

impl FractronApp {
    pub(crate) fn handle_viewport_input(
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
                    TriadHandle::XAxis => pre.transform_point2(Vec2::X),
                    TriadHandle::YAxis => pre.transform_point2(Vec2::Y),
                };
                self.triad_drag_handle_offset_ui = fractal_to_ui_space(
                    viewport_rect,
                    handle_fractal,
                    self.flame.camera_transform,
                    histogram_width,
                    histogram_height,
                )
                .map(|handle_ui| handle_ui - mouse_pos);
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
                        solve_pre_affine_x_axis_rotate_only(pre_affine_start, pointer_fractal)
                    } else if fixed_angle_no_skew {
                        solve_pre_affine_x_axis_endpoint(pre_affine_start, pointer_fractal)
                    } else {
                        solve_pre_affine_x_axis_rotate_scale_only(pre_affine_start, pointer_fractal)
                    }
                }
                TriadHandle::YAxis => {
                    if rotation_only {
                        solve_pre_affine_y_axis_rotate_only(pre_affine_start, pointer_fractal)
                    } else if fixed_angle_no_skew {
                        solve_pre_affine_y_axis_endpoint(pre_affine_start, pointer_fractal)
                    } else {
                        solve_pre_affine_y_axis_rotate_scale_only(pre_affine_start, pointer_fractal)
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

    pub(crate) fn dump_debug_state(
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
            .map(|m| {
                format!(
                    "[{:.4},{:.4},{:.4};{:.4},{:.4},{:.4}]",
                    m.x_axis.x, m.y_axis.x, m.z_axis.x, m.x_axis.y, m.y_axis.y, m.z_axis.y
                )
            })
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

    pub(crate) fn present_output_texture(
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

            let uv = egui::Rect::from_min_max(egui::pos2(0.0, 1.0), egui::pos2(1.0, 0.0));
            ui.add(egui::Image::new((texture_id, viewport_size)).uv(uv));
            "Rendering"
        } else {
            ui.label("Render state unavailable");
            "Render state unavailable"
        }
    }

    pub(crate) fn pick_hovered_triad_handle(
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

    pub(crate) fn render_affine_triads(
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

            let is_origin_dragged = is_selected && drag_handle == Some(TriadHandle::Origin) && pointer_down;
            let is_x_dragged = is_selected && drag_handle == Some(TriadHandle::XAxis) && pointer_down;
            let is_y_dragged = is_selected && drag_handle == Some(TriadHandle::YAxis) && pointer_down;

            let origin_color = if is_origin_hovered || is_origin_dragged {
                TRIAD_HOVER_COLOR
            } else {
                TRIAD_COLOR
            };
            let x_color = if is_x_hovered || is_x_dragged {
                TRIAD_HOVER_COLOR
            } else {
                TRIAD_COLOR
            };
            let y_color = if is_y_hovered || is_y_dragged {
                TRIAD_HOVER_COLOR
            } else {
                TRIAD_COLOR
            };

            let origin_radius = if is_origin_hovered || is_origin_dragged {
                TRIAD_POINT_RADIUS * 1.4
            } else {
                TRIAD_POINT_RADIUS
            };
            let x_radius = if is_x_hovered || is_x_dragged {
                TRIAD_POINT_RADIUS * 1.4
            } else {
                TRIAD_POINT_RADIUS
            };
            let y_radius = if is_y_hovered || is_y_dragged {
                TRIAD_POINT_RADIUS * 1.4
            } else {
                TRIAD_POINT_RADIUS
            };
            let hover_stroke = egui::Stroke::new(1.0, egui::Color32::from_rgb(30, 30, 30));

            let line_thickness = if is_selected {
                TRIAD_LINE_STROKE * 2.0
            } else {
                TRIAD_LINE_STROKE
            };
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
