# TextArrayEditor Refactoring - Implementation Plan

## Status: COMPLETED ✅ (7/7 files created)

## Problem
TextArrayEditorWindow.xaml.cs has **862 lines** with multiple responsibilities:
- Window and tab management
- Array name validation
- Language management (add, select, refresh)
- Strings list management (add, remove, edit)
- Preview rendering with SkiaSharp
- Font management (categories, repository, TTF import, LiveLink)
- ASCII mapping table
- State classes and helper dialogs

## Solution: 7 Partial Classes

### 1. TextArrayEditorWindow_Main.cs - ~100 lines
**Responsibility**: Main class, initialization, fields, state classes

**Contents**:
- Private fields (_module, _projectPath, _state, _activeLanguageIndex, _selectedStringIndex)
- Constructor
- ModuleData property
- TextArrayState class
- TextLanguage class
- TextInputDialog class (helper)

### 2. TextArrayEditorWindow_Window.cs - ~80 lines
**Responsibility**: Window and tab management

**Methods**:
- `TitleBar_MouseLeftButtonDown()` - Drag window
- `BtnClose_Click()` - Close window
- `BtnCancel_Click()` - Cancel and close
- `BtnSave_Click()` - Save button handler
- `SaveAndClose()` - Validation and save logic
- `BtnTabStrings_Click()` - Switch to Strings tab
- `BtnTabFont_Click()` - Switch to Font tab
- `ActivateTab()` - Tab activation logic

### 3. TextArrayEditorWindow_ArrayName.cs - ~30 lines
**Responsibility**: Array name validation

**Methods**:
- `TxtArrayNameInput_TextChanged()` - Name input handler
- `IsValidIdentifier()` - C identifier validation

### 4. TextArrayEditorWindow_Languages.cs - ~100 lines
**Responsibility**: Language management

**Methods**:
- `RefreshLanguageTabs()` - Rebuild language tabs UI
- `SelectLanguage()` - Switch active language
- `BtnAddLanguage_Click()` - Add new language dialog

### 5. TextArrayEditorWindow_Strings.cs - ~120 lines
**Responsibility**: Strings list management

**Methods**:
- `RefreshStringsList()` - Rebuild strings list UI
- `BtnAddString_Click()` - Add empty string to all languages
- `RemoveStringAtIndex()` - Remove string with confirmation

### 6. TextArrayEditorWindow_Preview.cs - ~100 lines
**Responsibility**: Preview rendering

**Methods**:
- `RenderPreview()` - Render string preview with DefaultFont
- `ConvertSkBitmapToBitmapSource()` - SkiaSharp → WPF conversion

### 7. TextArrayEditorWindow_Font.cs - ~400 lines
**Responsibility**: Font management

**Methods**:
- `PopulateFontCategories()` - Initialize font dropdown
- `CmbFontCategory_SelectionChanged()` - Font selection handler
- `ExpandMoreFontsAsync()` - Load repository fonts
- `RenderFontPreview()` - Render built-in font preview
- `RenderRepositoryFontPreviewAsync()` - Render repository font preview
- `DrawGridOverlay()` - Draw 8x8 grid overlay
- `BtnBrowseFont_Click()` - Browse PNG font file
- `BtnImportFromTtf_Click()` - Import TTF font
- `BtnLiveLink_Click()` - Capture font from emulator
- `PopulateAsciiMap()` - Populate ASCII mapping table
- `FontCategoryItem` - Helper class
- `AsciiMapItem` - Helper class

## Implementation Steps

1. ✅ Create TextArrayEditorWindow_Main.cs
2. ✅ Create TextArrayEditorWindow_Window.cs
3. ✅ Create TextArrayEditorWindow_ArrayName.cs
4. ✅ Create TextArrayEditorWindow_Languages.cs
5. ✅ Create TextArrayEditorWindow_Strings.cs
6. ✅ Create TextArrayEditorWindow_Preview.cs
7. ✅ Create TextArrayEditorWindow_Font.cs
8. ⏳ Rename/backup original TextArrayEditorWindow.xaml.cs
9. ⏳ Test compilation
10. ⏳ Test functionality (strings, languages, fonts, preview)

