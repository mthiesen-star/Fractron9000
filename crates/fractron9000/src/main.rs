// Native entry point only — excluded from WASM builds.
// The WASM entry point is `start()` in lib.rs.
#![cfg(not(target_arch = "wasm32"))]

fn main() -> eframe::Result<()> {
    eframe::run_native(
        "Fractron 9000",
        eframe::NativeOptions::default(),
        Box::new(|cc| Ok(Box::new(fractron9000_lib::FractronApp::new(cc)))),
    )
}
