# Fractron 9000 — Agent Instructions

This file captures architectural decisions and conventions for AI-assisted development.
Read this before making any significant changes to the codebase.

## Project Overview

Fractron 9000 is a GPU-accelerated flame fractal renderer. It uses the chaos game /
iterated function system (IFS) algorithm: thousands of GPU threads each walk a random
trajectory through a set of parametric variation functions, accumulating hits into a
histogram, which is then tone-mapped to produce the final image.

The original project (in `Legacy/`) was written in C# .NET 4.0 with CUDA and OpenCL
backends. This is a full rewrite targeting modern platforms including the browser.

## Technology Stack

| Concern            | Choice                                      |
|--------------------|---------------------------------------------|
| Host language      | Rust                                        |
| UI framework       | egui / eframe                               |
| GPU rendering      | wgpu + WGSL compute shaders                 |
| Accumulation       | Fixed-point `atomicAdd` on `u32` buffers    |
| Math               | glam                                        |
| Flame file I/O     | quick-xml + serde (Apophysis-compatible XML)|
| WASM build tool    | trunk                                       |

## Crate Structure

```
fractron9000/              ← Cargo workspace root
├── crates/
│   ├── fractal-core/      ← Pure Rust, no GPU deps
│   │                         Flame data model, variation math, affine transforms,
│   │                         palette model, flame XML import/export
│   └── fractron9000/      ← Full application
│                             egui UI, wgpu renderer, eframe entry point (native + WASM)
├── AGENTS.md
└── Legacy/                ← Original C# source, read-only reference
```

`fractal-core` must never take a dependency on wgpu, egui, or any GPU/windowing crate.
This keeps it fully unit-testable and allows a CPU fallback renderer to live there.

## Rendering Pipeline

1. **Iterate** (WGSL compute shader) — N threads each run the chaos game for M steps.
   Each step: pick random branch → apply pre-affine → apply variations → apply post-affine
   → blend color → scatter-write to histogram via `atomicAdd`.
2. **Tone-map** (WGSL compute shader) — read histogram, apply log scale + gamma + vibrancy,
   write to an RGBA texture.
3. **Display** (wgpu render pipeline) — fullscreen quad samples the tone-mapped texture.

## Coordinate Spaces

Use these names consistently in code, comments, and debugging output.

1. **Fractal Space**
  - Continuous floating-point 2D coordinates where the chaos-game iteration runs.
  - Branch transforms and variations operate in this space.

2. **Screen Space**
  - Continuous floating-point 2D coordinates after applying `camera_transform`.
  - Intended visible region is normalized to approximately `[-1, 1]` in each axis.
  - Mapping: `screen = camera_transform * fractal` (affine 2D form).
  - Y Axis Orientation: Increases toward the *top* of the screen.

3. **Histogram Space**
  - Discrete integer bin coordinates used for accumulation.
  - Derived from Screen Space via normalization and raster mapping:
    - `hist_x = clamp((screen_x + 1) * 0.5 * width,  0, width  - 1)`
    - `hist_y = clamp((screen_y + 1) * 0.5 * height, 0, height - 1)`
  - Y Axis Orientation: Increases toward the *top* of the screen.

4. **UI Space**
  - egui point-space coordinates (logical UI units), not raw texture pixels.
  - Input pointer positions and viewport rectangles are expressed in this space.
  - Due to DPI scaling (`pixels_per_point`), one histogram texel does not necessarily equal one UI unit.
  - Y Axis Orientation: Increases toward the *bottom* of the screen. 

### Y-Axis Convention Note

- Due to common mathematical conventions, the chaos game takes place in a coordinate system where positive Y points up.
- egui and most other UI systems typically have the Y axis pointing down.
- The UI space transform is responsible for flipping the Y axis when presenting the tone-mapped result. 

### Histogram Accumulation

Uses fixed-point integer accumulation: float color values are scaled and stored as `u32`
via `atomicAdd` on a `array<atomic<u32>>` storage buffer. Precision is sufficient for
interactive preview. Upgrade to workgroup-local accumulation later if needed.

### Tone Mapping Formula

```
scale  = C1 * brightness / (pixel_area * total_iterations)
ka     = log10(1 + raw * scale) / raw
logPix = raw * ka
result = lerp(logPix^(1/gamma), logPix^(1/gamma) / logPix * logPix, vibrancy)
result = saturate(result)
```

Parameters: `brightness`, `gamma` (typically 2.0), `vibrancy` (0–1), `background`.

## Flame File Format

Apophysis-compatible XML. Must be able to import existing `.flame` files from the
Legacy project and from the Apophysis/Fractorium ecosystem.

Key XML elements:
- `<flame>` — top-level: camera (size, center, scale, rotate), tone-map params
- `<xform>` — a branch: pre/post affine, color (u,v), weight, per-variation weights
- `<finalxform>` — optional final transform applied after branch selection
- `<palette>` — 256 RGB color entries as hex strings

Use `quick-xml` for parsing and `serde` for struct mapping where possible.

## Variation System

30 named variation functions, each applied with a per-branch weight. Implemented in
WGSL as inline functions in the iterate shader. Host-side (`fractal-core`) holds the
variation enum, names, and default weights for UI purposes.

Variation IDs must match the Legacy codebase for flame file compatibility.

## Key Dependencies (anticipated)

```toml
# fractal-core
serde = { version = "1", features = ["derive"] }
quick-xml = { version = "0.31", features = ["serialize"] }
glam = "0.29"

# fractron9000
eframe = "0.29"          # egui + winit + wgpu integration
wgpu = "22"
glam = "0.29"
```

## Build Targets

```bash
# Native desktop
cargo run -p fractron9000

# Browser (WASM) — requires trunk; must run from the app crate directory
cd crates/fractron9000
trunk serve               # dev server with hot reload
trunk build --release     # production bundle → dist/
```

The app must compile and run correctly for both targets. WASM-incompatible code
(filesystem access, threads via std::thread) must be conditionally compiled.

## Deferred Features

The following are explicitly out of scope for v1 and should not be designed around:

- **Animation / morphing** — keyframe interpolation between flame parameter sets
- **High-resolution export** — offline render at higher iteration counts / supersampling
- **Video export**
- **CPU fallback renderer** — the `fractal-core` crate structure supports this, but
  the implementation is deferred

## Conventions

- Prefer the option that requires the least code while remaining readable.
- Use static typing and type annotations throughout.
- `fractal-core` structs should derive `serde::Serialize` / `serde::Deserialize` where
  they participate in file I/O.
- WGSL shader files live in `crates/fractron9000/src/shaders/`.
- The `Legacy/` folder is read-only reference material. Do not modify it.
