use glam::{Mat3, Vec2};

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

pub fn ui_to_fractal_space(viewport_rect: egui::Rect, pos: egui::Pos2, camera: Mat3) -> Option<Vec2> {
    let screen = ui_to_screen_space(viewport_rect, pos)?;
    let det = camera.determinant();
    if det.abs() <= 1e-8 {
        return None;
    }

    Some(camera.inverse().transform_point2(screen))
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
