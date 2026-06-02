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
        ├── entity/sms/          ← codegen.json + entity.c.rtrx
        ├── enemy/sms/
        ├── plane/sms/
        ├── sprite/sms/
        ├── tilemap/sms/
        ├── text_display/sms/
        └── ...                  ← One folder per module/target pair
```

## Retruxel.Core Internals

```
Retruxel.Core/
├── Interfaces/      ← ITarget, IModule, ITool, IRenderBackend, IToolchain, etc.
├── Models/          ← RetruxelProject, SceneData, ModuleManifest, AssetEntry,
│                       InputModels (InputPort, InputButton, InputPortBinding), etc.
├── Services/        ← CodeGenerator, ModuleRenderer, TemplateEngine, ToolRegistry,
│                       TargetRegistry, ModuleLoader, ProjectManager, VramAllocator,
│                       SatAllocator, etc.
├── Engine/          ← GameState, RenderCommandBuffer (runtime abstractions)
├── Connectors/      ← Data flow connectors between assets, modules, tools
├── Helpers/         ← Utility classes
└── Text/            ← Built-in font data and FontRepository
```

## Retruxel (WPF Shell) Internals

```
Retruxel/
├── Views/           ← All screens/views AND modal dialogs
│   ├── SceneEditor/ ← SceneEditorView + partial classes:
│   │   ├── SceneEditorView.xaml.cs        ← main + DI + initialization
│   │   ├── SceneEditorView_Tree.cs        ← project tree builder
│   │   ├── SceneEditorView_Elements.cs    ← CRUD for planes/entities/modules
│   │   ├── SceneEditorView_Properties.cs  ← right panel property editors
│   │   └── SceneEditorView_Preview.cs     ← canvas rendering (MapIndex pipeline)
│   ├── ModulePickerWindow.xaml/.cs        ← Module picker modal dialog
│   └── *.xaml/.cs   ← Other views and dialogs
├── Controls/        ← Reusable WPF controls
├── Services/        ← App-level services (navigation, etc.)
├── Themes/          ← WPF resource dictionaries (design system)
├── Assets/
│   ├── Localization/   ← en.json, pt-BR.json, ...
│   ├── Fonts/          ← Space Grotesk, Inter (embedded)
│   ├── Icons/
│   └── Data/           ← Static JSON data files
└── Localization/    ← Localization helpers
```

**Dialog pattern:** Modal dialogs are `Window` subclasses placed directly in `Views/`, not in `Windows/`. They use `WindowStyle="None"`, `AllowsTransparency="True"`, `CornerRadius="6"` on the outer border (physical window frame only — inner UI stays 0px radius), and `WindowStartupLocation="CenterOwner"`. Title bars implement drag via `DragMove()` on `MouseLeftButtonDown`.

## Key Architectural Patterns

### Plugin Interfaces (Retruxel.Core/Interfaces/)
- `ITarget` — Console platform implementation (toolchain, code gen, hardware palette, specs, input ports)
- `IModule` / `IGraphicModule` / `ILogicModule` / `IAudioModule` — Module types
- `ITool` — Asset converters and preprocessors
- `IToolExtension` — Target-specific tool behavior (e.g., `SmsPngToTilesExtension`)
- `IRenderBackend` — Platform-specific VRAM write implementation
- `IToolchain` / `IToolchainBuilder` — Compiler/assembler wrappers

### Entity System

`EntityData` is the owner of sprite and behavior configuration. Modules no longer float as independent scene elements for entities — they are absorbed into the `EntityData` hierarchy:

- `EntityData.SpriteAssetId` — which asset to render
- `EntityData.PaletteSlot` — which palette slot (combo in UI, not free text)
- `EntityData.InputSlot` — index into `RetruxelProject.InputPorts` (-1 = no input)
- `EntityData.WidthTiles` / `HeightTiles` — render grid size; **must match the asset layout**
- `EntityData.ModuleOverrides` — per-entity parameter overrides (physics, AI, etc.)
- `EntityData.EntityType` — drives which `codegen.json` folder is used (e.g. `"entity"`, `"enemy"`)

**Entity codegen tile layout:** tiles in VRAM are sequential starting at `START_TILE`. The update loop draws `widthTiles × heightTiles` hardware sprites row-major: `tile_idx = row * WIDTH_TILES + col`. The asset sheet must be organized with the same column count as `WidthTiles` for the mapping to be correct.

### Input System

`ITarget.GetInputPorts()` returns the hardware default `InputPort[]`. On project creation/load, `EnsureInputPorts(project, target)` copies these into `RetruxelProject.InputPorts` as `InputPortBinding[]` (remappable by the user). `EntityData.InputSlot` is the index into this array.

### Properties Panel (SceneEditorView_Properties.cs)

The right panel renders typed controls based on the selected item:
- **`PlaneLayerData`** — text rows + palette slot combo (lists real slots from target)
- **`EntityData`** — type-level rows (sprite asset, width/height tiles) + variant rows (name, palette slot combo, input slot combo, start X/Y)
- **`ProjectModuleData`** — label, enabled combo, then manifest-driven parameters (Enum → combo, Bool → combo, Int/String → textbox)

`AddPropertyCombo(label, options, currentValue, onChange)` is the helper for dropdown rows. `AddPropertyRow` is for free-text rows. Never use free text for palette slots, input slots, or enum parameters.

`BuildModuleProperties` reads `GetManifest()` from the live module instance (via `ResolveManifest(registry, moduleId)` which searches all three registries). Module state is read/written via `ParseStateDict` / `SerializeStateDict` / `MergeJson` — the original JSON injected by `CodeGenerator` is preserved through the round-trip.

### Planes & Layers
`TargetSpecs.Planes` is an array of `PlaneSpecs` — one entry per hardware BG plane. Each `PlaneSpecs` describes the plane's capabilities (flip support, bits-per-pixel, palette mode, default/max dimensions). Sprites are implicit and not listed here.

Within a scene, each hardware plane is represented by a `PlaneData` instance, which holds an unbounded list of `PlaneLayerData` entries. There is **no `MaxLayers` cap** — the user can add as many layers as they want. The practical limit is VRAM budget, reported in real time by `VramAllocator.Analyze()`.

**Palette slot ownership:**
- `PlaneData.PaletteSlot` — the default palette slot for the entire hardware plane.
- `TileEntry.PaletteSlot` — per-tile override, only meaningful for `PaletteMode.PerTile` targets. Value `-1` means "use the plane default".
- `PlaneLayerData` has **no `PaletteSlot`** — never add it to the layer model.

**Never use `VramRegionId` or any hardcoded region concept** — replaced by the `Planes` array + `VramAllocator`.

### VRAM Allocation
`VramAllocator` (Retruxel.Core/Services/) is the single source of truth for VRAM usage:

- **`Analyze(scene, target)`** — dry run, called on every editor refresh. Returns a `VramUsageReport`. Never mutates state.
- **`Allocate(scene, target, assets)`** — build-time pass. Assigns sequential `tileOffset` per asset. Returns `VramAllocation` with a `FitsInVram` flag.

`SatAllocator.Allocate(scene, target)` assigns SAT slots to entities: each entity occupies `WidthTiles × HeightTiles` hardware sprite slots sequentially.

### CodeGenerator — Entity State Injection

`CodeGenerator` builds `entityState` directly from `EntityData` fields and injects it via `RegisterModuleData`. Because `EntityModule.EntityState` does not know about `spriteAssetId`, `startTile`, etc., the original JSON is preserved in `originalJsonByModule` and merged with the contextual module JSON via `MergeJson(original, contextual)` before passing to `ModuleRenderer`. This prevents the `Deserialize→Serialize` round-trip from losing injected fields.

### Declarative CodeGen (Plugins/CodeGens/)
Each module+target pair has a folder with:
- `codegen.json` — variable mappings and tool invocations
- `*.c.rtrx` — C code template with `{{variable}}`, `{{#if}}`, `{{#each}}` syntax

### Module Picker (`ModulePickerWindow`)

`ModulePickerWindow` is the standard UI for adding modules to the project tree. It is opened via the static factory:

```csharp
ModulePickerWindow.Open(
    registry:       _moduleRegistry,
    scope:          isGlobal ? ModuleScope.Project : ModuleScope.Scene,
    existing:       /* moduleIds already at this level */,
    projectModules: /* moduleIds already at project level */,
    onSelected:     moduleId => AddModuleAndOpenProperties(moduleId, isGlobal),
    owner:          Window.GetWindow(this));
```

**Filtering rules:**
- `scope == Project` → show only modules where `IModule.DefaultScope == ModuleScope.Project`
- `scope == Scene` → show all modules; modules already in `projectModules` get an `[OVERRIDE]` badge
- `SingletonPolicy.Global` modules already in `existing` → shown disabled ("JÁ ADICIONADO"), not selectable

### Module Scope & Singleton Policy (editor perspective)

| Property | Values | Editor effect |
|---|---|---|
| `IModule.DefaultScope` | `Project`, `Scene` | Determines which MODULES section the module appears in by default |
| `IModule.SingletonPolicy` | `Global`, `PerScene`, `Multiple` | `Global` already added → disabled in picker; `Multiple` → always enabled |

### State-Based Rendering
Modules populate `GameState` and set dirty flags. Only `Engine_Render()` writes to VRAM, exclusively during VBlank. No module may write to VRAM directly.

### Event System
- `OnStart` — runs once at boot
- `OnVBlank` — runs every frame (~60Hz); update logic + render
- `OnInput_Button1/2` — input handlers; modify state only, no rendering

## Asset Model & MapIndex Pipeline

Assets are **never rendered as PNG at runtime**. This is an immutable rule.

When a PNG is imported, it is processed into a `MapIndex` — a flat `byte[]` where each byte is a palette color index. This is stored in `AssetGenerationParams.MapIndex` alongside `Palette: List<string>` (hex colors). The PNG file on disk is only the source; the canonical runtime form is `MapIndex`.

**`AssetGenerationParams` key fields:**
- `MapIndex` — `byte[]`, one byte per pixel, palette index (0–N)
- `Palette` — `List<string>` hex colors matching the index space
- `OptimizedWidth` / `OptimizedHeight` — pixel dimensions of the processed image
- `TileCount` — number of 8×8 tiles

**Rendering pipeline (editor preview and codegen):**
1. `MapIndex` + `Palette` + `OptimizedWidth/Height` → `IndexedBitmapRenderer.Render()` → `SKBitmap` of full tileset
2. Crop individual tiles from the `SKBitmap` by `tileId`: `srcX = (tileId % assetCols) * tileSize`
3. Compose the scene layer or entity sprite tile-by-tile onto an output `SKBitmap`
4. Convert `SKBitmap` → WPF `WriteableBitmap` for display

**Entity sprite preview** uses `widthTiles × heightTiles` from `EntityData` to determine how many tiles to draw, and `assetCols = OptimizedWidth / tileSize` to map tile indices to asset positions. This matches exactly what the codegen loop does at runtime.

**Tools never read MapIndex from disk.** `PngToTilesTool` reads exclusively from the in-memory asset registry injected by `VariableResolver`. If `inMemoryMapIndex` is not present, the tool throws — missing MapIndex is a hard error, not a recoverable condition.

## Scene Editor Tree — Planes & Layers

The tree always shows **all hardware planes** defined in `TargetSpecs.Planes`, regardless of whether `scene.Planes` has been populated. `BuildPlanesSection` iterates `_target.Specs.Planes` (not `scene.Planes`) and creates missing `PlaneData` entries on the fly.

Layers are **user-added** — they never appear automatically. The `+` button on each plane row adds a `PlaneLayerData`. There is no layer count cap.

`EnsureDefaultPlanes` is safe to call on existing scenes — it only adds planes that are missing (no early-return guard on `Count > 0`).

`EnsureInputPorts(project, target)` is called in `Initialize` — safe to call on existing projects, only adds ports that are missing.

## VisualToolInvoker — Tilemap Editor Integration

`VisualToolInvoker.OpenTilemapEditor` passes `"layer"` (`PlaneLayerData`) and `"planeData"` (`PlaneData`) in the input dict. `TilemapEditorTool.CreateWindow` reads these keys and builds a `moduleData` dict from the layer's current state before calling `LoadModuleData` — the editor always opens pre-populated with existing tile data.

On save, `VisualToolInvoker` reads `ModuleData` back and updates:
- `layer.AssetId` ← `"tilesAssetId"`
- `layer.Width` / `layer.Height` ← `"mapWidth"` / `"mapHeight"` (via `Convert.ToInt32`, not `is int`)
- `layer.Tiles` ← `"mapData"` deserialized with `PropertyNameCaseInsensitive = true`
- `planeData.PaletteSlot` ← `"paletteSlot"`

## Fundamental Rules

1. **Kung Fu Master is the primary use case** — validate every feature against it
2. **Zero hardcoded target knowledge in Core** — never `if (targetId == "sms")` outside a target plugin; always use `ITarget` interface methods
3. **Tools delegate to `IToolExtension`** — target-specific tool logic lives in target plugins, not in the tool itself
4. **Engine is target-agnostic** — Core calls `ITarget.GenerateEngineRuntime()`, never instantiates target-specific classes
5. **Event-based execution** — modules grouped by `Trigger` field; separate `OnStart`, `OnVBlank`, `OnInput` sections in generated code
6. **No direct VRAM writes** — all rendering goes through `GameState` dirty flags → `Engine_Render()` → VRAM
7. **No `MaxLayers` in specs** — layer count is unbounded; VRAM budget (via `VramAllocator`) is the only real constraint
8. **No `VramRegionId`** — deprecated; use `PlaneId` referencing `TargetSpecs.Planes[]`
9. **No `PaletteSlot` on `PlaneLayerData`** — palette slot belongs to `PlaneData` (hardware plane) or `TileEntry` (per-tile override); never add it to the layer model
10. **No disk reads during codegen** — all asset data comes from `RetruxelProject` in memory; `PngToTilesTool` reads only from `inMemoryAssets`
11. **No fallback to disk in editors** — `TilemapEditor` and `SpriteEditor` require `MapIndex` to be present; missing MapIndex is shown as an error, not silently regenerated from the source PNG

## Naming Conventions

- Projects: `Retruxel.<Area>` or `Retruxel.Target.<Console>` or `Retruxel.Tool.<Name>`
- Target IDs: lowercase short strings (`"sms"`, `"nes"`, `"gg"`, `"sg1000"`, `"coleco"`)
- Module IDs: lowercase with dots for namespacing (`"text.display"`, `"text.array"`, `"custom.text"`)
- Plane IDs: lowercase short strings (`"bg"` for SMS, `"plane_a"`, `"plane_b"` for Mega Drive, `"bg1"`–`"bg4"` for SNES)
- CodeGen folders: `{moduleId}/{targetId}/` (e.g., `entity/sms/`)
- Generated C functions: `{moduleId}_{instanceId}_init()`, `{moduleId}_{instanceId}_update()`
- WPF partial classes for large windows use `_` suffix files (e.g., `TilemapEditorWindow_Canvas.cs`)
