# Retruxel - Product Overview

## Purpose
Retruxel is a visual IDE for developing retro games for classic 8-bit and 16-bit consoles without writing code. Inspired by GB Studio, it provides a no-code visual editor where users place modules on a canvas, configure parameters through auto-generated UI, and export ready-to-run ROM files.

## Value Proposition
- **Zero Setup**: Embedded toolchains extract automatically - no terminal, no Makefile, no manual toolchain configuration
- **Visual Development**: Build games through drag-and-drop modules instead of writing C or assembly
- **Multi-Platform**: Single project can target multiple retro consoles (SMS, NES, Game Gear, SG-1000, ColecoVision)
- **Portable Modules**: Universal modules keep projects target-agnostic for easy migration between platforms
- **One-Click Export**: Complete build pipeline from visual project to compiled ROM file

## Key Features

### Visual Game Editor
- Canvas-based scene editor for placing and configuring modules
- Auto-generated UI from module manifests - no manual UI code needed
- Real-time parameter configuration with type-safe inputs
- Event-based execution model (OnStart, OnVBlank, OnInput)

### Multi-Target Support
- **Active Development**: Sega Master System (~60%), Nintendo NES (~5%)
- **Scaffolding**: Game Gear, SG-1000, ColecoVision (~15% each)
- **Planned**: SNES
- Favorites system for filtering preferred platforms
- Dynamic manufacturer discovery

### Module System
Three module types as building blocks:
- **Graphic Modules**: Tiles, sprites, palettes, tilemaps, text display
- **Logic Modules**: Physics, input, entities, animation, scrolling
- **Audio Modules**: Music and sound effects for target sound chips

Module portability categories:
- **Universal**: Identical output on any target - fully portable
- **Base + Specialization**: Shared base with optional target-specific fields
- **Exclusive**: Target-locked with warning icon in UI

### Declarative Code Generation
- JSON-based code generator manifests (`codegen.json`)
- C template files (`.c.rtrx`) with variable substitution
- Template engine supports conditionals, loops, nested properties, arithmetic
- Tool invocation for asset conversion and preprocessing
- Generation priority: ModuleRenderer → CodeGen DLL → Target fallback → Module fallback

### Build Pipeline
Complete ROM compilation with embedded toolchains:
- **SMS/GG/SG-1000/ColecoVision**: SDCC 4.5.24 + devkitSMS + SMSlib
- **NES**: cc65 + neslib
- Real-time build console with output streaming
- Toast notifications for build status
- Hardware diagnostics panel showing VRAM/ROM usage

### State-Based Rendering Engine
- Modules populate `GameState` structure instead of direct VRAM access
- Dirty flag optimization for efficient rendering
- Prevents VRAM conflicts between modules
- Platform-agnostic architecture enables easy porting

### Developer Experience
- Emulator integration with configurable launch settings
- Multilingual interface (English, Portuguese) with runtime switching
- Plugin architecture for extensibility
- Centralized toolchain adapter
- Neo-Technical Archive design system (1980s mainframe aesthetic)

## Target Users
- Game developers wanting to create retro games without low-level programming
- Hobbyists interested in classic console development
- Educators teaching game development concepts
- Retro gaming enthusiasts porting games between platforms

## Primary Use Case
**Kung Fu Master Port**: The entire development roadmap is driven by porting Kung Fu Master from NES to Master System. Every feature must validate against this real-world complexity. Features include:
- Map offset handling (NES 30 rows vs SMS 24 rows)
- Large scrolling level support
- LiveLink integration for capturing NES graphics in real-time
- Cross-platform asset conversion

## Current Status (v0.8.0-alpha)
- ✅ Project creation and multi-target infrastructure
- ✅ Visual scene editor with canvas
- ✅ Declarative code generation system
- ✅ Event system and ROM compilation pipeline
- ✅ SMS splash screen with fade effects
- ✅ Emulator integration and favorites system
- ✅ Internationalization with runtime language switching
- 🚧 Standard modules (early stage)
- 🚧 Asset editors (planned)
- 🚧 Visual scripting system (planned)
