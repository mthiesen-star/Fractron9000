//! Platform-specific "Save As" implementation.
//!
//! - **Native**: opens the OS file-save dialog via `rfd` and writes the file.
//! - **WASM**: triggers a browser download of `flame.f9k` (works in all browsers).

use fractal_core::flame::Flame;
use fractal_core::persist;

/// Initiate a "Save As" workflow for `flame`.
///
/// On native this blocks briefly while the OS dialog is open, which is expected
/// behaviour for a file-save action.  On WASM the browser initiates a download
/// immediately with the filename `flame.f9k`.
pub fn save_as(flame: &Flame) {
    match persist::save_flame(flame) {
        Ok(json) => save_json(&json),
        Err(e) => log::error!("save_as: serialization failed: {}", e),
    }
}

// ============================================================================
// Native
// ============================================================================

#[cfg(not(target_arch = "wasm32"))]
fn save_json(json: &str) {
    let Some(path) = rfd::FileDialog::new()
        .add_filter("Fractron 9000 Flame", &["f9k"])
        .set_file_name("flame.f9k")
        .save_file()
    else {
        return; // user cancelled
    };
    if let Err(e) = std::fs::write(&path, json) {
        log::error!("save_as: failed to write {:?}: {}", path, e);
    }
}

// ============================================================================
// WASM — trigger a browser download via a temporary <a download> element
// ============================================================================

#[cfg(target_arch = "wasm32")]
fn save_json(json: &str) {
    use eframe::wasm_bindgen::{JsCast, JsValue};
    use web_sys::{Blob, BlobPropertyBag, HtmlAnchorElement, Url};

    // Build a JSON Blob from the string.
    let arr = js_sys::Array::of1(&JsValue::from_str(json));
    let mut opts = BlobPropertyBag::new();
    opts.type_("application/json");
    let Ok(blob) = Blob::new_with_str_sequence_and_options(arr.as_ref(), &opts) else {
        log::error!("save_as: failed to create Blob");
        return;
    };
    let Ok(url) = Url::create_object_url_with_blob(&blob) else {
        log::error!("save_as: failed to create object URL");
        return;
    };

    // Create a temporary <a download="flame.f9k"> and programmatically click it.
    let Some(document) = web_sys::window().and_then(|w| w.document()) else {
        log::error!("save_as: DOM not available");
        return;
    };
    let anchor = document
        .create_element("a")
        .ok()
        .and_then(|el| el.dyn_into::<HtmlAnchorElement>().ok());
    let Some(a) = anchor else {
        log::error!("save_as: failed to create anchor element");
        return;
    };
    a.set_href(&url);
    a.set_download("flame.f9k");
    if let Some(body) = document.body() {
        let _ = body.append_child(&a);
        a.click();
        let _ = body.remove_child(&a);
    }
    let _ = Url::revoke_object_url(&url);
}
