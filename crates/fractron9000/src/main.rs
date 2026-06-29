// Native entry point. The WASM entry point is `start()` in lib.rs.

#[cfg(not(target_arch = "wasm32"))]
fn main() -> eframe::Result<()> {
    let _ = env_logger::Builder::from_env(
        env_logger::Env::default().default_filter_or("info"),
    )
    .format_timestamp_millis()
    .try_init();

    // Parse command-line arguments
    let args: Vec<String> = std::env::args().collect();
    let mut load_flame_file: Option<String> = None;
    let mut load_flame_name: Option<String> = None;
    
    let mut i = 1;
    while i < args.len() {
        if args[i] == "--loadFlame" && i + 2 < args.len() {
            load_flame_file = Some(args[i + 1].clone());
            load_flame_name = Some(args[i + 2].clone());
            i += 3;
        } else {
            i += 1;
        }
    }
    
    eframe::run_native(
        "Fractron 9000",
        eframe::NativeOptions::default(),
        Box::new(move |cc| {
            Ok(Box::new(fractron9000_lib::FractronApp::new(
                cc,
                load_flame_file,
                load_flame_name,
            )))
        }),
    )
}

// WASM doesn't use main() — the entry point is #[wasm_bindgen(start)] in lib.rs.
// A stub is required so the binary target compiles when targeting wasm32.
#[cfg(target_arch = "wasm32")]
fn main() {}
