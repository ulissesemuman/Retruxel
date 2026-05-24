# Retruxel - Development Guidelines

## Code Quality Standards

### File Headers and Documentation
- **XML Documentation**: All public interfaces, classes, and methods use XML doc comments (`///`)
- **Class-level summaries**: Describe purpose, responsibilities, and usage patterns
- **Method-level summaries**: Explain what the method does, parameters, and return values
- **Inline comments**: Used sparingly for complex logic, not for obvious code
- **Example pattern**:
```csharp
/// <summary>
/// Discovers and renders declarative CodeGens from the plugins/CodeGens/ folder.
///
/// A CodeGen is a folder (anywhere under plugins/CodeGens/) that contains:
///   codegen.json   — metadata, variable mappings, tool references
///   *.rtrx         — C template file (filename declared inside codegen.json)
/// </summary>
public class ModuleRenderer
{
    /// <summary>
    /// Returns true if a declarative CodeGen exists for this moduleId + targetId.
    /// </summary>
    public bool CanRender(string moduleId, string targetId)
        => _codeGens.ContainsKey(Key(targetId, moduleId));
}
```

### Naming Conventions
- **Interfaces**: PascalCase with `I` prefix (`ITarget`, `IModule`, `ITool`)
- **Classes**: PascalCase (`ModuleRenderer`, `CodeGenerator`, `TilemapModule`)
- **Methods**: PascalCase (`GenerateAsync`, `CanRender`, `GetHardwarePalette`)
- **Private fields**: camelCase with `_` prefix (`_moduleRegistry`, `_tools`, `_pluginsPath`)
- **Local variables**: camelCase (`moduleId`, `targetId`, `sourceFiles`)
- **Constants**: PascalCase (`API_VERSION`)
- **Enums**: PascalCase for type and values (`RetroPixelFormat`, `RETRO_PIXEL_FORMAT_RGB565`)

### Code Formatting
- **Indentation**: 4 spaces (no tabs)
- **Braces**: Opening brace on same line for methods, properties, and control structures
- **Line length**: No strict limit, but prefer readability
- **Blank lines**: Single blank line between methods, two lines between major sections
- **Expression-bodied members**: Used for simple one-liners
```csharp
public string TargetId => "sms";
public bool CanRender(string moduleId, string targetId)
    => _codeGens.ContainsKey(Key(targetId, moduleId));
```

### Null Handling
- **Nullable reference types**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **Null-coalescing**: Preferred over explicit null checks
```csharp
var value = prop.GetString() ?? varDef.Default ?? "";
```
- **Null-conditional operator**: Used extensively
```csharp
progress?.Report($"INFO: {moduleId} generated via plugin fallback.");
```
- **Pattern matching**: Used for null checks
```csharp
if (Activator.CreateInstance(type) is ITool tool)
{
    result[tool.ToolId] = tool;
}
```

### Error Handling
- **Try-catch blocks**: Used for external operations (file I/O, reflection, JSON parsing)
- **Silent failures**: Logged but don't crash the application
```csharp
catch (Exception ex)
{
    progress?.Report($"ERROR: Failed to load manifest {manifestPath}: {ex.Message}");
    return null;
}
```
- **Graceful degradation**: Return empty collections instead of null
```csharp
public IEnumerable<GeneratedFile> GenerateEngineRuntime() 
    => Enumerable.Empty<GeneratedFile>();
```

## Architectural Patterns

### Plugin Discovery via Reflection
All plugins (targets, tools, modules) are discovered automatically at runtime:

```csharp
private Dictionary<string, ITool> DiscoverTools()
{
    var result = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);
    var toolsDir = Path.Combine(_pluginsPath, "Tools");
    
    foreach (var dllPath in Directory.GetFiles(toolsDir, "*.dll", SearchOption.AllDirectories))
    {
        var asm = Assembly.LoadFrom(dllPath);
        foreach (var type in asm.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !typeof(ITool).IsAssignableFrom(type))
                continue;
                
            if (Activator.CreateInstance(type) is ITool tool)
                result[tool.ToolId] = tool;
        }
    }
    return result;
}
```

**Key principles**:
- No manual registration required
- Case-insensitive dictionary keys
- Skip abstract classes and interfaces
- Handle malformed DLLs gracefully

