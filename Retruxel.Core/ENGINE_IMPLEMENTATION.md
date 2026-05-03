# Engine Architecture - Implementation Guide

## 📋 Overview

This document explains how to integrate the new engine architecture into Retruxel's existing codebase without breaking current functionality.

---

## 🎯 Implementation Strategy

### Phase 1: Foundation (Non-Breaking)
Create new engine components alongside existing code. No changes to current modules yet.

**Files Created:**
- ✅ `Retruxel.Core/Engine/GameState.cs`
- ✅ `Retruxel.Core/Engine/RenderCommandBuffer.cs`
- ✅ `Retruxel.Core/Interfaces/IRenderBackend.cs`
- ⏳ `Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs` (next)

### Phase 2: SMS Backend Implementation
Implement `IRenderBackend` for SMS using existing SMSlib functions.

### Phase 3: Code Generation Update
Modify `main.c.rtrx` and module templates to use the new architecture.

### Phase 4: Module Migration
Update existing modules (Tilemap, Text, Sprite) to work with GameState.

---

## 🔧 Current Architecture vs New Architecture

### Current Flow (Direct VRAM Access)

```
Module Init → SMS_loadTiles() → VRAM
              SMS_loadTileMap() → VRAM
              SMS_displayOn()
```

**Problems:**
- Modules write directly to VRAM
- No coordination between modules
- Hard to port to other platforms
- Tilemap + Text conflict (VRAM clear issue)

### New Flow (State-Based Rendering)

```
Module Init → Update GameState → Mark Dirty
OnVBlank → Check Dirty Flags → RenderBackend → VRAM
```

**Benefits:**
- Single point of VRAM access (RenderBackend)
- Platform-agnostic modules
- Automatic conflict resolution
- Easy to port to NES/SNES

---

## 📝 Implementation Details

### 1. Render Backend Implementation (Target-Specific)

**Location:** `Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs`

**Note:** Each target implements `IRenderBackend` in its own assembly.
The interface is platform-agnostic and lives in `Retruxel.Core/Interfaces/`.

**Responsibilities:**
- Translate `GameState` → SMSlib calls
- Execute render commands during VBlank
- Manage VRAM bandwidth limits
- Handle layer composition (merge Background + UI)

**Key Methods:**

```csharp
public class SmsRenderBackend : IRenderBackend
{
    public string TargetId => "sms";
    
    public void Initialize()
    {
        // SMS_VRAMmemsetW(0, 0x0000, 16384);
        // SMS_displayOn();
    }
    
    public void DrawTilemap(TilemapLayerState state)
    {
        // SMS_loadTiles(state.TileData, state.StartTile, ...);
        // SMS_loadTileMap(state.MapX, state.MapY, state.MapData, ...);
    }
    
    public void DrawText(string text, byte x, byte y, byte color)
    {
        // SMS_autoSetUpTextRenderer();
        // SMS_printatXY(x, y, text);
    }
    
    public void ExecuteBuffer(RenderCommandBuffer buffer)
    {
        foreach (var cmd in buffer.Commands)
        {
            switch (cmd.Type)
            {
                case RenderCommandType.DrawTilemap:
                    DrawTilemap((cmd.Data as DrawTilemapCommand)!.State);
                    break;
                case RenderCommandType.DrawText:
                    var textCmd = (DrawTextCommand)cmd.Data!;
                    DrawText(textCmd.Text, textCmd.X, textCmd.Y, textCmd.Color);
                    break;
                // ...
            }
        }
    }
}
```

---

### 2. Code Generation Changes

#### main.c.rtrx (New Structure)

```c
#include "SMSlib.h"
#include "engine.h"  // New: Engine runtime
{{#each headers}}
{{this}}
{{/each}}

// Global game state
GameState g_gameState;
RenderCommandBuffer g_renderBuffer;

void main(void) {
{{#if splashEnabled}}
    splash_show();
{{/if}}
{{#ifnot splashEnabled}}
    // Initialize hardware
    RenderBackend_Init();
{{/ifnot}}

    // Initialize modules (populate GameState)
{{#each initCalls}}
{{this}}
{{/each}}

    while(1) {
        SMS_waitForVBlank();
        
        // 1. Process input (updates GameState)
{{#each inputCalls}}
{{this}}
{{/each}}

        // 2. Update game logic (updates GameState)
{{#each updateCalls}}
{{this}}
{{/each}}

        // 3. Render (GameState → VRAM via RenderBackend)
        Engine_Render(&g_gameState, &g_renderBuffer);
    }
}
```

#### engine.h (New Runtime Header)

```c
#ifndef ENGINE_H
#define ENGINE_H

#include <stdint.h>
#include <stdbool.h>

// Forward declarations
typedef struct GameState GameState;
typedef struct RenderCommandBuffer RenderCommandBuffer;

// Engine runtime functions
void RenderBackend_Init(void);
void Engine_Render(GameState* state, RenderCommandBuffer* buffer);

// State management
void GameState_MarkDirty(GameState* state, uint8_t layer);
void GameState_ClearDirtyFlags(GameState* state);

#endif // ENGINE_H
```

