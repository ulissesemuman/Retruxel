# Retruxel - Project Structure

## Solution Organization
Multi-project .NET 10 solution (`Retruxel.slnx`) with plugin-based architecture:

```
Retruxel/
├── Retruxel/                    # WPF Application (main shell)
├── Retruxel.Core/               # Core engine and services
├── Retruxel.SDK/                # Public plugin developer interfaces
├── Retruxel.Toolchain/          # Embedded compiler toolchains
├── Retruxel.Modules/            # Standard portable modules
├── Retruxel.Emulation/          # LibRetro emulator integration
└── Plugins/                     # Plugin ecosystem
    ├── Targets/                 # Platform implementations
    ├── Tools/                   # Asset converters and editors
    └── CodeGens/                # Declarative code generators
```

## Core Projects

### Retruxel (Main Application)
**Role**: WPF shell providing UI, navigation, and orchestration

**Key Components**:
- `Views/` - XAML views (WelcomeView, SceneEditor, BuildConsole, Settings)
- `Controls/` - Reusable WPF controls (TargetGrid, TargetSettings)
- `Services/` - UI services (StateManager, VisualToolInvoker)
- `Themes/` - Neo-Technical Archive design system
- `Assets/` - Fonts (Space Grotesk, Inter), icons, localization JSON
- `Localization/` - TrExtension for runtime language switching

**Dependencies**: References Core, Modules, and all plugin projects for build order

### Retruxel.Core
**Role**: Interfaces, models, services, and code generation engine

**Key Components**:
- `Interfaces/` - Plugin contracts (ITarget, ITool, IModule, IRenderBackend, IToolchain)
- `Models/` - Data structures (RetruxelProject, SceneData, BuildContext, ModuleManifest)
- `Services/` - Core services:
  - `CodeGenerator` - Orchestrates code generation from modules
  - `ModuleRenderer` - Template engine for `.c.rtrx` files
  - `TemplateEngine` - Variable substitution, conditionals, loops
  - `ToolExecutor` - Invokes asset conversion tools
  - `TargetRegistry` - Discovers and manages platform plugins
  - `ToolRegistry` - Discovers and manages tool plugins
  - `ModuleRegistry` - Manages module definitions
  - `ProjectManager` - Project file I/O and state management
  - `LocalizationService` - Runtime language switching
  - `ToolchainManager` - Extracts and manages embedded compilers
- `Engine/` - GameState and RenderCommandBuffer for state-based rendering
- `Connectors/` - Data flow between components (AssetToProject, PaletteToModule, etc.)

### Retruxel.SDK
**Role**: Public interfaces for third-party plugin developers

**Contents**: Minimal surface area exposing only what plugin authors need

### Retruxel.Toolchain
**Role**: Embedded compiler toolchains with centralized adapter

**Structure**:
- `Compilers/` - SDCC (Z80) and cc65 (6502) binaries
- `SDKs/` - devkitSMS, SMSlib, neslib
- `Utils/` - ihx2sms, ld65, makebin
- `Builders/` - Toolchain builders per platform (SmsToolchainBuilder, NesToolchainBuilder)
- `ToolchainAdapter` - Unified interface abstracting compiler differences
- `ToolchainOrchestrator` - Coordinates build pipeline execution

### Retruxel.Modules
**Role**: Standard portable module definitions

**Structure**:
- `Graphics/` - TilemapModule, SpriteModule, PaletteModule, TextDisplayModule, MetaspriteModule
- `Logic/` - InputModule, PhysicsModule, EntityModule, AnimationModule, ScrollModule, EnemyModule
- `Audio/` - (Planned)

Each module exposes `ModuleManifest` describing parameters for auto-generated UI.

### Retruxel.Emulation
**Role**: LibRetro core integration for in-IDE emulation

**Components**:
- `LibRetro/` - P/Invoke bindings to libretro API
- `EmulatorWindow` - WPF window hosting emulator display
- `cores/` - genesis_plus_gx_libretro.dll (SMS/GG/SG-1000)

## Plugin Architecture

### Plugins/Targets/
Platform-specific implementations as separate class libraries:

- **Retruxel.Target.SMS** - Sega Master System (~60% complete)
  - `SmsTarget` - ITarget implementation
  - `Rendering/SmsRenderBackend` - GameState → VRAM translation
  - `Modules/` - SMS-specific module implementations
  - `CodeGen/` - SMS code generation helpers
  
- **Retruxel.Target.NES** - Nintendo NES (~5% complete)
- **Retruxel.Target.GG** - Game Gear (~15% scaffolding)
- **Retruxel.Target.SG1000** - SG-1000 (~15% scaffolding)
- **Retruxel.Target.ColecoVision** - ColecoVision (~15% scaffolding)

Each target implements:
- `GetHardwarePalette()` - Available colors
- `GetToolchain()` - Compiler/assembler
- `GenerateEngineRuntime()` - engine.h/engine.c files
- `GenerateCodeForModule()` - Module → C code translation
- `GetBuildDiagnostics()` - VRAM/ROM usage stats

