# Palette Architecture Refactoring

## Summary

The palette system has been completely refactored from standalone draggable modules to scene-level fixed slots.

## Before (Old Architecture)

```
PaletteModule (draggable) → palette/sms/codegen.json → palette_0.c / palette_0.h
scene_main_init() calls palette_0_init()
```

**Problems:**
- User had to manually drag palette modules to scenes
- Generated separate files per palette (palette_0.c, palette_0.h)
- Extra includes and function calls
- Confusing UX: "Why do I need to drag a palette?"
- Palette was treated as game logic, not hardware configuration

## After (New Architecture)

```
SceneData.PaletteSlots → scene/sms/codegen.json → scene_main.c
scene_main_init() loads palette directly via SMS_loadBGPalette() / SMS_loadSpritePalette()
```

**Benefits:**
- Palettes are scene properties, not draggable modules
- Automatically created when scene is created (based on target specs)
- No separate palette files - colors embedded in scene_main.c as static arrays
- Cleaner for compiler: fewer files, fewer includes, less overhead
- Better UX: palette slots visible in Structure panel, no dragging needed

## Implementation Details

### Scene-Level Palette Slots

Each scene has `PaletteSlots` property:

```csharp
public class SceneData
{
    public List<PaletteSlotData> PaletteSlots { get; set; } = new();
}

public class PaletteSlotData
{
    public int SlotIndex { get; set; }
    public List<string> Colors { get; set; } = new(); // Hex colors: "#FF0000"
    public string Label { get; set; } = "";
}
```

### Target-Defined Slot Count

Each target defines how many palette slots it supports:

```csharp
public interface ITarget
{
    int GetPaletteSlotCount(); // SMS: 2 (BG + Sprite)
    int GetColorsPerSlot();    // SMS: 16 colors per slot
    PaletteSlotType GetPaletteSlotType(int slotIndex); // Background, Sprite, Shared
}
```

### Scene CodeGen Integration

The scene codegen (`scene/sms/codegen.json`) reads palette slots directly:

```json
{
  "variables": {
    "palette0Hex": {
      "from": "scenePaletteSlot",
      "slotIndex": 0
    },
    "palette1Hex": {
      "from": "scenePaletteSlot",
      "slotIndex": 1
    }
  }
}
```

Template (`scene/sms/scene.c.rtrx`) generates inline arrays:

```c
// Palette slot 0 (Background) - 16 colors (RGB222 format for SMS)
static const unsigned char scene_main_palette_0[16] = {
    {{palette0Hex}}
};

// Palette slot 1 (Sprite) - 16 colors
static const unsigned char scene_main_palette_1[16] = {
    {{palette1Hex}}
};

void scene_main_init(void) {
    // Load palette slots into CRAM
    SMS_loadBGPalette(scene_main_palette_0);
    SMS_loadSpritePalette(scene_main_palette_1);
    
    // ... rest of scene init
}
```

### Module Palette Slot Selection

Modules (tilemap, sprite) select which slot to use:

```csharp
public class TilemapModule
{
    private TilemapState _state = new();
    
    private class TilemapState
    {
        public int PaletteSlot { get; set; } = 0; // 0 = Background, 1 = Sprite
    }
}
```

SMS preprocessor applies bit 11 to nametable entries:

```csharp
public class SmsTilemapPreprocessorExtension : IToolExtension
{
    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var paletteSlot = (int)input["paletteSlot"];
        var nametable = (ushort[])input["nametable"];
        
        for (int i = 0; i < nametable.Length; i++)
        {
            if (paletteSlot == 1)
                nametable[i] |= 0x0800; // Set bit 11 for sprite palette
        }
        
        return new Dictionary<string, object> { ["nametable"] = nametable };
    }
}
```

## Obsolete Components

### PaletteModule (Deprecated)

- Marked with `[Obsolete]` attribute
- Display name changed to "Palette (Obsolete)"
- Removed from module palette (filtered out in `SceneEditorView_ModulePalette.cs`)
- Kept for backward compatibility with old projects only
- No longer generates code

### Palette CodeGen (Disabled)

- Folder renamed: `Plugins/CodeGens/palette` → `Plugins/CodeGens/palette.obsolete`
- No longer discovered by `ModuleRenderer`
- Kept for reference only

## Future: PaletteEffectModule

For runtime palette effects (flash, fade, cycle, pulse), a new draggable module will be added:

```csharp
public class PaletteEffectModule : ILogicModule
{
    public string ModuleId => "palette.effect";
    public string DisplayName => "Palette Effect";
    public ModuleScope DefaultScope => ModuleScope.Project;
}
```

