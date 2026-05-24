# Retruxel — Project Structure

## Solution Layout

```
Retruxel.slnx                    ← Solution file (VS 2022+ format)
│
├── Retruxel/                    ← WPF app shell (startup project)
├── Retruxel.Core/               ← Core library: interfaces, models, services
├── Retruxel.SDK/                ← Public SDK for plugin developers
├── Retruxel.Modules/            ← Standard portable module definitions
├── Retruxel.Toolchain/          ← Embedded toolchain adapter & orchestrator
├── Retruxel.Emulation/          ← LibRetro-based emulator integration
├── Retruxel.Splash/             ← Splash screen project builder
│
└── Plugins/
    ├── Targets/
    │   ├── Retruxel.Target.SMS/
    │   ├── Retruxel.Target.NES/
    │   ├── Retruxel.Target.GG/
    │   ├── Retruxel.Target.SG1000/
    │   └── Retruxel.Target.ColecoVision/
    ├── Tools/
    │   ├── Retruxel.Tool.*/     ← Standalone tools (editors, converters)
    │   ├── Retruxel.Lib.*/      ← Shared libraries used by tools
    │   └── ImageProcessing/
    └── CodeGens/
        ├── sprite/sms/          ← codegen.json + *.c.rtrx
        ├── tilemap/sms/
        ├── text_display/sms/
        └── ...                  ← One folder per module/target pair
```

## Retruxel.Core Internals

```
Retruxel.Core/
├── Interfaces/      ← ITarget, IModule, ITool, IRenderBackend, IToolchain, etc.
├── Models/          ← RetruxelProject, SceneData, ModuleManifest, AssetEntry, etc.
├── Services/        ← CodeGenerator, ModuleRenderer, TemplateEngine, ToolRegistry,
│                       TargetRegistry, ModuleLoader, ProjectManager, etc.
├── Engine/          ← GameState, RenderCommandBuffer (runtime abstractions)
├── Connectors/      ← Data flow connectors between assets, modules, tools
├── Helpers/         ← Utility classes
└── Text/            ← Built-in font data and FontRepository
```

## Retruxel (WPF Shell) Internals

```
Retruxel/
├── Views/           ← All screens/views (SceneEditor, TargetSelector, Settings, etc.)
├── Controls/        ← Reusable WPF controls
├── Windows/         ← Standalone windows
├── Services/        ← App-level services (navigation, etc.)
├── Themes/          ← WPF resource dictionaries (design system)
├── Assets/
│   ├── Localization/   ← en.json, pt-BR.json, ...
│   ├── Fonts/          ← Space Grotesk, Inter (embedded)
│   ├── Icons/
│   └── Data/           ← Static JSON data files
└── Localization/    ← Localization helpers
```

## Key Architectural Patterns

### Plugin Interfaces (Retruxel.Core/Interfaces/)
- `ITarget` — Console platform implementation (toolchain, code gen, hardware palette, specs)
- `IModule` / `IGraphicModule` / `ILogicModule` / `IAudioModule` — Module types
- `ITool` — Asset converters and preprocessors
- `IToolExtension` — Target-specific tool behavior (e.g., `SmsPaletteGenerator`)
- `IRenderBackend` — Platform-specific VRAM write implementation
- `IToolchain` / `IToolchainBuilder` — Compiler/assembler wrappers

### Planes & Layers
`TargetSpecs.Planes` is an array of `PlaneSpecs` — one entry per hardware BG plane. Each `PlaneSpecs` describes the plane's capabilities (flip support, bits-per-pixel, palette mode, default/max dimensions). Sprites are implicit and not listed here.

Within a scene, each hardware plane is represented by a `PlaneData` instance, which holds an unbounded list of `PlaneLayerData` entries. There is **no `MaxLayers` cap** — the user can add as many layers as they want. The practical limit is VRAM budget, reported in real time by `VramAllocator.Analyze()`.

