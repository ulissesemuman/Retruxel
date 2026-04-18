# Release Notes — v0.7.2-alpha

**Release Date:** 2025-01-XX  
**Build:** 0.7.2-alpha

---

## 🎯 Overview

Major refactoring release focusing on plugin architecture reorganization, declarative code generation improvements, and comprehensive documentation updates.

---

## 🏗️ Architecture Refactoring

### Plugin System Reorganization
- **Centralized plugin structure** - All pluggable components now under `Plugins/` directory
  - `Plugins/CodeGens/` - Declarative code generators (JSON + `.c.rtrx` templates)
  - `Plugins/Tools/` - Tool implementations (asset converters, preprocessors)
  - `Plugins/Targets/` - Platform-specific implementations
- **Moved 789 files** from `Tools/` to `Plugins/Tools/`
- **Moved 249 files** from `Targets/` to `Plugins/Targets/`
- **Updated all project references** in `.csproj` files and solution

### Declarative Code Generation Enhancements
- **ModuleRenderer improvements** - Full tool invocation system with JSON type parsing
- **TemplateEngine expansion** - Advanced pattern support:
  - `{{#ifnot condition}}` - Negated conditionals
  - `{{object.property}}` - Nested property access
  - `{{a * b}}` - Arithmetic operations (*, /, +, -)
  - `{{#if a > b}}` - Comparison operators (>, <, >=, <=, ==, !=)
  - Literal numeric values in expressions
- **Tool output structure** - Nested object storage for template access via `{{preprocessed.collisionHex}}`
- **Built-in tool registration** - TilemapPreprocessorTool and AnimationPreprocessorTool registered directly

### Toolchain Improvements
- **Centralized ToolchainAdapter** - Single generic adapter for all targets
- **SDCC cc1.exe fix** - Added SDCC bin directory to process PATH for preprocessor execution
- **Removed duplicate adapters** - Deleted 5 target-specific adapters in favor of centralized solution

---

## 📚 Documentation

### README.md Complete Overhaul
- **Added Progress column** to Supported Targets table:
  - SMS: ~60% (implemented but needs extensive testing)
  - GG/SG-1000/ColecoVision: ~15% (scaffolding with initial tests)
  - NES: ~5% (minimal implementation)
- **Clarified module system** - Standard modules vs console-specific modules
- **Updated Architecture section** - Reflects new plugin-based structure
- **Revised Build Pipeline** - Shows ModuleRenderer flow and generation priority
- **Expanded Current Status** - Added recent features (declarative codegen, tool system, splash screen, etc.)
- **Rewrote Writing a Plugin section** - Three plugin types with examples:
  1. Declarative Code Generators (JSON + templates)
  2. Tools (ITool implementations)
  3. Target Platforms (ITarget implementations)

---

## 🔧 Technical Improvements

### Module System
- **Restored TilemapModule fields** - Added missing `MapData` and `SolidTiles` properties
- **Extended ParameterType enum** - Added `IntArray` and `StringArray` types
- **ITool interface updates** - Changed `IconPath` to `Icon` (object type), added `IsStandalone` property

### Code Generation
- **ITarget.GenerateSystemFiles()** - New method for splash screens and boot code
- **SmsSplashCodeGen fixes** - Proper tile indexing, nametable loading, palette generation
- **Generation priority** - ModuleRenderer → CodeGen DLL → Target fallback → Module fallback

---

## 🐛 Bug Fixes

### Build System
- **Fixed splash screen tile indexing** - Blank tile at index 0, image tiles start at 1
- **Fixed nametable loading** - Line-by-line rendering to prevent displacement
- **Fixed palette array generation** - Proper RGB222 format for SMS
- **Increased splash hold time** - From 60 to 120 frames (2 seconds)

### UI/UX
- **Wizard button integration** - Added 🧙 button to MainWindow title bar
- **Removed duplicate wizard button** - Cleaned up BuildConsoleView

---

## 🧹 Maintenance

### Project Cleanup
- **Added `*.ps1` to .gitignore** - PowerShell maintenance scripts now local-only
- **Removed `zip_ps7.ps1`** - Deleted from repository
- **Removed leftover folders** - Cleaned up empty `Tools/` directory after reorganization

### Code Quality
- **Fixed line endings** - Normalized CRLF for all moved files
- **Updated solution file** - All paths reflect new `Plugins/` structure
- **Null checks added** - BuildConsoleView hash copy methods

---

## 📦 Installation

**Installer (Recommended):**
1. Download `Retruxel-0.7.2-alpha-setup.exe`
2. Run the installer
3. Follow the installation wizard

**Portable Version (ZIP):**
1. Download `Retruxel-0.7.2-alpha-portable.zip`
2. Extract to any folder
3. Run `Retruxel.exe`

The toolchain will be extracted automatically on first run to `%AppData%\Retruxel\toolchain\`

---

## 🎯 Supported Targets

| Console | Status | Progress | Toolchain |
|---|---|---|---|
| Sega Master System | 🟢 Active | ~60% | SDCC 4.5.24 + devkitSMS + SMSlib |
| Sega Game Gear | 🟡 Scaffolding | ~15% | SDCC 4.5.24 + devkitSMS + SMSlib |
| Sega SG-1000 | 🟡 Scaffolding | ~15% | SDCC 4.5.24 + devkitSMS + SMSlib |
| ColecoVision | 🟡 Scaffolding | ~15% | SDCC 4.5.24 + devkitSMS + SMSlib |
| Nintendo NES | 🟢 Active | ~5% | cc65 + neslib |
| SNES | 🔮 Planned | 0% | — |

---

## ✨ Key Features

### From Previous Releases
- ✅ Project creation and management
- ✅ Multi-target infrastructure (6 platforms)
- ✅ Visual scene editor with canvas
- ✅ Undo/Redo system with keyboard shortcuts
- ✅ Event system (OnStart, OnVBlank, OnInput)
- ✅ ROM compilation pipeline (SMS + NES)
- ✅ SMS splash screen with fade effects
- ✅ Build console with real-time output
- ✅ Emulator integration
- ✅ Favorites system
- ✅ Internationalization (i18n) with runtime switching
- ✅ Dynamic manufacturer discovery

### New in v0.7.2-alpha
- ✅ Declarative code generation system (JSON + `.c.rtrx` templates)
- ✅ Tool system with preprocessors and asset converters
- ✅ ModuleRenderer with advanced template engine
- ✅ Centralized toolchain adapter
- ✅ Reorganized plugin architecture
- ✅ Comprehensive documentation updates

---

## 🚀 What's Next (v0.8.0-alpha)

Based on the [Retruxel Roadmap](docs/Retruxel_Roadmap.md), v0.8.0 will focus on completing **Phase 1**:

### Priority Features
- **Asset deduplication** - Automatic removal of duplicate assets
- **Build incremental** - Recompile only changed modules
- **Hardware constraint validation** - Real-time VRAM/sprite/palette limits in editor
- **Asset Manager** - Centralized asset management system
- **Tilemap Editor (basic)** - Grid, layers, collision editing
- **Testing and stabilization** - Extensive testing across all targets

---

## 📝 Commit Summary

```
refactor: reorganize project structure and update documentation
- Moved Tools/ to Plugins/Tools/ (789 files)
- Moved Targets/ to Plugins/Targets/ (249 files)
- Updated README with progress percentages and new architecture
- Added *.ps1 to .gitignore
- Removed zip_ps7.ps1 from repository
- Fixed line endings for moved files
```

---

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

---

**Full Changelog:** https://github.com/ulissesemuman/Retruxel/compare/v0.7.1-alpha...v0.7.2-alpha
