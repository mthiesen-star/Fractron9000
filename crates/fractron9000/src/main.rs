// Native entry point. The WASM entry point is `start()` in lib.rs.

#[cfg(not(target_arch = "wasm32"))]
fn main() -> eframe::Result<()> {
    eframe::run_native(
        "Fractron 9000",
        eframe::NativeOptions::default(),
        Box::new(|cc| Ok(Box::new(fractron9000_lib::FractronApp::new(cc)))),
    )
}

// WASM doesn't use main() — the entry point is #[wasm_bindgen(start)] in lib.rs.
// A stub is required so the binary target compiles when targeting wasm32.
#[cfg(target_arch = "wasm32")]
fn main() {}
