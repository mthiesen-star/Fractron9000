//! Platform-specific "Open" implementation.
//!
//! - **Native**: opens the OS file-open dialog (synchronous) and writes the
//!   parsed flame to `slot` before returning.
//! - **WASM**: spawns an async task that shows a `<input type="file">` picker
//!   (works in all browsers) and writes the result to `slot` once the user
//!   has selected a file.
//!
//! In both cases the caller polls `slot` each frame and applies the flame
//! when it arrives.

use fractal_core::flame::Flame;
use fractal_core::persist;
use std::sync::{Arc, Mutex};

/// Shared slot used to deliver the loaded flame back to the app loop.
/// `None` = no result yet; `Some(Ok(flame))` = success; `Some(Err(…))` = parse/read error.
pub type OpenSlot = Arc<Mutex<Option<Result<Flame, String>>>>;

pub fn new_slot() -> OpenSlot {
    Arc::new(Mutex::new(None))
}

/// Initiate an open-file workflow, delivering the result into `slot`.
pub fn start_open(slot: &OpenSlot) {
    platform_start(Arc::clone(slot));
}

fn parse_bytes(bytes: Vec<u8>) -> Result<Flame, String> {
    let json = String::from_utf8(bytes).map_err(|e| format!("UTF-8 decode error: {}", e))?;
    persist::load_flame(&json)
        .map_err(|e| e.to_string())
        .map(|fr9k| fr9k.flame.into_flame(|_| None))
}

// ============================================================================
// Native — synchronous dialog, fills slot before returning
// ============================================================================

#[cfg(not(target_arch = "wasm32"))]
fn platform_start(slot: OpenSlot) {
    let Some(path) = rfd::FileDialog::new()
        .add_filter("Fractron 9000 Flame", &["f9k"])
        .pick_file()
    else {
        return; // user cancelled — leave slot empty
    };
    let result = std::fs::read(&path)
        .map_err(|e| format!("Read error for {:?}: {}", path, e))
        .and_then(parse_bytes);
    *slot.lock().unwrap() = Some(result);
}

// ============================================================================
// WASM — async via rfd's <input type="file"> (works in all browsers)
// ============================================================================

#[cfg(target_arch = "wasm32")]
fn platform_start(slot: OpenSlot) {
    wasm_bindgen_futures::spawn_local(async move {
        let Some(file) = rfd::AsyncFileDialog::new()
            .add_filter("Fractron 9000 Flame", &["f9k"])
            .pick_file()
            .await
        else {
            return; // user cancelled — leave slot empty
        };
        let bytes = file.read().await;
        let result = parse_bytes(bytes);
        *slot.lock().unwrap() = Some(result);
    });
}
