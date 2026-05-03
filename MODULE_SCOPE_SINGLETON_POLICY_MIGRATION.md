# ModuleScope & SingletonPolicy Migration

> Implemented: 2025-01-XX
> Replaces boolean IsSingleton with granular SingletonPolicy
> Adds per-instance scope override capability

---

## Summary

Migrated from boolean `IsSingleton` to enum `SingletonPolicy` with three levels:
- **Global**: One per project (shared across scenes)
- **PerScene**: One per scene (different scenes can have their own)
- **Multiple**: Unlimited instances

Added `ModuleScope` to determine initialization location:
- **Project**: Initialized in `main.c` OnStart, persists across scenes
- **Scene**: Initialized in `scene_X_init()`, reloaded per scene

---

## Changes Made

### 1. New Enums Created

**Retruxel.Core/Models/ModuleScope.cs**
```csharp
public enum ModuleScope
{
    Project,  // main() OnStart
    Scene     // scene_X_init()
}
```

**Retruxel.Core/Models/SingletonPolicy.cs**
```csharp
public enum SingletonPolicy
{
    Global,    // One per project
    PerScene,  // One per scene
    Multiple   // Unlimited
}
```

### 2. IModule Interface Updated

**Before:**
```csharp
bool IsSingleton { get; }
ModuleScope DefaultScope { get; }
```

**After:**
```csharp
SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;
ModuleScope DefaultScope => ModuleScope.Project;
```

### 3. ITarget Interface Updated

**Before:**
```csharp
IEnumerable<ModuleOverride> GetModuleOverrides();
```

**After:**
```csharp
Dictionary<string, SingletonPolicy> GetModulePolicyOverrides() => new();
```

### 4. SceneElementData Enhanced

Added scope override capability:
```csharp
[JsonPropertyName("scopeOverride")]
public ModuleScope? ScopeOverride { get; set; } = null;

public ModuleScope GetEffectiveScope(IModule module)
    => ScopeOverride ?? module.DefaultScope;
```

---

## Module Migration Table

| Module | Old IsSingleton | New SingletonPolicy | DefaultScope |
|--------|----------------|---------------------|--------------|
| **PaletteModule** | false | PerScene | Scene |
| **TilemapModule** | false | Multiple | Scene |
| **SpriteModule** | false | Multiple | Project |
| **TextDisplayModule** | false | Multiple | Scene |
| **AnimationModule** | false | Global | Project |
| **InputModule** | true | Global | Project |
| **PhysicsModule** | true | Global | Project |
| **EntityModule** | true | Global | Project |
| **EnemyModule** | false | Multiple | Scene |
| **ScrollModule** | true | PerScene | Scene |
| **GameVarModule** | false | Multiple | Project |

---

## Target Overrides

### SmsTarget

**Before:**
```csharp
public IEnumerable<ModuleOverride> GetModuleOverrides() =>
[
    new ModuleOverride { ModuleId = "palette", MaxInstances = 2 },
    new ModuleOverride { ModuleId = "entity", IsSingleton = true },
    // ... more overrides
];
```

**After:**
```csharp
public Dictionary<string, SingletonPolicy> GetModulePolicyOverrides() =>
    new() { ["palette"] = SingletonPolicy.PerScene };
```

SMS enforces palette as PerScene (only one palette per scene).

---

## Validation Logic

### SceneAddResult Enum
```csharp
public enum SceneAddResult
{
    Allow,          // No restrictions
    WarnGlobal,     // Global singleton already exists
    WarnPerScene,   // PerScene singleton already exists in this scene
    Block           // Not used currently (reserved for future)
}
```

### Validation Method
```csharp
public SceneAddResult CanAddModule(Scene scene, IModule module, ITarget target)
{
    var policy = target.GetModulePolicyOverrides()
        .TryGetValue(module.ModuleId, out var p) ? p : module.SingletonPolicy;

    var existing = scene.Elements
        .Where(e => e.ModuleId == module.ModuleId)
        .ToList();

    return policy switch
    {
        SingletonPolicy.Global => existing.Any()
            ? SceneAddResult.WarnGlobal
            : SceneAddResult.Allow,

        SingletonPolicy.PerScene => existing.Any()
            ? SceneAddResult.WarnPerScene
            : SceneAddResult.Allow,

        SingletonPolicy.Multiple => SceneAddResult.Allow,
        _ => SceneAddResult.Allow
    };
}
```

### Warning Messages

**WarnGlobal:**
> "Physics_0 already exists (Global). Adding Physics_1 allows runtime switching via Interactions."

**WarnPerScene:**
> "Palette_0 already exists in this scene. Multiple palette instances will overwrite each other unless managed via Interactions."

---

## UI Implementation (Pending)

### Module Properties Panel

