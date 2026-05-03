# Retruxel Engine Architecture - Implementation Status

## ✅ Completed (Phase 1)

### Core Infrastructure (Platform-Agnostic)

**Location:** `Retruxel.Core/`

1. **Interfaces/IRenderBackend.cs**
   - Platform-agnostic render backend interface
   - Defines contract for all target implementations
   - Methods: Initialize, DrawTilemap, DrawText, DrawSprite, SetScroll, LoadPalette, ClearScreen, ExecuteBuffer
   - RenderBackendConstraints for platform limits

2. **Engine/GameState.cs**
   - Central game state structure (single source of truth)
   - Layer states: TilemapLayerState, TextLayerState, SpriteState, SpriteLayerState
   - Dirty flags for optimization
   - Global state (scroll, etc.)

3. **Engine/RenderCommandBuffer.cs**
   - Platform-agnostic render command types
   - RenderCommand and RenderCommandBuffer classes
   - Command data structures for each render operation

### SMS Target Implementation

**Location:** `Retruxel.Target.SMS/Rendering/`

4. **SmsRenderBackend.cs**
   - SMS-specific backend implementation
   - Generates engine.h and engine.c for SMS runtime
   - Translates GameState → SMSlib calls
   - Hardware constraints (64 sprites, 448 tiles, etc.)

### Documentation

5. **ENGINE_ARCHITECTURE.md**
   - Complete architecture design document
   - Principles, pipeline, state structure, layer system
   - Event integration, module system
   - Implementation phases and success criteria

6. **ENGINE_IMPLEMENTATION.md**
   - Practical implementation guide
   - Current vs new architecture comparison
   - Code generation changes needed
   - Module template updates
   - Migration path and testing plan

---

## 🎯 Next Steps (Phase 2)

### 1. Integrate Engine into Build Pipeline

**Task:** Make CodeGenerator call SmsRenderBackend to generate engine.h/engine.c

**Files to modify:**
- `Retruxel.Core/Services/CodeGenerator.cs`

**Changes:**
```csharp
// In GenerateCode method, before generating modules:
var smsBackend = new SmsRenderBackend();
var engineHeader = smsBackend.GenerateEngineHeader();
var engineSource = smsBackend.GenerateEngineSource();

generatedFiles.Add(engineHeader);
generatedFiles.Add(engineSource);
```

### 2. Update main.c.rtrx

**Task:** Integrate engine initialization and render loop

**File:** `Plugins/CodeGens/main/sms/main.c.rtrx`

**Changes:**
```c
#include "SMSlib.h"
#include "engine.h"  // NEW
{{#each headers}}
{{this}}
{{/each}}

// Global game state (NEW)
GameState g_gameState = {0};

void main(void) {
{{#if splashEnabled}}
    splash_show();
{{/if}}
{{#ifnot splashEnabled}}
    Engine_Init();  // NEW
{{/ifnot}}

    // Initialize modules (populate g_gameState)
{{#each initCalls}}
{{this}}
{{/each}}

    while(1) {
        SMS_waitForVBlank();
        
        // Process input
{{#each inputCalls}}
{{this}}
{{/each}}

        // Update game logic
{{#each updateCalls}}
{{this}}
{{/each}}

        // Render (NEW)
        Engine_Render(&g_gameState);
        Engine_ClearDirtyFlags(&g_gameState);
    }
}
```

### 3. Update Tilemap Module Template

**Task:** Use GameState instead of direct VRAM writes

**File:** `Plugins/CodeGens/tilemap/sms/tilemap.c.rtrx`

