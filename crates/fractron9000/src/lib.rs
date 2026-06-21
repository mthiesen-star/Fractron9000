pub struct FractronApp {}

impl FractronApp {
    pub fn new(_cc: &eframe::CreationContext<'_>) -> Self {
        Self {}
    }
}

impl eframe::App for FractronApp {
    fn update(&mut self, ctx: &egui::Context, _frame: &mut eframe::Frame) {
        egui::CentralPanel::default().show(ctx, |ui| {
            ui.heading("Fractron 9000");
            ui.label("Fractal renderer coming soon...");
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
