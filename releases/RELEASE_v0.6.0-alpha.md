# Retruxel v0.6.0-alpha

**Release Date:** 2025-01-XX

---

## 🎨 New Features

### Asset Importer System
- **AssetImporterWindow**: Complete visual asset import dialog with drag-drop support
  - PNG import with automatic color reduction to hardware palette
  - Real-time before/after preview comparison
  - Tile/Sprite type selection with segmented control
  - Asset name validation (no spaces, invalid characters)
  - Automatic tile count calculation
  - Target-specific palette reduction using nearest-color matching
- **AssetImporter Service**: Core import pipeline with SkiaSharp integration
  - Color reduction to hardware palette via Euclidean distance
  - Dimension validation (multiples of 8px)
  - PNG as source of truth (binary generation at build time)
  - AssetEntry model with metadata tracking

### Design System Expansion
- **SidebarTab**: Tab button style for sidebar sections (MODULES / ASSETS)
  - Active/inactive states via Tag property
  - Primary accent on active state
  - Full-width stretch layout
- **AssetImportButton**: Discrete import button style (+ IMPORT TILES / + IMPORT SPRITES)
  - Left-aligned text, minimal padding
  - Link-like appearance for secondary actions
- **SegmentedRadio**: RadioButton style with border and primary background when checked
- **RetruxelTextBox**: TextBox style alias for consistent theming
- **Font aliases**: FontSpaceGrotesk and FontInter for easier reference

---

## 🐛 Bug Fixes

### Build & Compilation
- **AssetImporterWindow.xaml**: Fixed compilation errors
  - Removed `Spacing` property from StackPanel (not supported in .NET 10)
  - Replaced with Margin on child elements
  - Fixed namespace references to `Services.AssetImporter`
- **RetruxelTheme.xaml**: Resolved duplicate ResourceDictionary closing tag warning

### Code Quality
- **BuildConsoleView**: Added `BytesPerKilobyte` constant to replace magic number 1024
- **SmsEntityCodeGen**: Function names now match SmsTarget.cs generated calls (`sms_entity_init`, `sms_entity_update`)

---

## 🔧 Improvements

### Visual Polish
- **New logo assets**: Added logo variations in `Assets/Images/Logo/`
- **Splash screen**: Removed deprecated SVG logo reference
- **Multiple view refinements**: Updated WelcomeView, SceneEditorView, AboutView, SettingsWindow, NewProjectDialog, TargetSelectionDialog

### Localization
- **Build console i18n**: Added `buildconsole.savelog.filter` and `buildconsole.savelog.filename` keys
- **Consistent translations**: Updated en.json and pt-BR.json

### Project Structure
- **AssetEntry model**: New model in Retruxel.Core for asset metadata tracking
- **FontRasterizer**: Service improvements in Retruxel.Tools
- **StartupService**: Core initialization refinements

---

## 📦 Technical Details

### Dependencies
- SkiaSharp for image processing and color reduction
- .NET 10.0-windows with WPF

### Supported Asset Types
- **Tiles**: Background tiles, UI elements (8×8px grid)
- **Sprites**: Character sprites, objects (8×8px grid)

### Color Reduction Algorithm
- Nearest-color matching using Euclidean distance in RGB space
- Target-specific hardware palette support (SMS, NES, Game Gear, etc.)
- Transparent pixel preservation

---

## 🚀 What's Next (v0.7.0)

- Asset browser in sidebar with thumbnails
- Asset deletion and renaming
- Batch import support
- Palette editor
- Tilemap editor integration

---

## 📝 Commit Summary

```
feat: add complete asset importer system with visual dialog
feat: add SidebarTab and AssetImportButton styles
feat: add SegmentedRadio and RetruxelTextBox styles
fix: resolve AssetImporterWindow compilation errors (Spacing property)
fix: add BytesPerKilobyte constant in BuildConsoleView
refactor: improve visual consistency across multiple views
chore: update version to 0.6.0-alpha
```

---

**Full Changelog**: https://github.com/your-repo/retruxel/compare/v0.5.0-alpha...v0.6.0-alpha
