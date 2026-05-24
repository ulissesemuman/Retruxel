# SettingsWindow Refactoring - Implementation Plan

## Status: COMPLETED ✅ (3/3 files created)

## Problem
SettingsWindow.xaml.cs has **420 lines** with multiple responsibilities:
- Window initialization and navigation
- UI population (languages, emulators, targets)
- Settings loading and saving
- Event handlers for all controls

## Solution: 3 Partial Classes (Simple Refactoring)

### 1. SettingsWindow_Main.cs - ~80 lines
**Responsibility**: Main class, fields, initialization

**Contents**:
- Private fields (_settings, _loading, _sections, _targetControls)
- Constructor
- OnLoaded method
- Window chrome (TitleBar_MouseLeftButtonDown, CloseButton_Click)
- Helper methods (SelectComboByTag, FindVisualChild, FindTextBlockIn)

### 2. SettingsWindow_UI.cs - ~200 lines
**Responsibility**: UI population and navigation

**Methods**:
- `PopulateLanguageCombo()` - Populate language dropdown
- `PopulateEmulatorSettings()` - Generate emulator settings UI
- `GetOrCreateEmulatorSettings()` - Get/create emulator settings
- `GenerateTargetSections()` - Generate target-specific sections
- `ApplySettingsToUi()` - Apply loaded settings to controls
- `ShowSection()` - Navigate between sections
- `NavGeneral_Click()`, `NavAppearance_Click()`, etc. - Navigation handlers
- `TabGeneralInterface_Click()`, `TabGeneralBehavior_Click()` - Tab switching

### 3. SettingsWindow_Events.cs - ~140 lines
**Responsibility**: Event handlers and auto-save

**Methods**:
- `CmbLanguage_Changed()` - Language selection handler
- `ChkShowWelcome_Changed()` - Show welcome checkbox
- `ChkCheckUpdates_Changed()` - Check updates checkbox
- `ChkShowMadeWithSplash_Changed()` - Made with splash checkbox (with confirmation)
- `SliderUndoHistory_Changed()` - Undo history slider
- `ChkShowWarnings_Changed()` - Show warnings checkbox
- `ChkAutoSave_Changed()` - Auto-save checkbox
- `AutoSave()` - Save settings and show confirmation

## Implementation Steps

1. ✅ Create SettingsWindow_Main.cs
2. ✅ Create SettingsWindow_UI.cs
3. ✅ Create SettingsWindow_Events.cs
4. ⏳ Rename/backup original SettingsWindow.xaml.cs
5. ⏳ Test compilation
6. ⏳ Test functionality (all settings sections)

## Benefits

- **Maintainability**: Each file has single responsibility
- **Readability**: ~80-200 lines per file vs 420 lines
- **Testability**: Easier to test individual components
- **Navigation**: Easier to find specific functionality

## File Size Comparison

| File | Lines | Responsibility |
|------|-------|----------------|
| **Before** | 420 | Everything |
| **After (total)** | ~420 | Split across 3 files |
| SettingsWindow_Main.cs | ~80 | Main + helpers |
| SettingsWindow_UI.cs | ~200 | UI population + navigation |
| SettingsWindow_Events.cs | ~140 | Event handlers + auto-save |

## Next Steps

1. ⏳ Backup original SettingsWindow.xaml.cs → SettingsWindow.xaml.cs.bak
2. ⏳ Rename SettingsWindow_Main.cs → SettingsWindow.xaml.cs
3. ⏳ Test compilation
4. ⏳ Test all functionality:
   - Language selection
   - All checkboxes
   - Undo history slider
   - Emulator settings
   - Target sections
   - Navigation between sections
   - Auto-save
5. ⏳ If successful, delete backup file

## Notes

- All partial classes use `public partial class SettingsWindow`
- No breaking changes to public API
- All methods remain private (internal implementation)
- XAML file unchanged
- Zero impact on external code

## Testing Checklist

- [ ] Compile solution without errors
- [ ] Open Settings window
- [ ] Navigate between sections (General, Appearance, Toolchain, Emulators, Targets)
- [ ] Change language and verify UI updates
- [ ] Toggle all checkboxes
- [ ] Adjust undo history slider
- [ ] Configure emulator paths
- [ ] Verify auto-save works
- [ ] Close and reopen to verify persistence

## Conclusion

This refactoring splits SettingsWindow into 3 focused partial classes, keeping the largest file at ~200 lines. Simple and maintainable structure.
