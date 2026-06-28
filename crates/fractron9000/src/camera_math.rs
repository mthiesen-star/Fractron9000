use glam::{Mat3, Vec2, Vec3};

/// Compute the View-Projection-Screen transform used by both renderer and UI mapping.
///
/// This maps fractal space directly to histogram pixel space.
pub fn compute_vps_transform(camera: Mat3, width: u32, height: u32) -> Mat3 {
    let hw = width as f32 / 2.0;
    let hh = height as f32 / 2.0;
    let screen_transform = Mat3::from_cols(
        Vec3::new(hw, 0.0, 0.0),
        Vec3::new(0.0, hh, 0.0),
        Vec3::new(hw, hh, 1.0),
    );
    screen_transform * camera
}

pub fn ui_to_screen_space(viewport_rect: egui::Rect, pos: egui::Pos2) -> Option<Vec2> {
    let width = viewport_rect.width();
    let height = viewport_rect.height();
    if width <= 0.0 || height <= 0.0 {
        return None;
    }

    let u = (pos.x - viewport_rect.left()) / width;
    let v = (pos.y - viewport_rect.top()) / height;
    Some(Vec2::new(2.0 * u - 1.0, 1.0 - 2.0 * v))
}

pub fn ui_to_histogram_space(
    viewport_rect: egui::Rect,
    pos: egui::Pos2,
    histogram_width: u32,
    histogram_height: u32,
) -> Option<Vec2> {
    let width = viewport_rect.width();
    let height = viewport_rect.height();
    if width <= 0.0 || height <= 0.0 || histogram_width == 0 || histogram_height == 0 {
        return None;
    }

    let u = (pos.x - viewport_rect.left()) / width;
    let v_ui = (pos.y - viewport_rect.top()) / height;

    let hist_x = u * histogram_width as f32;
    let hist_y = (1.0 - v_ui) * histogram_height as f32;
    Some(Vec2::new(hist_x, hist_y))
}

pub fn ui_to_fractal_space(
    viewport_rect: egui::Rect,
    pos: egui::Pos2,
    camera: Mat3,
    histogram_width: u32,
    histogram_height: u32,
) -> Option<Vec2> {
    let hist = ui_to_histogram_space(viewport_rect, pos, histogram_width, histogram_height)?;
    let vps = compute_vps_transform(camera, histogram_width, histogram_height);
    let det = vps.determinant();
    if det.abs() <= 1e-8 {
        return None;
    }

    Some(vps.inverse().transform_point2(hist))
}

pub fn fractal_to_ui_space(
    viewport_rect: egui::Rect,
    point: Vec2,
    camera: Mat3,
    histogram_width: u32,
    histogram_height: u32,
) -> Option<egui::Pos2> {
    let width = viewport_rect.width();
    let height = viewport_rect.height();
    if width <= 0.0 || height <= 0.0 || histogram_width == 0 || histogram_height == 0 {
        return None;
    }

    let vps = compute_vps_transform(camera, histogram_width, histogram_height);
    let hist = vps.transform_point2(point);
    let u = hist.x / histogram_width as f32;
    let v_ui = 1.0 - (hist.y / histogram_height as f32);

    Some(egui::pos2(
        viewport_rect.left() + u * width,
        viewport_rect.top() + v_ui * height,
    ))
}

pub fn solve_pre_affine_origin_translation(pre_affine_start: Mat3, target_origin: Vec2) -> Mat3 {
    let mut next_pre_affine = pre_affine_start;
    next_pre_affine.z_axis.x = target_origin.x;
    next_pre_affine.z_axis.y = target_origin.y;
    next_pre_affine
}

pub fn solve_pan_camera_transform(
    pan_camera_start: Option<Mat3>,
    pan_anchor_fractal: Option<Vec2>,
    current_pos: Option<egui::Pos2>,
    viewport_rect: egui::Rect,
) -> Option<Mat3> {
    let (camera_start, anchor_fractal, current_pos) =
        (pan_camera_start?, pan_anchor_fractal?, current_pos?);
    let target_screen = ui_to_screen_space(viewport_rect, current_pos)?;

    // Keep the camera linear part fixed, then solve translation so the
    // stored fractal-space anchor remains directly under the cursor.
    let transformed_anchor = Vec2::new(
        camera_start.x_axis.x * anchor_fractal.x + camera_start.y_axis.x * anchor_fractal.y,
        camera_start.x_axis.y * anchor_fractal.x + camera_start.y_axis.y * anchor_fractal.y,
    );
    let translation = target_screen - transformed_anchor;

    let mut next_camera = camera_start;
    next_camera.z_axis.x = translation.x;
    next_camera.z_axis.y = translation.y;
    Some(next_camera)
}

