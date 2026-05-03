# Retruxel Engine Architecture

## 🎯 Vision

Transform Retruxel into an intelligent abstraction layer over retro hardware, where users describe **what they want**, and the engine decides **how to execute correctly on each platform**.

---

## 🧠 Core Principles

### 1. Separation of Concerns

```
┌─────────────────────────────────────────────────────────────┐
│                      USER MODULES                            │
│  (Tilemap, Sprite, Text, Physics, Input, etc.)              │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    GAME STATE                                │
│  (Single source of truth - no direct VRAM access)           │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                  RENDER PIPELINE                             │
│  (Platform-agnostic render commands)                         │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                 RENDER BACKEND                               │
│  (SMS / NES / SNES / etc. - hardware-specific)              │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
                    [VRAM]
```

### 2. Critical Rule

> **No event (OnStart, OnInput, etc.) writes directly to VRAM.**

All VRAM updates happen exclusively during VBlank through the render backend.

---

## 🔄 Frame Execution Pipeline

```
┌──────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Input   │───▶│ Update State │───▶│ Render Logic │───▶│  VRAM Sync   │
│ (OnInput)│    │  (OnVBlank)  │    │   (Buffer)   │    │  (VBlank)    │
└──────────┘    └──────────────┘    └──────────────┘    └──────────────┘
```

### Detailed Flow

```c
// Frame N
OnInput_Button1() {
    gameState.player.isJumping = true;  // Only modify state
    gameState.spriteDirty = true;       // Mark for render
}

OnVBlank() {
    // 1. Update game logic
    UpdatePhysics(&gameState);
    
    // 2. Generate render commands (if dirty)
    if (gameState.tilemapDirty) {
        renderBuffer.AddCommand(DRAW_TILEMAP, ...);
    }
    if (gameState.spriteDirty) {
        renderBuffer.AddCommand(DRAW_SPRITE, ...);
    }
    
    // 3. Execute render commands (backend-specific)
    RenderBackend_Execute(&renderBuffer);
    
    // 4. Clear dirty flags
    ClearDirtyFlags(&gameState);
}
```

---

## 🧩 Game State Structure

### Core State

```c
typedef struct {
    // Layer states
    TilemapState background;
    TilemapState foreground;  // Future: SNES
    TextLayerState ui;
    SpriteLayerState sprites;
    
    // Dirty flags (optimization)
    bool backgroundDirty;
    bool foregroundDirty;
    bool uiDirty;
    bool spritesDirty;
    
    // Global state
    uint8_t scrollX;
    uint8_t scrollY;
    bool scrollDirty;
    
} GameState;
```

### Layer-Specific States

```c
typedef struct {
    uint8_t* tileData;
    uint16_t* mapData;
    uint8_t mapWidth;
    uint8_t mapHeight;
    uint8_t startTile;
    uint8_t paletteIndex;
} TilemapState;

typedef struct {
    char text[256];
    uint8_t x;
    uint8_t y;
    uint8_t color;
} TextLayerState;

typedef struct {
    uint8_t x;
    uint8_t y;
    uint8_t tileIndex;
    bool visible;
    bool flipX;
    bool flipY;
} SpriteState;

typedef struct {
    SpriteState sprites[64];  // SMS limit
    uint8_t count;
} SpriteLayerState;
```

---

## 🎨 Layer System (Abstraction)

### Layer Types

```c
typedef enum {
    LAYER_BACKGROUND,   // Main tilemap
    LAYER_FOREGROUND,   // Future: SNES BG2
    LAYER_UI,           // Text, HUD
    LAYER_SPRITES       // Entities
} LayerType;
```

### Composition Order (Fixed)

```
Background → Foreground → Sprites → UI
```

### Platform-Specific Behavior

**SMS:**
- Background + UI → merged into single tilemap
- Sprites → hardware sprite layer
- Foreground → not supported (ignored or merged)

**NES:**
- Background → nametable 0
- Foreground → nametable 1 (with attribute tables)
- Sprites → OAM
- UI → merged with background or sprites

**SNES (future):**
- Background → BG1
- Foreground → BG2
- UI → BG3 or window layer
- Sprites → OAM with priority

---

## 🧱 Render Pipeline

### 1. Render Command Buffer

```c
typedef enum {
    CMD_DRAW_TILEMAP,
    CMD_DRAW_TEXT,
    CMD_DRAW_SPRITE,
    CMD_SET_SCROLL,
    CMD_LOAD_PALETTE
} RenderCommandType;

typedef struct {
    RenderCommandType type;
    void* data;
} RenderCommand;

typedef struct {
    RenderCommand commands[256];
    uint8_t count;
} RenderCommandBuffer;
```

### 2. Render Backend Interface

```c
typedef struct {
    void (*Init)(void);
    void (*DrawTilemap)(TilemapState* state);
    void (*DrawText)(TextLayerState* state);
    void (*DrawSprite)(SpriteState* sprite);
    void (*SetScroll)(uint8_t x, uint8_t y);
    void (*LoadPalette)(uint8_t* colors, uint8_t count);
    void (*ExecuteBuffer)(RenderCommandBuffer* buffer);
} RenderBackend;
```

### 3. Backend Implementations

```c
// SMS Backend
RenderBackend RenderBackend_SMS = {
    .Init = SMS_RenderInit,
    .DrawTilemap = SMS_DrawTilemap,
    .DrawText = SMS_DrawText,
    .DrawSprite = SMS_DrawSprite,
    .SetScroll = SMS_SetScroll,
    .LoadPalette = SMS_LoadPalette,
    .ExecuteBuffer = SMS_ExecuteBuffer
};

// NES Backend (future)
RenderBackend RenderBackend_NES = {
    .Init = NES_RenderInit,
    .DrawTilemap = NES_DrawTilemap,
    // ...
};
```