### Plugins/Tools/
Asset converters, preprocessors, and editors:

**Image Processing**:
- `Retruxel.Lib.ImageProcessing` - Core image manipulation library
- `Retruxel.Tool.PngToTiles` - PNG → tile data converter
- `Retruxel.Tool.TilePacker` - Tile deduplication and optimization
- `Retruxel.Tool.PixelArtEditor` - In-IDE pixel editor

**Asset Editors**:
- `Retruxel.Tool.TilemapEditor` - Visual tilemap editor
- `Retruxel.Tool.SpriteEditor` - Sprite frame editor
- `Retruxel.Tool.PaletteEditor` - Color palette editor
- `Retruxel.Tool.AudioEditor` - Sound/music editor

**Preprocessors**:
- `Retruxel.Tool.TilemapPreprocessor` - Tilemap optimization
- `Retruxel.Tool.AnimationPreprocessor` - Animation frame processing
- `Retruxel.Tool.MetaspritePreprocessor` - Metasprite compilation

**Advanced Tools**:
- `Retruxel.Tool.LiveLink` - Real-time emulator capture
- `Retruxel.Tool.AssetImporter` - Batch asset import
- `Retruxel.Tool.AutoPorting` - Cross-platform migration
- `Retruxel.Tool.BankManager` - ROM banking management
- `Retruxel.Tool.CompressionEngine` - Asset compression

Each tool implements `ITool` interface with `Execute(input) → output` method.

### Plugins/CodeGens/
Declarative code generators (JSON + templates):

```
CodeGens/
├── tilemap/
│   ├── sms/
│   │   ├── codegen.json          # Manifest
│   │   └── tilemap.c.rtrx        # C template
│   └── nes/
│       ├── codegen.json
│       └── tilemap.c.rtrx
├── sprite/
├── palette/
├── text_display/
└── main/                          # Entry point generation
```

**codegen.json** structure:
```json
{
  "moduleId": "tilemap",
  "targetId": "sms",
  "template": "tilemap.c.rtrx",
  "variables": {
    "tilemapName": { "from": "module", "path": "name" },
    "tileData": { "from": "tool", "tool": "png_to_tiles_sms" }
  }
}
```

**Template syntax** (`.c.rtrx`):
- `{{variable}}` - Variable substitution
- `{{#if condition}}` - Conditional blocks
- `{{#each array}}` - Iteration
- `{{object.property}}` - Nested access
- `{{a * b}}` - Arithmetic expressions

## Build Pipeline Flow

```
User Project (.rtrxproject)
    ↓
SceneEditor (Visual Canvas)
    ↓
Module Configuration (Auto-generated UI)
    ↓
CodeGenerator.GenerateCode()
    ↓
ModuleRenderer (JSON + .c.rtrx templates)
    ├→ Tool Invocation (asset conversion)
    └→ Variable Substitution
    ↓
Generated C/H Files
    ↓
ToolchainAdapter.Compile()
    ├→ SDCC (SMS/GG/SG-1000/ColecoVision)
    └→ cc65 (NES)
    ↓
ROM File (.sms / .nes)
    ↓
Emulator Launch (optional)
```

## Code Generation Priority

When generating code for a module, the system tries in order:

1. **ModuleRenderer** - Declarative JSON + `.c.rtrx` template (preferred)
2. **CodeGen DLL** - Legacy compiled generator plugin
3. **Target.GenerateCodeForModule()** - Target-specific fallback
4. **Module.GenerateCode()** - Module-level fallback

## Architectural Patterns

### Plugin Discovery
- Reflection-based discovery at startup
- Plugins loaded from `Plugins/Targets/` and `Plugins/Tools/`
- No manual registration required

### State-Based Rendering
- Modules populate `GameState` structure
- `Engine_Render()` translates GameState → VRAM during VBlank
- Dirty flags optimize rendering (only update changed data)
- Prevents VRAM conflicts between modules

### Event-Based Execution
- Modules assigned to events: OnStart, OnVBlank, OnInput
- CodeGenerator groups modules by trigger
- Generated main.c has separate event sections

### Zero Hardcoded Target Knowledge
- Core code never checks target IDs with switch statements
- All target-specific logic via `ITarget` interface methods
- Enables third-party target plugins without modifying Core

### Transparent Information Flow
- Targets expose data via interface methods
- Tools call interface methods to get target-specific data
- UI auto-generates from target data (e.g., settings panels)

## Directory Conventions

- `bin/Debug/` - Build output (not in source control)
- `obj/` - Intermediate build files (not in source control)
- `Assets/` - Static resources (images, fonts, localization)
- `docs/` - Architecture documentation
- `releases/` - Release notes
- `installer/` - Inno Setup installer
- `publish/` - Published build artifacts