**Palette slot ownership:**
- `PlaneData.PaletteSlot` — the default palette slot for the entire hardware plane. Used as-is for `PaletteMode.PerPlane` targets; used as the fallback for `PaletteMode.PerTile` targets.
- `TileEntry.PaletteSlot` — per-tile override, only meaningful for `PaletteMode.PerTile` targets (e.g. SMS). Value `-1` means "use the plane default".
- `PlaneLayerData` has **no `PaletteSlot`** — a layer is a logical editor subdivision and has no opinion about palette. Never add `PaletteSlot` to `PlaneLayerData`.

**Never use `VramRegionId` or any hardcoded region concept** — it has been replaced by the `Planes` array + `VramAllocator`.

### VRAM Allocation
`VramAllocator` (Retruxel.Core/Services/) is the single source of truth for VRAM usage:

- **`Analyze(scene, target)`** — dry run, called on every editor refresh. Returns a `VramUsageReport` with per-plane and per-layer byte breakdowns. Never mutates state.
- **`Allocate(scene, target, assets)`** — build-time pass. Assigns sequential `tileOffset` per asset (deduplicated across layers sharing the same asset). Returns `VramAllocation` with a `FitsInVram` flag.

The editor uses `VramUsageReport` to show the user how much VRAM is consumed and how much remains — this is what informs whether adding more layers is viable, not a hardcoded spec field.

### Declarative CodeGen (Plugins/CodeGens/)
Each module+target pair has a folder with:
- `codegen.json` — variable mappings and tool invocations
- `*.c.rtrx` — C code template with `{{variable}}`, `{{#if}}`, `{{#each}}` syntax

### Module Scope
- `ModuleScope.Project` — initialized in `main.c` OnStart, persists across scenes (input, physics, sprite, entity)
- `ModuleScope.Scene` — initialized in `scene_X_init()`, reloaded per scene (palette, tilemap, text.display)

### State-Based Rendering
Modules populate `GameState` and set dirty flags. Only `Engine_Render()` writes to VRAM, exclusively during VBlank. No module may write to VRAM directly.

### Event System
- `OnStart` — runs once at boot
- `OnVBlank` — runs every frame (~60Hz); update logic + render
- `OnInput_Button1/2` — input handlers; modify state only, no rendering

## Fundamental Rules (must follow)

1. **Kung Fu Master is the primary use case** — validate every feature against it
2. **Zero hardcoded target knowledge in Core** — never `if (targetId == "sms")` outside a target plugin; always use `ITarget` interface methods
3. **Tools delegate to `IToolExtension`** — target-specific tool logic lives in target plugins, not in the tool itself
4. **Engine is target-agnostic** — Core calls `ITarget.GenerateEngineRuntime()`, never instantiates target-specific classes
5. **Event-based execution** — modules grouped by `Trigger` field; separate `OnStart`, `OnVBlank`, `OnInput` sections in generated code
6. **No direct VRAM writes** — all rendering goes through `GameState` dirty flags → `Engine_Render()` → VRAM
7. **No `MaxLayers` in specs** — layer count is unbounded; VRAM budget (via `VramAllocator`) is the only real constraint
8. **No `VramRegionId`** — deprecated; use `PlaneId` referencing `TargetSpecs.Planes[]`
9. **No `PaletteSlot` on `PlaneLayerData`** — palette slot belongs to `PlaneData` (hardware plane) or `TileEntry` (per-tile override); never add it to the layer model

## Naming Conventions

- Projects: `Retruxel.<Area>` or `Retruxel.Target.<Console>` or `Retruxel.Tool.<Name>`
- Target IDs: lowercase short strings (`"sms"`, `"nes"`, `"gg"`, `"sg1000"`, `"coleco"`)
- Module IDs: lowercase with dots for namespacing (`"text.display"`, `"text.array"`, `"custom.text"`)
- Plane IDs: lowercase short strings (`"bg"` for SMS, `"plane_a"`, `"plane_b"` for Mega Drive, `"bg1"`–`"bg4"` for SNES)
- CodeGen folders: `{moduleId}/{targetId}/` (e.g., `sprite/sms/`)
- Generated C functions: `{moduleId}_{instanceId}_init()`, `{moduleId}_{instanceId}_update()`
- WPF partial classes for large windows use `_` suffix files (e.g., `TilemapEditorWindow_Canvas.cs`)
