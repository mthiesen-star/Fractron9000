use crate::{FractronApp, MenuAction, TriadHandle, RendererStatus};

impl FractronApp {
    pub(crate) fn ui_regions(&self, full_rect: egui::Rect) -> (egui::Rect, egui::Rect, egui::Rect) {
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

    pub(crate) fn render_menu_bar(&self, ui: &mut egui::Ui, menu_rect: egui::Rect) -> MenuAction {
        let mut action = MenuAction::None;
        ui.scope_builder(egui::UiBuilder::new().max_rect(menu_rect), |ui| {
            let frame = egui::Frame::new()
                .fill(egui::Color32::from_rgb(14, 16, 20))
                .inner_margin(egui::Margin::symmetric(6, 2))
                .stroke(egui::Stroke::new(1.0, egui::Color32::from_rgb(38, 42, 48)));

            frame.show(ui, |ui| {
                egui::MenuBar::new().ui(ui, |ui| {
                    ui.menu_button("File", |ui| {
                        if ui.button("Open...").clicked() {
                            action = MenuAction::Open;
                            ui.close();
                        }
                        if ui.button("Save As...").clicked() {
                            action = MenuAction::SaveAs;
                            ui.close();
                        }
                    });
                    ui.menu_button("Edit", |_ui| {});
                });
            });
        });
        action
    }

    pub(crate) fn render_splitter(
        ui: &mut egui::Ui,
        splitter_rect: egui::Rect,
        splitter_hovered: bool,
        splitter_dragged: bool,
    ) {
        ui.scope_builder(egui::UiBuilder::new().max_rect(splitter_rect), |ui| {
            let stroke_color = if splitter_dragged || splitter_hovered {
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
            if splitter_hovered || splitter_dragged {
                ui.output_mut(|o| o.cursor_icon = egui::CursorIcon::ResizeHorizontal);
            }
        });
    }

    pub(crate) fn split_content_rects(
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

    pub(crate) fn render_status_bar(
        &self,
        ui: &mut egui::Ui,
        status_rect: egui::Rect,
        renderer_status: RendererStatus,
    ) {
        let drag_constraint_hint = self.active_triad_axis_constraint_hint(ui);

        ui.scope_builder(egui::UiBuilder::new().max_rect(status_rect), |ui| {
            let frame = egui::Frame::new()
                .fill(egui::Color32::from_rgb(28, 30, 34))
                .inner_margin(egui::Margin::symmetric(8, 4))
                .stroke(egui::Stroke::new(1.0, egui::Color32::from_rgb(55, 58, 64)));

            frame.show(ui, |ui| {
                ui.set_height(20.0);
                ui.horizontal(|ui| {
                    ui.spacing_mut().item_spacing.x = 10.0;

                    // Left: Constraint hint
                    ui.push_id("status_left", |ui| {
                        if let Some(hint) = drag_constraint_hint {
                            ui.label(egui::RichText::new(hint).color(egui::Color32::from_rgb(224, 196, 118)));
                        }
                    });

                    // Right: Renderer status or performance stats
                    ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                        match renderer_status {
                            RendererStatus::Error(msg) => {
                                ui.label(egui::RichText::new(msg).color(egui::Color32::from_rgb(255, 100, 100)));
                            }
                            RendererStatus::Ok => {
                                let (iterations_millions, iterations_per_sec_millions, quality) = self
                                    .gpu_renderer
                                    .as_ref()
                                    .map(|r| {
                                        (
                                            r.iteration_count() as f64 / 1_000_000.0,
                                            r.iterations_per_sec() / 1_000_000.0,
                                            r.quality(),
                                        )
                                    })
                                    .unwrap_or((0.0, 0.0, 0.0));
                                let perf_text = format!(
                                    "Performed {:.1}M iterations at {:.1}M iterations/sec for a quality of {:.0}",
                                    iterations_millions, iterations_per_sec_millions, quality
                                );
                                ui.label(egui::RichText::new(&perf_text).color(egui::Color32::from_gray(220)));
                            }
                        }
                    });
                });
            });
        });
    }

    pub(crate) fn active_triad_axis_constraint_hint(&self, ui: &egui::Ui) -> Option<&'static str> {
        let (primary_down, shift, alt) = ui.input(|i| {
            (
                i.pointer.button_down(egui::PointerButton::Primary),
                i.modifiers.shift,
                i.modifiers.alt,
            )
        });
        if !primary_down {
            return None;
        }

        match self.triad_drag_handle {
            Some(TriadHandle::XAxis) | Some(TriadHandle::YAxis) => {
                if shift {
                    Some("Constraint: Rotate only")
                } else if alt {
                    Some("Constraint: Unconstrained")
                } else {
                    Some("Constraint: Rotate + Scale")
                }
            }
            _ => None,
        }
    }
}