#### engine.c (New Runtime Implementation)

```c
#include "engine.h"
#include "SMSlib.h"

void RenderBackend_Init(void) {
    SMS_VRAMmemsetW(0, 0x0000, 16384);
    SMS_displayOn();
}

void Engine_Render(GameState* state, RenderCommandBuffer* buffer) {
    // Check dirty flags and generate render commands
    if (state->backgroundDirty) {
        // Add DrawTilemap command
    }
    if (state->uiDirty) {
        // Add DrawText command
    }
    if (state->spritesDirty) {
        // Add DrawSprite commands
    }
    
    // Execute all commands (writes to VRAM)
    RenderBackend_ExecuteBuffer(buffer);
    
    // Clear dirty flags
    GameState_ClearDirtyFlags(state);
    
    // Clear command buffer
    buffer->count = 0;
}
```

---

### 3. Module Template Changes

#### tilemap.c.rtrx (New Structure)

**Before (Direct VRAM):**
```c
void tilemap_{{instanceId}}_init(void) {
    SMS_loadTiles({{tilesAssetId}}, {{startTile}}, ...);
    SMS_loadTileMap({{mapX}}, {{mapY}}, tilemap_{{instanceId}}_map, ...);
    SMS_displayOn();
}
```

**After (State-Based):**
```c
void tilemap_{{instanceId}}_init(void) {
    // Populate GameState
    g_gameState.background.tileData = {{tilesAssetId}};
    g_gameState.background.mapData = tilemap_{{instanceId}}_map;
    g_gameState.background.mapWidth = {{mapWidth}};
    g_gameState.background.mapHeight = {{mapHeight}};
    g_gameState.background.startTile = {{startTile}};
    g_gameState.background.mapX = {{mapX}};
    g_gameState.background.mapY = {{mapY}};
    
    // Mark dirty (will render on next VBlank)
    g_gameState.backgroundDirty = true;
}
```

#### text_display.c.rtrx (New Structure)

**Before (Direct VRAM):**
```c
void text_display_{{instanceId}}_init(void) {
    SMS_autoSetUpTextRenderer();
    SMS_printatXY({{x}}, {{y}}, "{{text}}");
}
```

**After (State-Based):**
```c
void text_display_{{instanceId}}_init(void) {
    // Populate GameState
    g_gameState.ui.text = "{{text}}";
    g_gameState.ui.x = {{x}};
    g_gameState.ui.y = {{y}};
    
    // Mark dirty (will render on next VBlank)
    g_gameState.uiDirty = true;
}
```

---

## 🚀 Migration Path

### Step 1: Create Engine Runtime (C Code)
Generate `engine.h` and `engine.c` as part of the build process.

**CodeGen:** `Plugins/CodeGens/engine/sms/`
- `codegen.json`
- `engine.c.rtrx`

### Step 2: Update main.c.rtrx
Add engine initialization and render loop.

### Step 3: Update Module Templates
Change all modules to use GameState instead of direct VRAM calls.

### Step 4: Test with Existing Projects
Ensure backward compatibility - existing ROMs should work identically.

### Step 5: Add Optimization
Implement dirty flag tracking and partial updates.

---

## 🎯 Success Criteria

### Functional
- ✅ Tilemap + Text render together without conflicts
- ✅ No direct VRAM writes outside RenderBackend
- ✅ All rendering happens during VBlank
- ✅ Existing projects compile and run identically

### Performance
- ✅ No performance regression vs current implementation
- ✅ VRAM bandwidth usage within VBlank limits
- ✅ Dirty flags minimize unnecessary updates

### Architecture
- ✅ Clear separation: State → Logic → Render → Backend
- ✅ Platform-specific code isolated in backends
- ✅ Easy to add NES/SNES backends
- ✅ Modules are platform-agnostic

---

## 📊 Testing Plan

### Unit Tests
- GameState dirty flag management
- RenderCommandBuffer overflow handling
- Backend constraint validation

### Integration Tests
- Tilemap + Text rendering (no conflicts)
- Sprite + Background rendering
- Scroll + Tilemap rendering

### Real-World Tests
- Kung Fu Master port (complex scene)
- Multiple tilemaps + sprites
- Dynamic text updates

---

## 🔥 Next Actions

1. **Implement SmsRenderBackend** (in Retruxel.Target.SMS/Rendering/)
2. **Create engine.c.rtrx CodeGen** (C runtime)
3. **Update main.c.rtrx** (integrate engine loop)
4. **Update tilemap.c.rtrx** (use GameState)
5. **Update text_display.c.rtrx** (use GameState)
6. **Test with simple project** (tilemap + text)
7. **Test with Kung Fu Master** (real-world validation)

---

## 📚 References

- `ENGINE_ARCHITECTURE.md` - High-level design
- `PIPELINE_ARCHITECTURE.md` - Asset pipeline
- SMSlib documentation - Hardware functions
- GB Studio source - Scene rendering patterns
