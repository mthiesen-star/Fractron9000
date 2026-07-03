#[cfg(target_arch = "wasm32")]
pub(crate) async fn start_wasm() {
    use crate::FractronApp;
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