**Changes:**
```c
// @retruxel:block header
#ifndef TILEMAP_{{instanceId}}_H
#define TILEMAP_{{instanceId}}_H

#include "engine.h"

extern const unsigned char {{tilesAssetId}}[];
extern const unsigned int tilemap_{{instanceId}}_map[];

void tilemap_{{instanceId}}_init(void);

#endif
// @retruxel:end

// @retruxel:block source
#include "tilemap_{{instanceId}}.h"

// Tile data
const unsigned char {{tilesAssetId}}[] = {
{{tilesData.tilesHex}}
};

// Map data
const unsigned int tilemap_{{instanceId}}_map[] = {
{{preprocessed.processedMapHex}}
};

void tilemap_{{instanceId}}_init(void) {
    // Populate GameState (no direct VRAM writes)
    extern GameState g_gameState;
    
    g_gameState.background.tileData = {{tilesAssetId}};
    g_gameState.background.tileDataSize = sizeof({{tilesAssetId}});
    g_gameState.background.mapData = tilemap_{{instanceId}}_map;
    g_gameState.background.mapDataSize = sizeof(tilemap_{{instanceId}}_map);
    g_gameState.background.startTile = {{startTile}};
    g_gameState.background.mapX = {{preprocessed.drawX}};
    g_gameState.background.mapY = {{preprocessed.drawY}};
    g_gameState.background.mapWidth = {{preprocessed.clippedWidth}};
    g_gameState.background.mapHeight = {{preprocessed.clippedHeight}};
    
    // Mark dirty (will render on next VBlank)
    g_gameState.backgroundDirty = true;
}
// @retruxel:end
```

### 4. Update Text Display Module Template

**Task:** Use GameState instead of direct VRAM writes

**File:** `Plugins/CodeGens/text_display/sms/text_display.c.rtrx`

**Changes:**
```c
// @retruxel:block header
#ifndef TEXT_DISPLAY_{{instanceId}}_H
#define TEXT_DISPLAY_{{instanceId}}_H

#include "engine.h"

void text_display_{{instanceId}}_init(void);

#endif
// @retruxel:end

// @retruxel:block source
#include "text_display_{{instanceId}}.h"
#include <string.h>

void text_display_{{instanceId}}_init(void) {
    // Populate GameState (no direct VRAM writes)
    extern GameState g_gameState;
    
    strncpy(g_gameState.ui.text, "{{text}}", sizeof(g_gameState.ui.text) - 1);
    g_gameState.ui.x = {{x}};
    g_gameState.ui.y = {{y}};
    g_gameState.ui.color = 0;
    
    // Mark dirty (will render on next VBlank)
    g_gameState.uiDirty = true;
}
// @retruxel:end
```

### 5. Test with Simple Project

**Task:** Create test project with tilemap + text

**Steps:**
1. Create new SMS project
2. Add tilemap module
3. Add text_display module
4. Build ROM
5. Verify both render without conflicts

**Expected Result:**
- Tilemap appears
- Text appears on top
- No flickering or VRAM conflicts

---

## 🔥 Phase 3: Advanced Features

### 1. Sprite Module Integration
- Update sprite module to use GameState.sprites
- Implement sprite rendering in Engine_Render

### 2. Scroll Module Integration
- Update scroll module to use GameState.scrollX/scrollY
- Implement scroll in Engine_Render

### 3. Palette Module Integration
- Add palette state to GameState
- Implement LoadPalette in Engine_Render

### 4. Physics Module Integration
- Physics updates GameState (no rendering)
- Collision detection uses tilemap state

---

## 🎯 Success Metrics

### Immediate (Phase 2)
- ✅ Tilemap + Text render together without conflicts
- ✅ No direct VRAM writes in module code
- ✅ All rendering happens in Engine_Render during VBlank
- ✅ Existing projects compile successfully

### Medium Term (Phase 3)
- ✅ All graphic modules use GameState
- ✅ Dirty flags optimize VRAM bandwidth
- ✅ Performance matches or exceeds current implementation

### Long Term (Phase 4+)
- ✅ NES backend implemented
- ✅ Same project compiles for SMS and NES
- ✅ SNES backend implemented
- ✅ Multi-platform projects work seamlessly

---

## 📊 Architecture Benefits

### For Users
- No need to understand VRAM
- No need to understand VBlank timing
- No conflicts between modules
- Same project works on multiple platforms

### For Developers
- Clear separation of concerns
- Easy to add new platforms
- Platform-specific code isolated
- Testable and maintainable

### For Performance
- Dirty flags minimize VRAM writes
- Batch rendering during VBlank
- Predictable frame timing
- Optimized for each platform

---

## 🚀 Ready to Proceed

The foundation is complete. Next step is integrating the engine into the build pipeline and updating module templates.

**Recommended order:**
1. Integrate SmsRenderBackend into CodeGenerator
2. Update main.c.rtrx
3. Update tilemap.c.rtrx
4. Update text_display.c.rtrx
5. Test with simple project
6. Validate with Kung Fu Master

**Estimated effort:** 2-3 hours for Phase 2 implementation + testing