---

## 🎮 Event System Integration

### Current Events (Retruxel)

- `OnStart` → executes once at boot
- `OnVBlank` → executes every frame (~60Hz)
- `OnInput_Button1` → button press
- `OnInput_Button2` → button press

### Event Behavior

**OnStart:**
```c
void OnStart() {
    // Initialize state
    gameState.background = LoadTilemap("map1");
    gameState.ui.text = "HELLO";
    
    // Mark dirty
    gameState.backgroundDirty = true;
    gameState.uiDirty = true;
    
    // NO direct VRAM writes!
}
```

**OnInput:**
```c
void OnInput_Button1() {
    // Only modify state
    gameState.sprites.sprites[0].x += 1;
    gameState.spritesDirty = true;
    
    // NO rendering here!
}
```

**OnVBlank:**
```c
void OnVBlank() {
    // 1. Update logic
    UpdatePhysics(&gameState);
    
    // 2. Render (via backend)
    if (gameState.backgroundDirty) {
        RenderBackend->DrawTilemap(&gameState.background);
    }
    if (gameState.spritesDirty) {
        for (int i = 0; i < gameState.sprites.count; i++) {
            RenderBackend->DrawSprite(&gameState.sprites.sprites[i]);
        }
    }
    
    // 3. Clear flags
    gameState.backgroundDirty = false;
    gameState.spritesDirty = false;
}
```

---

## 🔧 Module System Integration

### Module Interface Extension

```c
// Current: IModule (C#)
// Future: Add runtime hooks

typedef struct {
    void (*Update)(GameState* state);   // Called during OnVBlank
    void (*Render)(RenderContext* ctx); // Generates render commands
} ModuleRuntime;
```

### Module Types

**Logic Module:**
- Updates game state
- Does NOT render directly
- Example: Physics, Input, AI

**Graphic Module:**
- Writes to render context (not VRAM)
- Example: Tilemap, Sprite, Text

**Audio Module:**
- Independent of render pipeline
- Example: Music, SFX

---

## 🚀 Implementation Phases

### Phase 1: Core Infrastructure (Current Sprint)
- [x] Create `GameState` structure (Retruxel.Core/Engine/)
- [x] Create `RenderCommandBuffer` (Retruxel.Core/Engine/)
- [x] Create `IRenderBackend` interface (Retruxel.Core/Interfaces/)
- [ ] Implement `SmsRenderBackend` (Retruxel.Target.SMS/Rendering/)
- [ ] Implement `NesRenderBackend` (Retruxel.Target.NES/Rendering/) - future

### Phase 2: Event System Refactor
- [ ] Modify `main.c.rtrx` to use new pipeline
- [ ] Update `OnVBlank` to call render backend
- [ ] Ensure no modules write to VRAM directly

### Phase 3: Module Integration
- [ ] Update Tilemap module to use state
- [ ] Update Text module to use state
- [ ] Update Sprite module to use state

### Phase 4: Optimization
- [ ] Implement dirty flag system
- [ ] Add render command batching
- [ ] Profile VRAM bandwidth usage

### Phase 5: Multi-Platform (Future)
- [ ] Implement `RenderBackend_NES`
- [ ] Implement `RenderBackend_SNES`
- [ ] Add platform-specific optimizations

---

## 📊 Performance Considerations

### VRAM Bandwidth Limits

**SMS:**
- VBlank duration: ~1.3ms (76 scanlines at 60Hz)
- Safe VRAM writes: ~1KB per frame
- Strategy: Batch updates, prioritize dirty regions

**NES:**
- VBlank duration: ~2.3ms (20 scanlines at 60Hz)
- Safe VRAM writes: ~256 bytes per frame
- Strategy: Aggressive dirty tracking, buffer updates

### Dirty Flag Optimization

```c
// Only update what changed
if (gameState.backgroundDirty) {
    // Full tilemap update (expensive)
    RenderBackend->DrawTilemap(&gameState.background);
}

// Future: Partial updates
if (gameState.backgroundPartialDirty) {
    // Only update changed tiles (cheap)
    RenderBackend->DrawTilemapRegion(&gameState.background, x, y, w, h);
}
```

---

## 🎯 Success Criteria

### User Experience
- ✅ User never thinks about VRAM
- ✅ User never thinks about VBlank timing
- ✅ User never thinks about hardware limits
- ✅ Same project runs on SMS, NES, SNES

### Technical Goals
- ✅ Zero direct VRAM writes outside render backend
- ✅ All rendering happens during VBlank
- ✅ Dirty flags minimize bandwidth usage
- ✅ Backend abstraction allows easy porting

### Code Quality
- ✅ Clear separation: State → Logic → Render → Backend
- ✅ Platform-specific code isolated in backends
- ✅ Modules are platform-agnostic
- ✅ Easy to add new platforms

---

## 🔥 Next Steps

1. **Create base structures** (`GameState`, `RenderBackend`)
2. **Implement SMS backend** (reference implementation)
3. **Refactor existing modules** (Tilemap, Text, Sprite)
4. **Update code generation** (main.c.rtrx, module templates)
5. **Test with Kung Fu Master** (real-world validation)
6. **Document patterns** (for future contributors)

---

## 📚 References

- GB Studio: Scene-based rendering with automatic VRAM management
- NESmaker: Module system with hardware abstraction
- SGDK (Sega Genesis): State-driven sprite engine
- SMSlib: Low-level SMS hardware access (current foundation)
