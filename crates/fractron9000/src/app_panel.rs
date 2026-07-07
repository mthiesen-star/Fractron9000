use crate::FractronApp;
use fractal_core::flame::Branch;
use fractal_core::flame::VariEntry;
use fractal_core::variations::Variation;
use glam::{Vec2, Vec4};

impl FractronApp {
    pub(crate) fn render_left_panel(
        &mut self,
        ui: &mut egui::Ui,
        left_panel_rect: egui::Rect,
        frame: &mut eframe::Frame,
    ) -> bool {
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

                if self.render_branch_tabs(ui) {
                    panel_dirty = true;
                }

                ui.add_space(8.0);

                if let Some(branch_index) = self.selected_branch {
                    if self.render_branch_parameter_controls(ui, branch_index) {
                        panel_dirty = true;
                    }

                    // Branch deletion can change selection; avoid using a stale index this frame.
                    if self.selected_branch != Some(branch_index) {
                        return;
                    }

                    ui.add_space(8.0);

                    if self.render_branch_variation_controls(ui, branch_index) {
                        panel_dirty = true;
                    }

                    ui.add_space(8.0);

                    if let Some(branch) = self.flame.branches.get(branch_index) {
                        let chroma = branch.chroma;
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

    pub(crate) fn render_tone_map_controls(&mut self, ui: &mut egui::Ui) -> bool {
        let mut changed = false;

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

    pub(crate) fn render_branch_tabs(&mut self, ui: &mut egui::Ui) -> bool {
        const MAX_BRANCHES: usize = 8;

        let branch_count = self.flame.branches.len();
        if branch_count == 0 {
            self.selected_branch = None;
            ui.label(egui::RichText::new("(no branches)").color(egui::Color32::from_gray(140)));
            return false;
        }

        debug_assert!(
            self.selected_branch.is_none_or(|branch_index| branch_index < branch_count),
            "selected_branch out of bounds: {:?} (branch_count={})",
            self.selected_branch,
            branch_count
        );

        let mut changed = false;
        ui.horizontal(|ui| {
            egui::ScrollArea::horizontal()
                .id_salt("branch-tabs")
                .max_height(24.0)
                .show(ui, |ui| {
                    ui.horizontal(|ui| {
                        for branch_index in 0..branch_count {
                            let selected = self.selected_branch == Some(branch_index);
                            let label = egui::RichText::new(branch_index.to_string()).size(10.0);
                            if ui
                                .add_sized(
                                    [18.0, 16.0],
                                    egui::Button::new(label).selected(selected),
                                )
                                .clicked()
                            {
                                self.selected_branch = Some(branch_index);
                                changed = true;
                            }
                        }
                    });
                });

            let can_add_branch = branch_count < MAX_BRANCHES;
            let add_response = ui
                .add_enabled(
                    can_add_branch,
                    egui::Button::new(egui::RichText::new("+").size(10.0)).min_size([18.0, 16.0].into()),
                )
                .on_hover_text(if can_add_branch {
                    "Add branch"
                } else {
                    "Maximum branch count reached"
                });

            if add_response.clicked()
            {
                self.flame.branches.push(Branch::default());
                self.selected_branch = Some(self.flame.branches.len() - 1);
                changed = true;
            }
        });

        changed
    }

    pub(crate) fn render_branch_parameter_controls(
        &mut self,
        ui: &mut egui::Ui,
        branch_index: usize,
    ) -> bool {
        let mut changed = false;

        if ui
            .button(egui::RichText::new("Delete Branch").size(11.0))
            .on_hover_text("Remove this branch")
            .clicked()
            && branch_index < self.flame.branches.len()
        {
            self.flame.branches.remove(branch_index);
            self.selected_branch = if self.flame.branches.is_empty() {
                None
            } else if branch_index >= self.flame.branches.len() {
                Some(self.flame.branches.len() - 1)
            } else {
                Some(branch_index)
            };
            return true;
        }

        let Some(branch) = self.flame.branches.get_mut(branch_index) else {
            return changed;
        };

        ui.add_space(6.0);

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

    pub(crate) fn render_branch_variation_controls(
        &mut self,
        ui: &mut egui::Ui,
        branch_index: usize,
    ) -> bool {
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

                    branch.variations[slot] =
                        VariEntry::new(selected_variation, weight.clamp(0.0, 1.0));
                    changed = true;
                }
            });
        }

        changed
    }

    pub(crate) fn render_palette_picker(
        &mut self,
        ui: &mut egui::Ui,
        frame: &mut eframe::Frame,
        chroma: Vec2,
    ) -> bool {
        const PALETTE_SIZE: f32 = 200.0;
        const CROSSHAIR_SIZE: f32 = 8.0;
        const CROSSHAIR_THICKNESS: f32 = 1.0;

        let (palette_rect, palette_response) =
            ui.allocate_exact_size(egui::Vec2::splat(PALETTE_SIZE), egui::Sense::click());

        let palette_texture_id = if let Some(render_state) = frame.wgpu_render_state() {
            if let Some(renderer) = self.gpu_renderer.as_ref() {
                let _ = renderer.palette_size();
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
            let painter = ui.painter_at(palette_rect);
            painter.image(
                texture_id,
                palette_rect,
                egui::Rect::from_min_max(egui::pos2(0.0, 0.0), egui::pos2(1.0, 1.0)),
                egui::Color32::WHITE,
            );

            let mut palette_changed = false;
            if palette_response.clicked() {
                if let Some(click_pos) = palette_response.interact_pointer_pos() {
                    let click_rel = click_pos - palette_rect.min;
                    let u = (click_rel.x / palette_rect.width()).clamp(0.0, 1.0);
                    let v = (click_rel.y / palette_rect.height()).clamp(0.0, 1.0);

                    if let Some(branch_mut) =
                        self.flame.branches.get_mut(self.selected_branch.unwrap())
                    {
                        if (branch_mut.chroma.x - u).abs() > f32::EPSILON
                            || (branch_mut.chroma.y - v).abs() > f32::EPSILON
                        {
                            branch_mut.chroma = Vec2::new(u, v);
                            palette_changed = true;
                        }
                    }
                }
            }

            let chroma_ui = palette_rect.min
                + egui::Vec2::new(chroma.x * palette_rect.width(), chroma.y * palette_rect.height());

            let crosshair_color = egui::Color32::WHITE;
            let h_offset = CROSSHAIR_SIZE / 2.0;

            painter.line_segment(
                [
                    chroma_ui - egui::Vec2::new(h_offset, 0.0),
                    chroma_ui + egui::Vec2::new(h_offset, 0.0),
                ],
                egui::Stroke::new(CROSSHAIR_THICKNESS, crosshair_color),
            );

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
}