**Why draggable?**
- Represents game logic, not hardware configuration
- Multiple effects can exist (flash on damage, fade on transition, cycle for water)
- Needs to be placed in OnVBlank or triggered by events

**Stub created at:** `Retruxel.Modules/Graphics/PaletteEffectModule.cs`

## Migration Guide

### For Old Projects

Old projects with `PaletteModule` instances will continue to work but won't generate code. To migrate:

1. Open project in new version
2. Note the colors in existing palette modules
3. Delete palette modules from scenes
4. Edit scene palette slots in Structure panel with same colors
5. Save project

### For New Projects

1. Create scene - palette slots are automatically initialized
2. Click "EDIT" button next to palette slot in Structure panel
3. Choose colors using PaletteEditorWindow
4. Modules (tilemap, sprite) select which slot to use via dropdown

## Technical Notes

### Color Conversion

Palette colors are stored as hex strings in `SceneData.PaletteSlots`:

```json
{
  "paletteSlots": [
    {
      "slotIndex": 0,
      "colors": ["#000000", "#FF0000", "#00FF00", ...],
      "label": "Background"
    }
  ]
}
```

During code generation, `SmsPaletteConverter` converts to hardware format:

```csharp
public class SmsPaletteConverter : IPaletteConverter
{
    public byte[] ConvertColors(IEnumerable<string> hexColors)
    {
        return hexColors
            .Select(hex => SmsColorUtils.ConvertToSmsRgb222(hex))
            .ToArray();
    }
}
```

### Nametable Bit 11

SMS nametable entries are 16-bit:

```
Bits 0-8:   Tile index (0-511)
Bit 9:      Horizontal flip
Bit 10:     Vertical flip
Bit 11:     Palette select (0 = BG, 1 = Sprite)
Bits 12-15: Priority (unused in most games)
```

The preprocessor automatically sets bit 11 based on module's `PaletteSlot` property.

## Files Changed

### Core Models
- `Retruxel.Core/Models/SceneData.cs` - Added `PaletteSlots` property
- `Retruxel.Core/Models/PaletteSlotData.cs` - New model
- `Retruxel.Core/Models/PaletteSlotType.cs` - New enum

### Interfaces
- `Retruxel.Core/Interfaces/ITarget.cs` - Added palette slot methods
- `Retruxel.Core/Interfaces/IPaletteConverter.cs` - New interface

### Modules
- `Retruxel.Modules/Graphics/PaletteModule.cs` - Marked obsolete
- `Retruxel.Modules/Graphics/PaletteEffectModule.cs` - New stub
- `Retruxel.Modules/Graphics/TilemapModule.cs` - Added `PaletteSlot` property
- `Retruxel.Modules/Graphics/SpriteModule.cs` - Added `PaletteSlot` property

### Target Implementations
- `Retruxel.Target.SMS/SmsTarget.cs` - Implemented palette slot methods
- `Retruxel.Target.SMS/Palette/SmsPaletteConverter.cs` - New converter
- `Retruxel.Target.SMS/Tools/SmsTilemapPreprocessorExtension.cs` - New preprocessor

### CodeGens
- `Plugins/CodeGens/scene/sms/codegen.json` - Added palette slot variables
- `Plugins/CodeGens/scene/sms/scene.c.rtrx` - Generates inline palette arrays
- `Plugins/CodeGens/palette/` → `Plugins/CodeGens/palette.obsolete/` - Disabled

### UI
- `Retruxel/Views/SceneEditor/SceneEditorView_Structure.cs` - Palette slot UI
- `Retruxel/Views/SceneEditor/SceneEditorView_Scenes.cs` - Initialize slots on scene creation
- `Retruxel/Views/SceneEditor/SceneEditorView_ModulePalette.cs` - Filter out obsolete palette
- `Retruxel.Tool.PaletteEditor/PaletteEditorWindow.xaml.cs` - Added slot editing constructor
- `Retruxel.Tool.TilemapEditor/TilemapEditorWindow_PaletteSlot.cs` - Slot selector

### Services
- `Retruxel.Core/Services/ModuleRenderer.cs` - Added `scenePaletteSlot` resolution
- `Retruxel.Core/Services/VariableResolver.cs` - Palette slot variable source

## Conclusion

This refactoring simplifies the palette system by treating palettes as what they are: hardware configuration, not game logic. The new architecture is cleaner, more intuitive, and generates more efficient code.
