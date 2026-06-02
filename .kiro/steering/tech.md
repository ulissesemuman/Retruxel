# Retruxel — Tech Stack

## Platform & Runtime

- **.NET 10** (C# 13)
- **WPF** (Windows only) — `net10.0-windows`
- **Nullable reference types** enabled across all projects
- Solution file: `Retruxel.slnx` (new VS solution format)

## Key Libraries

| Library | Usage |
|---|---|
| **SkiaSharp 3.116.1** | Image processing, tile rendering, palette operations |
| **SMSlib / devkitSMS** | SMS/GG/SG-1000/ColecoVision runtime library (embedded toolchain) |
| **SDCC 4.5.24** | C compiler for SMS/GG/SG-1000/ColecoVision targets |
| **cc65 / ld65** | C compiler/linker for NES target |
| **neslib** | NES runtime library (embedded toolchain) |

## Embedded Toolchains

Toolchains are bundled with the app and extracted automatically on first run to `%AppData%\Retruxel\toolchain\`. No manual setup required.

## Build System

Standard MSBuild via Visual Studio 2022+. The main project (`Retruxel.csproj`) has custom post-build targets that:
- Copy tool DLLs → `bin/Plugins/Tools/`
- Copy target DLLs → `bin/Plugins/Targets/`
- Copy CodeGen folders → `bin/Plugins/CodeGens/`
- Copy module DLL → `bin/modules/`

## Common Commands

```powershell
# Build the full solution (Visual Studio recommended)
# Open Retruxel.slnx in Visual Studio 2022+, then Build → Build Solution

# Build from CLI (Debug)
dotnet build Retruxel.slnx

# Build from CLI (Release)
dotnet build Retruxel.slnx -c Release

# Run the app
dotnet run --project Retruxel/Retruxel.csproj

# Publish (self-contained)
dotnet publish Retruxel/Retruxel.csproj -c Release -o publish/
```

## Localization

Language files are JSON in `Retruxel/Assets/Localization/`. Supported: English (`en.json`), Português Brasil (`pt-BR.json`). New languages are auto-discovered at startup — no code changes needed.

## Plugin Discovery

Plugins (targets, tools, CodeGens) are discovered at runtime via reflection. The app scans:
- `Plugins/Tools/*.dll` — implements `ITool`
- `Plugins/Targets/*.dll` — implements `ITarget`
- `Plugins/CodeGens/**/codegen.json` — declarative code generators

## Code Generation Pipeline Priority

When generating code for a module, the system tries in order:
1. **ModuleRenderer** (declarative JSON + `.c.rtrx` templates) ← preferred
2. **CodeGen DLL** (`ICodeGenPlugin`) — legacy, being phased out
3. **`ITarget.GenerateCodeForModule()`** — target-specific fallback
4. **`IModule.GenerateCode()`** — module-level fallback

## WPF Pitfalls & Constraints

- **`StackPanel.Spacing` does not exist in WPF** — it is a WinUI/MAUI property. Use `Margin` on child elements instead (e.g., `Margin="0,0,8,0"` on all but the last child).
- **Named `TextBox` styles** — the theme defines an implicit style for `TargetType="TextBox"` (no `x:Key`). There is no `"RetruxelTextBox"` key; referencing it in XAML will silently fall back to default. Use the implicit style (no `Style=` attribute needed) or set `Style="{StaticResource {x:Type TextBox}}"`.
- **`BrushSurfaceDim` / `BrushSurfaceContainerLowest`** — these do **not** exist in `RetruxelTheme.xaml`. Use `BrushSurface` (`#0e0e0e`) as the darkest available surface. Available surface brushes: `BrushSurface`, `BrushSurfaceContainerLow`, `BrushSurfaceContainerHigh`, `BrushSurfaceContainerHighest`.
- **`CornerRadius` rule** — 0px everywhere except the outer `Border` of modal `Window` dialogs (use `CornerRadius="6"` there for the physical window frame only).

## Asset Pipeline Rule

**Retruxel never works with PNG at runtime.** PNG is import-only. The canonical asset form is `MapIndex` (`byte[]` of palette indices) stored in `AssetGenerationParams`. Any code that loads a PNG file for rendering is wrong. The `.rtrxproject` file on disk is persistence-only — the `RetruxelProject` object in memory is the single source of truth during a session. No service, tool, or codegen may read the project file from disk; only `ProjectManager` loads it at startup.

## VRAM Allocation

VRAM is managed by `VramAllocator` (Retruxel.Core/Services/). Key concepts:

- `TargetSpecs.Planes[]` — array of `PlaneSpecs`, one per hardware BG plane. Each plane has its own `BitsPerPixel`, `BytesPerTile`, palette mode, and default/max dimensions.
- `TargetSpecs.VramBytesForTiles` — total usable VRAM for tile data (after subtracting Name Table, SAT, and other fixed structures).
- `PlaneSpecs.BytesPerTile` — derived from `BitsPerPixel`; used by the allocator to calculate cost per tile.
- Layers per plane are **unbounded** — the user adds as many as needed. VRAM budget is the only real constraint, surfaced via `VramUsageReport`.
- `VramRegionId` is **deprecated and must not be used** — replaced by `PlaneId` + `VramAllocator`.

## In-Memory Asset Registry (CodeGen)

Before code generation starts, `CodeGenerator.GenerateAsync` populates `inMemoryAssets` with **all** `project.Assets`. This dict is passed to `ModuleRenderer` → `VariableResolver`, which injects `inMemoryMapIndex`, `inMemoryWidth`, `inMemoryHeight`, `inMemoryTileCount` into tool inputs. Tools (e.g. `PngToTilesTool`) must read exclusively from this in-memory registry — never from disk.

## Input System

Input ports are defined by the target via `ITarget.GetInputPorts()` and stored in `RetruxelProject.InputPorts` as `InputPortBinding[]` (remappable). `EntityData.InputSlot` holds the index into this array (-1 = no input). The `InputButton.DevkitConst` field is the C constant emitted by CodeGen (e.g. `PORT_A_KEY_1`).
