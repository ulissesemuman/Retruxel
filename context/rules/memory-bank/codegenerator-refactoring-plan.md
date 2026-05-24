# CodeGenerator Refactoring - Implementation Plan

## Status: COMPLETED ✅ (5/5 files created)

## Problem
CodeGenerator.cs has **619 lines** with multiple responsibilities:
- Module processing and context injection
- Batch module generation (GameVars, TextArray)
- Validation logic (tile conflicts)
- Helper methods (formatting)
- Main orchestration (GenerateAsync)

## Solution: 5 Partial Classes

### ✅ 1. CodeGenerator_Main.cs (COMPLETED) - ~350 lines
**Responsibility**: Main orchestration and GenerateAsync method

**Methods**:
- `CodeGenerator()` - Constructor
- `GenerateAsync()` - Main orchestration method
  - Module collection from scenes
  - Text analyzer integration
  - Code generation per module
  - Scene file generation
  - Build context assembly

### ✅ 2. CodeGenerator_Modules.cs (COMPLETED) - ~100 lines
**Responsibility**: Module processing and context injection

**Methods**:
- `InjectContextFlags()` - Injects context flags into modules
  - entity → usePhysics, useInput, useAnimation
  - enemy → useAnimation
  - physics → useTilemap
- `InjectFlags()` - Merges flags into JSON
- `ContextualModule` - Wrapper class for context injection

### ✅ 3. CodeGenerator_Batch.cs (COMPLETED) - ~150 lines
**Responsibility**: Batch module processing

**Methods**:
- `GenerateGameVarsFile()` - Generates gamevars.c with all variables
- `GenerateTextArrayFile()` - Generates retruxel_text.c with all text arrays
- `CalculateGraphicTilesEnd()` - Calculates first free tile slot

### ✅ 4. CodeGenerator_Validation.cs (COMPLETED) - ~50 lines
**Responsibility**: Validation logic

**Methods**:
- `ValidateTileConflicts()` - Validates tilemap vs text.display conflicts
  - Warns if tilemap startTile < 256 (overwrites ASCII font)

### ✅ 5. CodeGenerator_Helpers.cs (COMPLETED) - ~30 lines
**Responsibility**: Utility methods

**Methods**:
- `FormatByteArray()` - Formats byte array as C initializer

## Implementation Steps

1. ✅ Create CodeGenerator_Modules.cs
2. ✅ Create CodeGenerator_Batch.cs
3. ✅ Create CodeGenerator_Validation.cs
4. ✅ Create CodeGenerator_Helpers.cs
5. ✅ Create CodeGenerator_Main.cs (new main class)
6. ⏳ Rename/backup original CodeGenerator.cs
7. ⏳ Test compilation
8. ⏳ Test functionality (build project, generate code)

## Benefits

- **Maintainability**: Each file has single responsibility
- **Readability**: ~30-350 lines per file vs 619 lines
- **Testability**: Easier to test individual components
- **Extensibility**: Easy to add new validation rules or batch modules
- **Navigation**: Easier to find specific functionality

## File Size Comparison

| File | Lines | Responsibility |
|------|-------|----------------|
| **Before** | 619 | Everything |
| **After (total)** | ~680 | Split across 5 files |
| CodeGenerator_Main.cs | ~350 | Orchestration |
| CodeGenerator_Modules.cs | ~100 | Module processing |
| CodeGenerator_Batch.cs | ~150 | Batch generation |
| CodeGenerator_Validation.cs | ~50 | Validation |
| CodeGenerator_Helpers.cs | ~30 | Utilities |

## Next Steps

1. ⏳ Backup original CodeGenerator.cs → CodeGenerator.cs.bak
2. ⏳ Rename CodeGenerator_Main.cs → CodeGenerator.cs
3. ⏳ Test compilation
4. ⏳ Test all functionality:
   - Build project
   - Generate code for all module types
   - Validate batch modules (GameVars, TextArray)
   - Check validation warnings
   - Verify build diagnostics
5. ⏳ If successful, delete backup file

## Notes

- All partial classes use `public partial class CodeGenerator`
- No breaking changes to public API
- All methods remain private (internal implementation)
- Zero impact on external code
- Backward compatible with existing projects

## Architecture Improvements

### Before
```
CodeGenerator.cs (619 lines)
├── GenerateAsync() - 300 lines
├── GenerateGameVarsFile() - 80 lines
├── GenerateTextArrayFile() - 90 lines
├── ValidateTileConflicts() - 40 lines
├── InjectContextFlags() - 60 lines
├── InjectFlags() - 20 lines
├── ContextualModule - 30 lines
└── FormatByteArray() - 20 lines
```

### After
```
CodeGenerator (5 files, ~680 lines)
├── CodeGenerator_Main.cs - Orchestration
├── CodeGenerator_Modules.cs - Context injection
├── CodeGenerator_Batch.cs - Batch processing
├── CodeGenerator_Validation.cs - Validation
└── CodeGenerator_Helpers.cs - Utilities
```

## Testing Checklist

- [ ] Compile solution without errors
- [ ] Build SMS project successfully
- [ ] Build NES project successfully
- [ ] Generate GameVars file correctly
- [ ] Generate TextArray file correctly
- [ ] Validate tile conflicts warning appears
- [ ] Build diagnostics generated correctly
- [ ] All module types generate code
- [ ] Scene files generated correctly
- [ ] Main.c generated correctly

## Conclusion

This refactoring improves code organization by separating concerns into focused partial classes. Each file has a clear responsibility, making the codebase more maintainable and easier to extend with new features.
