# Release Notes — v0.7.0-alpha

**Release Date:** 2025-01-XX  
**Build:** 0.7.0-alpha

---

## 🎯 Overview

This release introduces the **Undo/Redo system** for the Scene Editor, fixes critical module interface issues, improves the Build Console with build statistics, and enhances the overall UI/UX with new design elements.

---

## ✨ New Features

### Undo/Redo System
- **Full undo/redo support** in Scene Editor for element operations
- Keyboard shortcuts: `Ctrl+Z` (Undo) and `Ctrl+Y` (Redo)
- Visual feedback in title bar showing undo/redo availability
- Command pattern implementation with `IUndoableCommand` interface
- Support for:
  - Element creation and deletion
  - Property changes
  - Position changes

### Build Console Enhancements
- **Total Assets counter** - Shows number of elements in the project
- **Elapsed Time display** - Shows build duration in seconds
- Real-time build statistics after successful compilation

### Scene Editor Improvements
- **Documentation button** added to sidebar with separator
- Improved sidebar layout with proper spacing
- Better visual hierarchy with separators

---

## 🐛 Bug Fixes

### Module System
- **Fixed TextDisplayModule interface** - Now correctly implements `IGraphicModule` instead of `ILogicModule`
- **Fixed duplicate entity.c generation** - SMS target now generates entity files only once when multiple entity modules exist
- Corrected module registration in BuildConsoleView

### Code Generation
- **SMS Target:** Prevented duplicate code generation for `sms.entity`, `sms.scroll`, and `sms.enemy` modules
- Entity files (entity.c/entity.h) now generated once in `GenerateMainFile` using first instance's ModuleState

---

## 🎨 UI/UX Improvements

### Theme System
- **New styles added:**
  - `SidebarTab` - Sidebar section tabs with active/inactive states
  - `AssetImportButton` - Discrete import buttons with left-aligned text
  - `SegmentedRadio` - RadioButton with border and primary background when checked
  - `RetruxelTextBox` - TextBox style alias
  - Font aliases: `FontSpaceGrotesk` and `FontInter`

### MainWindow
- **Logo image** replaces text "RETRUXEL" in title bar
- High-quality bitmap scaling for crisp rendering at 20px height

### WelcomeView
- **Sidebar reorganization** using Grid layout
- Documentation and About buttons restructured with icon/text separation
- New Project/Open Project buttons fixed at bottom
- Improved visual consistency

### BuildConsoleView
- Build statistics displayed in separate panels
- Consistent layout with other stat panels (Memory, Verification)
- Values displayed below labels for better readability

---

## 🌍 Localization

### New Translation Keys
- `buildconsole.total_assets` - "TOTAL ASSETS" / "TOTAL DE ASSETS"
- `buildconsole.elapsed_time` - "ELAPSED TIME" / "TEMPO DECORRIDO"
- `scene.documentation` - "Documentation" / "Documentação"

---

## 🏗️ Architecture Changes

### New Core Components
- `IUndoableCommand` interface for command pattern
- `UndoRedoStack` service for managing command history
- Command implementations:
  - `AddElementCommand`
  - `DeleteElementCommand`
  - `ChangePropertyCommand`
  - `MoveElementCommand`

### Module Interface Corrections
- `IModule` - Base interface for all modules
- `IGraphicModule` - For modules that generate visual assets
- `ILogicModule` - For modules that generate game logic
- Proper separation of concerns between graphic and logic modules

---

## 📦 Technical Details

### Version Information
- **Version:** 0.7.0-alpha
- **Assembly Version:** 0.7.0.0
- **Target Framework:** .NET 10.0
- **Installer Version:** 0.7.0-alpha

### Files Changed
- 34 files modified
- 4 new files added (Undo/Redo system)
- 3 files deleted (cleanup)

---

## 🔧 Developer Notes

### Breaking Changes
- None

### Deprecations
- None

### Known Issues
- Undo/Redo does not yet support scene switching
- Some module property changes may not be fully undoable
- Build statistics only show after successful builds

---

## 📝 Commit Summary

```
feat: add undo/redo system to Scene Editor with Ctrl+Z/Ctrl+Y support
fix: correct TextDisplayModule interface from ILogicModule to IGraphicModule
fix: prevent duplicate entity.c generation in SMS target
feat: add build statistics (total assets and elapsed time) to Build Console
ui: add documentation button to Scene Editor sidebar with separators
ui: replace MainWindow text logo with image logo
ui: reorganize WelcomeView sidebar with Grid layout
style: add new theme styles (SidebarTab, AssetImportButton, SegmentedRadio)
i18n: add translation keys for build statistics and documentation
chore: bump version to 0.7.0-alpha
```

---

## 🚀 What's Next (v0.8.0)

- Asset editors (Sprite Editor, Tilemap Editor)
- Plugin system implementation
- Additional modules (Audio, Advanced Physics)
- Performance optimizations
- Extended undo/redo support for all operations

---

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

---

**Full Changelog:** https://github.com/ulissesemuman/Retruxel/compare/v0.6.0-alpha...v0.7.0-alpha