pub fn solve_zoom_camera_transform(
    camera_start: Mat3,
    anchor_fractal: Vec2,
    target_screen: Vec2,
    zoom_factor: f32,
) -> Option<Mat3> {
    if !zoom_factor.is_finite() || zoom_factor <= 0.0 {
        return None;
    }

    let mut next_camera = camera_start;
    next_camera.x_axis.x *= zoom_factor;
    next_camera.x_axis.y *= zoom_factor;
    next_camera.y_axis.x *= zoom_factor;
    next_camera.y_axis.y *= zoom_factor;

    // Keep the cursor-anchored fractal point fixed on screen after scaling.
    let transformed_anchor = Vec2::new(
        next_camera.x_axis.x * anchor_fractal.x + next_camera.y_axis.x * anchor_fractal.y,
        next_camera.x_axis.y * anchor_fractal.x + next_camera.y_axis.y * anchor_fractal.y,
    );
    let translation = target_screen - transformed_anchor;
    next_camera.z_axis.x = translation.x;
    next_camera.z_axis.y = translation.y;

    Some(next_camera)
}

pub fn solve_aspect_camera_transform(camera_start: Mat3, viewport_aspect: f32) -> Option<Mat3> {
    if !viewport_aspect.is_finite() || viewport_aspect <= 1e-8 {
        return None;
    }

    let x_axis = Vec2::new(camera_start.x_axis.x, camera_start.x_axis.y);
    let y_axis = Vec2::new(camera_start.y_axis.x, camera_start.y_axis.y);
    let x_len = x_axis.length();
    let y_len = y_axis.length();
    if x_len <= 1e-8 || y_len <= 1e-8 {
        return None;
    }

    let desired_x_len = y_len / viewport_aspect;
    if (x_len - desired_x_len).abs() <= 1e-6 {
        return Some(camera_start);
    }

    let x_dir = x_axis / x_len;
    let mut next_camera = camera_start;
    next_camera.x_axis.x = x_dir.x * desired_x_len;
    next_camera.x_axis.y = x_dir.y * desired_x_len;
    Some(next_camera)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn round_trip_fractal_ui_fractal_preserves_point() {
        // Realistic viewport in UI points and target histogram in physical pixels.
        let viewport_rect = egui::Rect::from_min_size(
            egui::pos2(140.0, 72.0),
            egui::vec2(1366.0, 768.0),
        );
        let histogram_width = 2048;
        let histogram_height = 1152;

        // Non-trivial camera: scale, slight shear, translation.
        let camera = Mat3::from_cols(
            Vec3::new(0.92, 0.06, 0.0),
            Vec3::new(-0.04, 0.88, 0.0),
            Vec3::new(0.17, -0.11, 1.0),
        );

        // Test a few points to ensure robust behavior across the view.
        let points = [
            Vec2::new(0.0, 0.0),
            Vec2::new(-0.65, 0.35),
            Vec2::new(0.72, -0.54),
        ];

        for original in points {
            let ui_pos = fractal_to_ui_space(
                viewport_rect,
                original,
                camera,
                histogram_width,
                histogram_height,
            )
            .expect("fractal_to_ui_space should produce a valid UI position");

            let round_trip = ui_to_fractal_space(
                viewport_rect,
                ui_pos,
                camera,
                histogram_width,
                histogram_height,
            )
            .expect("ui_to_fractal_space should produce a valid fractal position");

            let eps = 1e-4;
            assert!(
                (round_trip.x - original.x).abs() <= eps,
                "round-trip x mismatch: original={}, got={}",
                original.x,
                round_trip.x
            );
            assert!(
                (round_trip.y - original.y).abs() <= eps,
                "round-trip y mismatch: original={}, got={}",
                original.y,
                round_trip.y
            );
        }
    }

    #[test]
    fn round_trip_ui_fractal_ui_preserves_position() {
        // Match the same realistic conditions as the companion test.
        let viewport_rect = egui::Rect::from_min_size(
            egui::pos2(140.0, 72.0),
            egui::vec2(1366.0, 768.0),
        );
        let histogram_width = 2048;
        let histogram_height = 1152;

        let camera = Mat3::from_cols(
            Vec3::new(0.92, 0.06, 0.0),
            Vec3::new(-0.04, 0.88, 0.0),
            Vec3::new(0.17, -0.11, 1.0),
        );

        let ui_points = [
            egui::pos2(260.0, 160.0),
            egui::pos2(980.0, 380.0),
            egui::pos2(1420.0, 740.0),
        ];

        for original in ui_points {
            let fractal = ui_to_fractal_space(
                viewport_rect,
                original,
                camera,
                histogram_width,
                histogram_height,
            )
            .expect("ui_to_fractal_space should produce a valid fractal position");

            let round_trip = fractal_to_ui_space(
                viewport_rect,
                fractal,
                camera,
                histogram_width,
                histogram_height,
            )
            .expect("fractal_to_ui_space should produce a valid UI position");

            let eps = 1e-3;
            assert!(
                (round_trip.x - original.x).abs() <= eps,
                "round-trip ui x mismatch: original={}, got={}",
                original.x,
                round_trip.x
            );
            assert!(
                (round_trip.y - original.y).abs() <= eps,
                "round-trip ui y mismatch: original={}, got={}",
                original.y,
                round_trip.y
            );
        }
    }
}
