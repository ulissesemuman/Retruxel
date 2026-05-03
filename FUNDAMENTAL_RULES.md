# Retruxel — Fundamental Rules

> **CRITICAL**: These rules MUST be followed in EVERY implementation, no matter how simple.
> They define the core architectural principles that make Retruxel pluggable and maintainable.

---

## Rule #1: Kung Fu Master is THE Primary Use Case

**Context**: Retruxel started as a port of Kung Fu Master (NES→Master System) and evolved into a full IDE.

**Rule**: Kung Fu Master is THE primary use case driving all development decisions toward v1.0.

**Why**: Every feature, every module, every tool must be validated against the real-world complexity of porting Kung Fu Master. If it doesn't work for Kung Fu Master, it doesn't work.

**Examples**:
- Map offset feature exists because NES has 2 extra tile rows (30 vs 24)
- Tilemap editor supports large maps because Kung Fu Master has scrolling levels
- LiveLink integration exists to capture NES graphics in real-time

**Validation**: Before implementing any feature, ask: "Does this help port Kung Fu Master?"

---

## Rule #2: Zero Hardcoded Target Knowledge

**Context**: Retruxel is a pluggable IDE where targets are discovered at runtime via reflection.

**Rule**: Core code and tools MUST NEVER contain hardcoded target-specific logic. All target knowledge must come from `ITarget` interface methods.

**Why**: Hardcoding target IDs (like `"sms"`, `"nes"`, `"gg"`) violates pluggability. When a new target is added, no core code should need to change.

**Anti-Pattern** ❌:
```csharp
// WRONG: Hardcoded switch on target IDs
return targetId switch
{
    "sms" or "gg" or "sg1000" => GenerateSmsMasterPalette(),
    "nes" => GenerateNesPalette(),
    _ => null
};
```

**Correct Pattern** ✅:
```csharp
// RIGHT: Use ITarget interface
var hardwareColors = target.GetHardwarePalette();
var hardwarePalette = hardwareColors
    .Select(c => (uint)((0xFF << 24) | (c.R << 16) | (c.G << 8) | c.B))
    .ToArray();
```

**How to Fix Violations**:
1. Identify the hardcoded logic
2. Find or create the appropriate `ITarget` method
3. Pass `ITarget` instance instead of `targetId` string
4. Call the interface method to get target-specific data

**Examples of ITarget Methods**:
- `GetHardwarePalette()` — Returns all colors the hardware can display
- `GetToolchain()` — Returns the compiler/assembler for this target
- `GetBuiltinModules()` — Returns target-specific modules
- `GenerateCodeForModule()` — Translates universal modules to target code
- `GetBuildDiagnostics()` — Returns hardware usage stats

**Method Naming Convention**:
- ❌ `GenerateSmsMasterPalette()` — Target name in method
- ✅ `GeneratePalette()` — Generic, implemented by each target

**Where This Applies**:
- Pipelines (asset conversion, code generation)
- Tools (editors, importers, preprocessors)
- Core services (build orchestrator, module renderer)
- UI code (target selection, settings panels)

**Exception**: The ONLY place target IDs can be hardcoded is in the target plugin itself (e.g., `SmsTarget.cs` can return `"sms"` from `TargetId` property).

---

## Rule #3: Tools Delegate to Target Extensions

**Context**: Some operations require target-specific implementations (e.g., palette generation, asset conversion).

**Rule**: Tools should delegate target-specific logic to `IToolExtension` implementations inside target plugins, not implement it themselves.

**Why**: This keeps tools generic and allows each target to provide its own specialized behavior.

**Pattern**:
1. Tool defines a generic interface (e.g., `IPaletteGenerator`)
2. Each target implements `IToolExtension` with that interface
3. Tool discovers and calls the extension at runtime

**Example**:
```csharp
// Tool code (generic)
var paletteGenerator = _toolRegistry.GetToolExtension<IPaletteGenerator>(target.TargetId);
if (paletteGenerator != null)
{
    var palette = paletteGenerator.GeneratePalette(colors);
}

// Target code (SMS-specific)
public class SmsPaletteGenerator : IToolExtension, IPaletteGenerator
{
    public string TargetId => "sms";
    public uint[] GeneratePalette(uint[] colors) => /* SMS logic */;
}
```

**Status**: SMS already has tool extensions. Other targets need to implement them.

---

## Rule #4: Engine Architecture Must Be Target-Agnostic

**Context**: Engine runtime (GameState, RenderBackend, engine.h/engine.c) is provided by targets via `ITarget.GenerateEngineRuntime()`, not hardcoded in Core.

**Rule**: Core MUST call `ITarget.GenerateEngineRuntime()` to get engine files. Core MUST NOT check target IDs or instantiate target-specific classes.

**Why**: Allows third-party developers to create new targets (Mega Drive, SNES, etc.) and distribute them as plugins without modifying Retruxel.Core.

**Anti-Pattern** ❌:
```csharp
// WRONG: Hardcoded target check
if (_target.TargetId == "sms") {
    var backend = new SmsRenderBackend();
    var engineFiles = backend.Generate();
}
```