## Benefits

- **Maintainability**: Each file has single responsibility
- **Readability**: ~30-400 lines per file vs 862 lines
- **Testability**: Easier to test individual components
- **Navigation**: Easier to find specific functionality
- **Extensibility**: Easy to add new font sources or preview modes

## File Size Comparison

| File | Lines | Responsibility |
|------|-------|----------------|
| **Before** | 862 | Everything |
| **After (total)** | ~930 | Split across 7 files |
| TextArrayEditorWindow_Main.cs | ~100 | Main class + state |
| TextArrayEditorWindow_Window.cs | ~80 | Window + tabs |
| TextArrayEditorWindow_ArrayName.cs | ~30 | Name validation |
| TextArrayEditorWindow_Languages.cs | ~100 | Languages |
| TextArrayEditorWindow_Strings.cs | ~120 | Strings list |
| TextArrayEditorWindow_Preview.cs | ~100 | Preview rendering |
| TextArrayEditorWindow_Font.cs | ~400 | Font management |

## Next Steps

1. ⏳ Backup original TextArrayEditorWindow.xaml.cs → TextArrayEditorWindow.xaml.cs.bak
2. ⏳ Rename TextArrayEditorWindow_Main.cs → TextArrayEditorWindow.xaml.cs
3. ⏳ Test compilation
4. ⏳ Test all functionality:
   - Array name validation
   - Add/remove languages
   - Add/remove strings
   - String preview rendering
   - Font category selection
   - Repository font loading
   - TTF import
   - LiveLink capture
   - ASCII map display
   - Save/cancel
5. ⏳ If successful, delete backup file

## Notes

- All partial classes use `public partial class TextArrayEditorWindow`
- No breaking changes to public API
- All methods remain private (internal implementation)
- XAML file unchanged
- Zero impact on external code
- TextInputDialog remains in same file (helper class)

## Architecture Improvements

### Before
```
TextArrayEditorWindow.xaml.cs (862 lines)
├── Constructor + fields - 40 lines
├── Window Management - 50 lines
├── Tab Management - 30 lines
├── Array Name - 30 lines
├── Language Management - 80 lines
├── Strings Management - 100 lines
├── Preview - 80 lines
├── Font Management - 350 lines
├── ASCII Map - 40 lines
├── State Classes - 30 lines
└── TextInputDialog - 60 lines
```

### After
```
TextArrayEditorWindow (7 files, ~930 lines)
├── TextArrayEditorWindow_Main.cs - Main + state
├── TextArrayEditorWindow_Window.cs - Window + tabs
├── TextArrayEditorWindow_ArrayName.cs - Name validation
├── TextArrayEditorWindow_Languages.cs - Languages
├── TextArrayEditorWindow_Strings.cs - Strings list
├── TextArrayEditorWindow_Preview.cs - Preview
└── TextArrayEditorWindow_Font.cs - Font management
```

## Testing Checklist

- [ ] Compile solution without errors
- [ ] Open TextArrayEditor from module
- [ ] Change array name (valid/invalid)
- [ ] Add new language
- [ ] Switch between languages
- [ ] Add string to all languages
- [ ] Remove string from all languages
- [ ] Edit string and see preview update
- [ ] Select font category
- [ ] Load repository fonts
- [ ] Import TTF font
- [ ] Capture font via LiveLink
- [ ] View ASCII mapping table
- [ ] Save and verify ModuleData
- [ ] Cancel without saving

## Conclusion

This refactoring improves code organization by separating concerns into focused partial classes. The largest file (Font management) is isolated at 400 lines, making the codebase more maintainable and easier to extend with new font sources or preview modes.
