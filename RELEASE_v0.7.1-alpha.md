# Release Notes — v0.7.1-alpha

**Release Date:** 2025-01-XX  
**Build:** 0.7.1-alpha

---

## 🎯 Overview

Hotfix release to resolve GitHub Actions workflow issues and improve CI/CD pipeline reliability.

---

## 🐛 Bug Fixes

### CI/CD Pipeline
- **Fixed GitHub Actions permissions** - Added `contents: write` permission for release creation
- **Fixed workflow trigger** - Changed from branch push to tag-based releases
- **Fixed installer path** - Corrected output path for Inno Setup installer
- **Fixed [Run] section syntax** - Removed invalid `Name:` parameter in installer.iss

### Build System
- **Added file verification step** - Validates that release artifacts exist before upload
- **Improved error handling** - Better logging for troubleshooting build issues

---

## 📚 Documentation

- **Updated README screenshot** - Now uses `docs/builder.png` instead of missing screenshot
- **Organized release notes** - Moved historical releases to `releases/` folder

---

## 🔧 Maintenance

- **Cleaned up .gitignore** - Removed `[Rr]eleases/` pattern to allow release notes folder
- **Removed obsolete files** - Deleted backup files and unused scripts

---

## 📦 Installation

**Installer (Recommended):**
1. Download `Retruxel-0.7.1-alpha-setup.exe`
2. Run the installer
3. Follow the installation wizard

**Portable Version (ZIP):**
1. Download `Retruxel-0.7.1-alpha-portable.zip`
2. Extract to any folder
3. Run `Retruxel.exe`

The toolchain will be extracted automatically on first run to `%AppData%\Retruxel\toolchain\`

---

## 🎯 Supported Targets

- 🟢 Sega Master System (Active)
- 🟢 Nintendo NES (Active)
- 🟡 Sega Game Gear (Scaffolding)
- 🟡 Sega SG-1000 (Scaffolding)
- 🟡 ColecoVision (Scaffolding)

---

## ✨ Features from v0.7.0-alpha

This release includes all features from v0.7.0-alpha:

### Undo/Redo System
- Full undo/redo support in Scene Editor
- Keyboard shortcuts: `Ctrl+Z` (Undo) and `Ctrl+Y` (Redo)
- Visual feedback in title bar
- Support for element creation, deletion, property changes, and position changes

### Build Console Enhancements
- Total Assets counter showing number of elements in project
- Elapsed Time display showing build duration
- Real-time build statistics after successful compilation

### UI/UX Improvements
- New theme styles: SidebarTab, AssetImportButton, SegmentedRadio
- MainWindow logo replaced with image
- WelcomeView sidebar reorganized with Grid layout
- Documentation button added to Scene Editor sidebar

### Module System Fixes
- TextDisplayModule now correctly implements IGraphicModule
- Fixed duplicate entity.c generation in SMS target
- Proper module interface separation

---

## 📝 Commit Summary

```
chore: bump version to 0.7.1-alpha
ci: add file verification step before release creation
ci: fix GitHub Actions permissions and workflow trigger
fix: correct [Run] section syntax in installer.iss
fix: correct installer icon path
docs: update screenshot to use builder.png
chore: organize release notes into releases/ folder
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

**Full Changelog:** https://github.com/ulissesemuman/Retruxel/compare/v0.6.0-alpha...v0.7.1-alpha