### Interface-Based Abstraction
Core never depends on concrete implementations:

```csharp
public class CodeGenerator
{
    private readonly ModuleRegistry _moduleRegistry;
    private readonly ModuleRenderer _moduleRenderer;
    private readonly ITarget _target;  // Interface, not concrete type
    
    public CodeGenerator(ModuleRegistry moduleRegistry, ModuleRenderer moduleRenderer, ITarget target)
    {
        _moduleRegistry = moduleRegistry;
        _moduleRenderer = moduleRenderer;
        _target = target;
    }
}
```

**Benefits**:
- Enables plugin architecture
- Facilitates testing with mocks
- Allows third-party extensions

### Priority-Based Code Generation
Multiple code generation strategies with fallback chain:

```csharp
// Priority 1: Try ModuleRenderer (declarative CodeGens)
if (_moduleRenderer.CanRender(module.ModuleId, project.TargetId))
{
    generatedFiles = _moduleRenderer.Render(...).ToList();
    progress?.Report($"INFO: {module.ModuleId} generated via ModuleRenderer.");
}
// Priority 2: Ask target to translate (legacy fallback)
else if (_target.GenerateCodeForModule(contextualModule).ToList() is var targetFiles && targetFiles.Count > 0)
{
    generatedFiles = targetFiles;
    progress?.Report($"INFO: {module.ModuleId} generated via target.");
}
// Priority 3: Module's own GenerateCode() (external plugins)
else
{
    generatedFiles = module switch
    {
        ILogicModule lm => lm.GenerateCode().ToList(),
        IGraphicModule gm => gm.GenerateCode().ToList(),
        IAudioModule am => am.GenerateCode().ToList(),
        _ => []
    };
}
```

### State Management with Serialization
Modules store state as JSON and provide serialization methods:

```csharp
public class TilemapModule : IGraphicModule
{
    private TilemapState _state = new();
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    
    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<TilemapState>(json, _jsonOptions) ?? new();
    
    private class TilemapState
    {
        public string TilesAssetId { get; set; } = string.Empty;
        public int StartTile { get; set; } = 0;
        public int MapWidth { get; set; } = 32;
        // ... other properties
    }
}
```

**Pattern**:
- Private nested state class
- Static `JsonSerializerOptions` for performance
- Null-coalescing to ensure non-null state
- camelCase JSON properties

### Progress Reporting
All long-running operations accept `IProgress<string>?` parameter:

```csharp
public async Task<BuildContext> GenerateAsync(
    RetruxelProject project,
    string outputDirectory,
    IProgress<string>? progress = null)
{
    progress?.Report("INIT: Starting code generation...");
    
    foreach (var (moduleId, instances) in instancesByModule)
    {
        progress?.Report($"PROC: Generating code for {instances.Count} {moduleId} instance(s)...");
        // ... processing
        progress?.Report($"INFO: {moduleId} generated via ModuleRenderer.");
    }
    
    progress?.Report($"INFO: {sourceFiles.Count} source files generated.");
}
```

**Conventions**:
- Prefix messages with severity: `INIT`, `PROC`, `INFO`, `WARN`, `ERROR`
- Use null-conditional operator (`?.`) for optional progress
- Report both start and completion of operations
- Include counts and identifiers in messages

### Dictionary-Based Configuration
Configuration and metadata stored in dictionaries:

```csharp
private Dictionary<string, object> ResolveVariables(
    CodeGenManifest manifest,
    string moduleJson,
    string? projectPath = null)
{
    var result = new Dictionary<string, object>();
    
    if (!string.IsNullOrEmpty(projectPath))
        result["projectPath"] = projectPath;
    
    foreach (var (varName, varDef) in manifest.Variables)
    {
        switch (varDef.From)
        {
            case "module":
                result[varName] = ReadModuleValue(root, varDef);
                break;
            case "tool":
                result[varName] = InvokeTool(varDef, root, result);
                break;
        }
    }
    
    return result;
}
```

**Benefits**:
- Flexible schema without compile-time types
- Easy to extend with new keys
- Natural fit for JSON serialization

### Type Forwarding for SDK
Public SDK uses `TypeForwardedTo` to redirect to Core implementations:

```csharp
// Retruxel.SDK/RetruxelSdk.cs
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.IModule))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.ITarget))]
[assembly: TypeForwardedTo(typeof(Retruxel.Core.Interfaces.ITool))]
// ... more forwards
```

**Purpose**:
- Plugin developers reference only SDK
- Core can change internal APIs without breaking plugins
- CLR resolves types from Core at runtime

### P/Invoke for Native Libraries
LibRetro integration uses P/Invoke with proper marshaling:

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct RetroGameInfo
{
    public IntPtr path;
    public IntPtr data;
    public UIntPtr size;
    public IntPtr meta;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void RetroVideoRefreshCallback(IntPtr data, uint width, uint height, UIntPtr pitch);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
public delegate bool RetroEnvironmentCallback(uint cmd, IntPtr data);
```

**Conventions**:
- `StructLayout(LayoutKind.Sequential)` for structs
- `UnmanagedFunctionPointer(CallingConvention.Cdecl)` for callbacks
- `MarshalAs(UnmanagedType.U1)` for bool returns
- `IntPtr` for pointers, `UIntPtr` for sizes

## Common Code Idioms

### Collection Initialization
Modern collection expressions and initializers:

```csharp
// Collection expressions (C# 12+)
generatedFiles = module switch
{
    ILogicModule lm => lm.GenerateCode().ToList(),
    IGraphicModule gm => gm.GenerateCode().ToList(),
    _ => []  // Empty collection expression
};

// Object initializers
return new GeneratedFile
{
    FileName = "main.c",
    Content = content,
    FileType = GeneratedFileType.Source,
    SourceModuleId = "retruxel.core"
};

// Collection initializers
Parameters =
[
    new ParameterDefinition { Name = "startTile", Type = ParameterType.Int },
    new ParameterDefinition { Name = "mapWidth", Type = ParameterType.Int }
]
```

### LINQ Patterns
Extensive use of LINQ for data transformation:

```csharp
// Filtering and projection
var updateCalls = files
    .Where(f => f.FileType == GeneratedFileType.Header)
    .Where(f => modulesWithUpdate.Contains(f.SourceModuleId))
    .Select(f => $"        {Path.GetFileNameWithoutExtension(f.FileName)}_update();")
    .ToList();

// Grouping
var grouped = filtered.GroupBy(f => GetFileProperty(f, varDef.GroupBy));

// Any/All predicates
if (sourceFiles.Any(f => f.FileName == file.FileName))
    continue;

// ToDictionary
BuildParameters = project.Parameters
    .Where(p => p.Key.StartsWith("target."))
    .ToDictionary(p => p.Key.Replace("target.", ""), p => p.Value)
```

### Switch Expressions
Pattern matching with switch expressions:

```csharp
var value = prop.ValueKind switch
{
    JsonValueKind.Number => prop.TryGetInt32(out var i) ? (object)i : prop.GetDouble(),
    JsonValueKind.True => true,
    JsonValueKind.False => false,
    JsonValueKind.Array => prop.EnumerateArray().Select(e => e.ToString()).ToList(),
    JsonValueKind.Object => prop,
    _ => prop.GetString() ?? varDef.Default ?? ""
};
```

### String Interpolation
Prefer interpolation over concatenation:

```csharp
progress?.Report($"PROC: Generating code for {instances.Count} {moduleId} instance(s)...");
progress?.Report($"INFO: {sourceFiles.Count} source files generated.");

var key = $"{targetId}::{moduleId}".ToLowerInvariant();
```

### File Path Handling
Use `Path` class methods for cross-platform compatibility:

```csharp
var templatePath = Path.Combine(dir, raw.Template);
var baseName = Path.GetFileNameWithoutExtension(file.FileName);
var toolsDir = Path.Combine(_pluginsPath, "Tools");
```

### Deduplication Patterns
HashSet for tracking processed items:

```csharp
var processedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

foreach (var file in files)
{
    if (processedModules.Contains(moduleId))
        continue;
        
    // ... process file
    processedModules.Add(moduleId);
}
```

## Target-Agnostic Design

### Never Hardcode Target IDs
**Anti-pattern** ❌:
```csharp
return targetId switch
{
    "sms" or "gg" or "sg1000" => GenerateSmsMasterPalette(),
    "nes" => GenerateNesPalette(),
    _ => null
};
```

**Correct pattern** ✅:
```csharp
var hardwareColors = target.GetHardwarePalette();
var hardwarePalette = hardwareColors
    .Select(c => (uint)((0xFF << 24) | (c.R << 16) | (c.G << 8) | c.B))
    .ToArray();
```

### Use Interface Methods
All target-specific data comes from `ITarget` interface:

```csharp
public interface ITarget
{
    IReadOnlyList<HardwareColor> GetHardwarePalette();
    IToolchain GetToolchain();
    IEnumerable<string> GetRequiredToolchainBinaries();
    IEnumerable<IModule> GetBuiltinModules();
    IEnumerable<GeneratedFile> GenerateEngineRuntime();
    BuildDiagnosticsReport? GetBuildDiagnostics(BuildDiagnosticInput input);
}
```

### Tool Extensions for Target-Specific Logic
Tools delegate to target-specific extensions:

```csharp
// Generic tool discovers extension at runtime
IToolExtension? extension = null;
if (tool.TargetExtensionId is not null && _targetAssembly is not null)
{
    extension = FindToolExtension(_targetAssembly, tool.TargetExtensionId);
    if (extension is not null)
    {
        var extensionResult = extension.Execute(extensionInput);
        foreach (var (k, v) in extensionResult)
            toolResult[k] = v;
    }
}
```

## Module Development

### Module Structure
Standard module implementation pattern:

```csharp
public class TilemapModule : IGraphicModule
{
    // Interface implementation
    public string ModuleId => "tilemap";
    public string DisplayName => "Tilemap";
    public string Category => "Graphics";
    public ModuleType Type => ModuleType.Logic;
    public bool IsSingleton => false;
    public string[] Compatibility { get; set; } = [];
    
    // State management
    private TilemapState _state = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    
    // Manifest for auto-generated UI
    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Parameters = [ /* parameter definitions */ ]
    };
    
    // Serialization
    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<TilemapState>(json, _jsonOptions) ?? new();
    
    // Code generation (delegated to target or ModuleRenderer)
    public IEnumerable<GeneratedFile> GenerateCode() => [];
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];
}
```

### Parameter Definitions
Declarative UI generation from manifest:

```csharp
Parameters =
[
    new ParameterDefinition
    {
        Name         = "startTile",
        DisplayName  = "Start Tile",
        Description  = "First VRAM tile slot to load graphics into (0–447).",
        Type         = ParameterType.Int,
        DefaultValue = 0,
        MinValue     = 0,
        MaxValue     = 447
    },
    new ParameterDefinition
    {
        Name         = "paletteRef",
        DisplayName  = "Palette",
        Description  = "Palette module to use for this tilemap.",
        Type         = ParameterType.ModuleReference,
        ModuleFilter = "palette",
        Required     = true
    }
]
```

## Testing and Validation

### Validation Through Use Case
- Primary validation: Kung Fu Master port
- Every feature must work for real-world game porting
- No automated test suite in alpha stage
- Manual testing via ROM compilation and emulation

### Build Validation
```csharp
progress?.Report($"INFO: {sourceFiles.Count} source files generated.");
progress?.Report($"INFO: {assets.Count} assets generated.");

if (diagnostics is not null)
{
    progress?.Report($"INFO: Build diagnostics generated — {diagnostics.Metrics.Count} metrics.");
}
```

## Performance Considerations

### Static JSON Options
Reuse `JsonSerializerOptions` instances:

```csharp
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
```

### Case-Insensitive Dictionaries
Use `StringComparer.OrdinalIgnoreCase` for lookups:

```csharp
var result = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);
var processedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
```

### Lazy Evaluation
Use `IEnumerable<T>` for deferred execution:

```csharp
public IEnumerable<GeneratedFile> GenerateEngineRuntime()
{
    yield return backend.GenerateEngineHeader();
    yield return backend.GenerateEngineSource();
}
```

### Avoid Repeated Reflection
Cache discovered types and instances:

```csharp
private readonly Dictionary<string, ITool> _tools;
private readonly Dictionary<string, CodeGenManifest> _codeGens;

public ModuleRenderer(string pluginsPath)
{
    _tools = DiscoverTools();      // Called once at startup
    _codeGens = DiscoverCodeGens(); // Called once at startup
}
```