**Correct Pattern** ✅:
```csharp
// RIGHT: Use interface method
var engineFiles = _target.GenerateEngineRuntime();
sourceFiles.AddRange(engineFiles);
```

**How Targets Implement**:
```csharp
// In SmsTarget.cs
public IEnumerable<GeneratedFile> GenerateEngineRuntime()
{
    var backend = new Rendering.SmsRenderBackend();
    return
    [
        backend.GenerateEngineHeader(),
        backend.GenerateEngineSource()
    ];
}
```

**Interface Definition**:
```csharp
// In ITarget.cs
IEnumerable<GeneratedFile> GenerateEngineRuntime() 
    => Enumerable.Empty<GeneratedFile>(); // Default: no engine
```

**Result**: A developer can create `Retruxel.Target.MegaDrive`, implement `GenerateEngineRuntime()`, and distribute the DLL. Zero changes to Core required.

---

## Rule #5: Event-Based Execution (OnStart, OnVBlank, OnInput)

**Context**: Modules are assigned to events in SceneEditor. Code generation must respect these assignments.

**Rule**: CodeGenerator MUST group modules by their `Trigger` field and generate separate init calls for each event.

**Why**: Ensures correct execution order: OnStart runs once, OnInput processes input, OnVBlank updates logic, then rendering happens.

**Implementation**:
- `CodeGenerator` tracks `triggersByElement` dictionary
- `ModuleRenderer.GenerateEventCalls()` filters modules by trigger
- `main.c.rtrx` has separate sections: `{{#each onStartCalls}}`, `{{#each onVBlankCalls}}`, etc.

**Correct Flow**:
```c
void main(void) {
    // OnStart: Initialize modules
    tilemap_0_init();
    text_display_0_init();
    
    while(1) {
        SMS_waitForVBlank();
        
        // OnInput: Process input
        input_update();
        
        // OnVBlank: Update game logic
        physics_update();
        entity_update();
        
        // Render: Apply state to VRAM
        Engine_Render(&g_gameState);
        Engine_ClearDirtyFlags(&g_gameState);
    }
}
```

**Anti-Pattern** ❌:
```c
// WRONG: All modules in initCalls, no event separation
{{#each initCalls}}
    tilemap_0_init();
    text_display_0_init();
    physics_init();
{{/each}}
```

---

## Rule #6: State-Based Rendering (No Direct VRAM Access)

**Context**: Modules populate `GameState`, engine renders during VBlank. This prevents VRAM conflicts and enables multi-platform support.

**Rule**: Modules MUST NOT write directly to VRAM. They MUST populate `GameState` and mark dirty flags. Only `Engine_Render()` writes to VRAM.

**Why**: 
- Prevents conflicts (tilemap + text no longer conflict)
- Enables dirty flag optimization
- Makes modules platform-agnostic
- Allows easy porting to NES/SNES

**Anti-Pattern** ❌:
```c
// WRONG: Direct VRAM write in module
void tilemap_0_init(void) {
    SMS_loadTiles(tiles, 0, size);
    SMS_loadTileMap(0, 0, map, size);
    SMS_displayOn();
}
```

**Correct Pattern** ✅:
```c
// RIGHT: Populate GameState
void tilemap_0_init(void) {
    extern GameState g_gameState;
    
    g_gameState.background.tileData = tiles;
    g_gameState.background.mapData = map;
    g_gameState.background.startTile = 0;
    // ... other properties
    
    g_gameState.backgroundDirty = true;
}
```

**Engine Renders**:
```c
void Engine_Render(GameState* state) {
    if (state->backgroundDirty) {
        SMS_loadTiles(state->background.tileData, ...);
        SMS_loadTileMap(state->background.mapX, ...);
    }
    if (state->uiDirty) {
        SMS_printatXY(state->ui.x, state->ui.y, state->ui.text);
    }
}
```

**Architecture**:
```
Module Init → GameState → Dirty Flag
                ↓
            VBlank Loop
                ↓
         Engine_Render → VRAM
```

---

## Rule #4: Transparent Information Flow

**Context**: Data flows from targets → tools → UI without manual intervention.

**Rule**: Obtaining target-specific information must be transparent. No manual mapping, no switch statements, no configuration files.

**Why**: When you add a new target, it should "just work" without updating 10 different places.

**How**:
- Targets expose data via `ITarget` methods
- Tools call those methods to get what they need
- UI auto-generates from target data (e.g., settings panels from `GetSettingsDefinitions()`)

**Example**:
```csharp
// WRONG: Manual mapping
var tileSize = targetId == "sms" ? 8 : targetId == "nes" ? 8 : 16;

// RIGHT: Transparent
var tileSize = target.Specs.TileWidth;
```

---

## Enforcement

These rules are enforced through:
1. **Code Review**: Every PR must validate against these rules
2. **Architecture Tests**: Automated tests scan for hardcoded target IDs
3. **Documentation**: This file is the source of truth

**When in Doubt**: If you're writing `if (targetId == "sms")`, you're probably violating Rule #2.

---

## Version History

- **v1.0** (2025-01-XX): Initial rules based on ImportedAssetToTilemapPipeline refactoring