When a SceneElement is selected, show scope and policy at the top:

```
┌─────────────────────────────────┐
│ TILEMAP_0                       │
│─────────────────────────────────│
│ Scope   [ Scene    ▼ ]          │
│ Policy  [ Multiple    ] (read)  │
│─────────────────────────────────│
│ ... module properties ...       │
└─────────────────────────────────┘
```

**Scope ComboBox:**
- Editable dropdown with Project/Scene values
- Initial value = `module.DefaultScope`
- On change, persists to `SceneElement.ScopeOverride`
- If matches default, clears override (sets to null)

**Policy Label:**
- Read-only text showing effective policy
- Resolves from target override or module default
- Not editable (module characteristic, not user choice)

### XAML Structure
```xml
<StackPanel Margin="0,0,0,12">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="64"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="28"/>
            <RowDefinition Height="28"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Grid.Column="0"
                   Text="SCOPE"
                   Style="{StaticResource TextLabelCaps}"/>
        <ComboBox Grid.Row="0" Grid.Column="1"
                  x:Name="CmbScope"
                  SelectionChanged="CmbScope_SelectionChanged"/>

        <TextBlock Grid.Row="1" Grid.Column="0"
                   Text="POLICY"
                   Style="{StaticResource TextLabelCaps}"/>
        <TextBlock Grid.Row="1" Grid.Column="1"
                   x:Name="TxtPolicy"
                   Foreground="{StaticResource BrushOnSurfaceVariant}"/>
    </Grid>
</StackPanel>

<Border Height="1"
        Background="{StaticResource BrushSurfaceContainerHighest}"
        Margin="0,0,0,12"/>
```

---

## Code Generation Impact

### ModuleRenderer Classification

Modules are now classified by effective scope:

```csharp
var projectModules = scene.Elements
    .Where(e => e.GetEffectiveScope(GetModule(e.ModuleId)) == ModuleScope.Project)
    .ToList();

var sceneModules = scene.Elements
    .Where(e => e.GetEffectiveScope(GetModule(e.ModuleId)) == ModuleScope.Scene)
    .ToList();
```

### Template Usage

**main.c.rtrx** iterates `projectModules` for OnStart section
**scene_X.c.rtrx** iterates `sceneModules` for scene init

---

## Benefits

### 1. Granular Control
- **Before**: Binary singleton/non-singleton
- **After**: Three-level policy (Global, PerScene, Multiple)

### 2. Per-Instance Overrides
Users can override default scope per instance:
- Sprite normally Project scope (shared)
- Boss sprite can be Scene scope (scene-specific)

### 3. Better Warnings
- Global: "Already exists globally, adding allows runtime switching"
- PerScene: "Already exists in this scene, will overwrite"
- Multiple: No warning

### 4. Target Flexibility
Targets can enforce stricter policies:
- SMS forces palette to PerScene (hardware limitation)
- NES might allow Multiple palettes (different hardware)

---

## Migration Checklist

- ✅ Create ModuleScope enum
- ✅ Create SingletonPolicy enum
- ✅ Update IModule interface
- ✅ Update ITarget interface
- ✅ Add ScopeOverride to SceneElementData
- ✅ Update all standard modules
- ✅ Update SmsTarget with GetModulePolicyOverrides
- ⏳ Implement UI in SceneEditor
- ⏳ Update ModuleRenderer to use effective scope
- ⏳ Add validation warnings in SceneEditor
- ⏳ Update documentation

---

## Breaking Changes

### For Module Developers
- Replace `bool IsSingleton` with `SingletonPolicy SingletonPolicy`
- Add `ModuleScope DefaultScope` property

### For Target Developers
- Replace `GetModuleOverrides()` with `GetModulePolicyOverrides()`
- Return `Dictionary<string, SingletonPolicy>` instead of `IEnumerable<ModuleOverride>`

### For Project Files
- New optional field: `scopeOverride` in SceneElementData
- Backward compatible: null = use module default

---

## Future Enhancements

### Additional Scopes
```csharp
public enum ModuleScope
{
    Project,
    Scene,
    Level,      // Persists per level, not per scene
    Persistent  // Saved to save file
}
```

### Additional Policies
```csharp
public enum SingletonPolicy
{
    Global,
    PerScene,
    Multiple,
    Limited     // Max N instances (configurable)
}
```

---

## Notes

### Why Enemy is Scene Scope?
Enemies are scene-specific entities. Each scene has its own enemy placement and behavior. They should be reloaded when entering a scene.

### Why Scroll is PerScene?
Each scene can have different scroll settings (speed, direction, loop). Only one scroll configuration per scene makes sense for SMS hardware.

### Why Animation is Global?
Animation definitions are shared across scenes. The player uses the same walk/jump/attack animations regardless of which scene they're in.
