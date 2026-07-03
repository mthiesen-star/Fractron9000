use fractal_core::flame::Flame;
use fractal_core::io::parse_flame_file;

/// Load a named flame from a .flame file.
pub(crate) fn load_flame_from_file(file_path: &str, flame_name: &str) -> Result<Flame, String> {
    // Read the file
    let contents = std::fs::read_to_string(file_path)
        .map_err(|e| format!("Failed to read file '{}': {}", file_path, e))?;

    // Parse all flames from the file
    let flames = parse_flame_file(&contents)
        .map_err(|e| format!("Failed to parse flame file: {}", e))?;

    // Collect available names for error reporting
    let available_names: Vec<String> = flames.iter().map(|(n, _)| n.clone()).collect();

    // Find the requested flame by name
    for (name, flame) in flames {
        if name == flame_name {
            return Ok(flame);
        }
    }

    // If not found, list available flames for debugging
    Err(format!(
        "Flame '{}' not found in file. Available: {}",
        flame_name,
        available_names.join(", ")
    ))
}
