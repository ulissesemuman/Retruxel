using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Reflection;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Discovers and renders declarative CodeGens from the plugins/CodeGens/ folder.
///
/// A CodeGen is a folder (anywhere under plugins/CodeGens/) that contains:
///   codegen.json   — metadata, variable mappings, tool references
///   *.rtrx         — C template file (filename declared inside codegen.json)
///
/// Rendering pipeline for each module instance:
///   1. Locate the matching CodeGen folder by moduleId + targetId
///   2. Build variable dictionary:
///      a. "from": "module" → read value from the module's JSON
///      b. "from": "tool"   → locate tool by ToolId via reflection,
///                            call Execute(), merge returned keys into dict
///   3. Inject ModuleRenderer-managed variables (instanceId for non-singletons)
///   4. Render the .rtrx template blocks via TemplateEngine
///   5. Return GeneratedFile list (header + source)
///
/// The ModuleRenderer does not know what any tool does or what it returns.
/// It trusts that the codegen.json author matched variable names correctly
/// between the tool's output contract and the .rtrx template.
/// </summary>
public class ModuleRenderer
{
    private readonly string _pluginsPath;
    private readonly Dictionary<string, ITool> _tools;        // keyed by ToolId
    private readonly Dictionary<string, CodeGenManifest> _codeGens; // keyed by "targetId::moduleId"
    private Assembly? _targetAssembly;

    // Per-render instance counters for non-singleton modules
    private readonly Dictionary<string, int> _instanceCounters = new();

    public ModuleRenderer(string pluginsPath, Assembly? targetAssembly = null)
    {
        _pluginsPath = pluginsPath;
        _targetAssembly = targetAssembly;
        _tools = DiscoverTools();
        _codeGens = DiscoverCodeGens();
        
        // Debug: log discovered CodeGens
        Console.WriteLine($"[ModuleRenderer] Plugins path: {pluginsPath}");
        Console.WriteLine($"[ModuleRenderer] Discovered {_codeGens.Count} CodeGens:");
        foreach (var (key, manifest) in _codeGens)
        {
            Console.WriteLine($"  - {key} → {manifest.TemplatePath}");
        }
    }

    /// <summary>
    /// Resets instance counters. Call before each full build.
    /// </summary>
    public void ResetState() => _instanceCounters.Clear();

    /// <summary>
    /// Sets the target assembly for discovering IToolExtension implementations.
    /// </summary>
    public void SetTargetAssembly(Assembly? targetAssembly)
        => _targetAssembly = targetAssembly;

    /// <summary>
    /// Returns true if a declarative CodeGen exists for this moduleId + targetId.
    /// </summary>
    public bool CanRender(string moduleId, string targetId)
        => _codeGens.ContainsKey(Key(targetId, moduleId));

    /// <summary>
    /// Renders the main.c file using the declarative CodeGen for the target.
    /// Returns a single GeneratedFile (main.c).
    /// </summary>
    public GeneratedFile? RenderMainFile(
        string targetId,
        RetruxelProject project,
        IEnumerable<GeneratedFile> moduleFiles)
    {
        var key = Key(targetId, "main");
        if (!_codeGens.TryGetValue(key, out var manifest))
            return null;

        if (!manifest.IsSystemModule)
            return null;

        // Build variable dictionary from project and moduleFiles
        var variables = new Dictionary<string, object>
        {
            ["projectName"] = project.Name,
            ["targetId"] = project.TargetId,
            ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["moduleFiles"] = moduleFiles.ToList()
        };

        // Resolve variables from manifest
        foreach (var (varName, varDef) in manifest.Variables)
        {
            switch (varDef.From)
            {
                case "project":
                    if (varDef.Path == "name")
                        variables[varName] = project.Name;
                    else if (varDef.Path == "targetId")
                        variables[varName] = project.TargetId;
                    break;

                case "system":
                    if (varName == "timestamp")
                        variables[varName] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    break;

                case "settings":
                    // TODO: Load from SettingsService if needed
                    variables[varName] = varDef.Default ?? false;
                    break;

                case "constant":
                    variables[varName] = varDef.Default ?? new string[0];
                    break;

                case "moduleFiles":
                    // These are processed by the template engine
                    // Just pass the moduleFiles list
                    break;
            }
        }

        // Load and render template
        var template = File.ReadAllText(manifest.TemplatePath);
        var content = TemplateEngine.Render(template, variables);

        return new GeneratedFile
        {
            FileName = "main.c",
            Content = content,
            FileType = GeneratedFileType.Source,
            SourceModuleId = "retruxel.core"
        };
    }

    /// <summary>
    /// Renders the CodeGen for the given module instance.
    /// Returns header + source GeneratedFiles, same as ICodeGenPlugin.Generate().
    /// </summary>
    public IEnumerable<GeneratedFile> Render(
        string moduleId,
        string targetId,
        string moduleJson,
        bool isSingleton)
    {
        var key = Key(targetId, moduleId);
        if (!_codeGens.TryGetValue(key, out var manifest))
            yield break;

        // Build variable dictionary
        var variables = ResolveVariables(manifest, moduleJson);

        // Inject instanceId for non-singleton modules
        if (!isSingleton)
        {
            if (!_instanceCounters.ContainsKey(key))
                _instanceCounters[key] = 0;

            var instanceId = _instanceCounters[key]++;
            variables["instanceId"] = instanceId;
            variables["isFirstInstance"] = instanceId == 0;
        }

        // Load and render template
        var template = File.ReadAllText(manifest.TemplatePath);

        yield return new GeneratedFile
        {
            FileName = isSingleton
                ? $"{manifest.ModuleId.Replace('.', '_')}.h"
                : $"{manifest.ModuleId.Replace('.', '_')}_{variables["instanceId"]}.h",
            FileType = GeneratedFileType.Header,
            SourceModuleId = moduleId,
            Content = TemplateEngine.RenderBlock(template, "header", variables)
        };

        yield return new GeneratedFile
        {
            FileName = isSingleton
                ? $"{manifest.ModuleId.Replace('.', '_')}.c"
                : $"{manifest.ModuleId.Replace('.', '_')}_{variables["instanceId"]}.c",
            FileType = GeneratedFileType.Source,
            SourceModuleId = moduleId,
            Content = TemplateEngine.RenderBlock(template, "source", variables)
        };
    }

    /// <summary>
    /// Returns all discovered tools — used by the Tools panel to list standalone tools.
    /// </summary>
    public IEnumerable<ITool> GetStandaloneTools()
        => _tools.Values;

    // ── Variable resolution ───────────────────────────────────────────────────

    private Dictionary<string, object> ResolveVariables(
        CodeGenManifest manifest,
        string moduleJson)
    {
        var result = new Dictionary<string, object>();

        using var doc = JsonDocument.Parse(moduleJson);
        var root = doc.RootElement;

        foreach (var (varName, varDef) in manifest.Variables)
        {
            switch (varDef.From)
            {
                case "module":
                    result[varName] = ReadModuleValue(root, varDef);
                    break;

                case "tool":
                    var toolValues = InvokeTool(varDef, root);
                    // Store tool output as nested object under the variable name
                    // so template can access as {{varName.property}}
                    result[varName] = toolValues;
                    break;
            }
        }

        return result;
    }

    private static object ReadModuleValue(JsonElement root, VariableDefinition varDef)
    {
        if (!root.TryGetProperty(varDef.Path ?? varDef.Name, out var prop))
            return varDef.Default ?? "";

        // parseBool: treat as boolean regardless of JSON type
        if (varDef.ParseBool)
            return prop.ValueKind == JsonValueKind.True ||
                   (prop.ValueKind == JsonValueKind.String &&
                    prop.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);

        var value = prop.ValueKind switch
        {
            JsonValueKind.Number => prop.TryGetInt32(out var i) ? (object)i : prop.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => prop.GetString() ?? varDef.Default ?? ""
        };

        // If variable name ends with "Length" and value is string, return its length
        if (varDef.Name.EndsWith("Length", StringComparison.OrdinalIgnoreCase) && value is string str)
            return str.Length;

        return value;
    }

    private Dictionary<string, object> InvokeTool(
        VariableDefinition varDef,
        JsonElement moduleRoot)
    {
        if (varDef.ToolId is null)
            return new();

        if (!_tools.TryGetValue(varDef.ToolId, out var tool))
            return new() { [varDef.Name] = $"/* tool '{varDef.ToolId}' not found */" };

        var input = new Dictionary<string, object>();
        if (varDef.ToolInput is not null)
        {
            foreach (var (inputKey, moduleJsonPath) in varDef.ToolInput)
            {
                if (moduleRoot.TryGetProperty(moduleJsonPath, out var v))
                {
                    input[inputKey] = v.ValueKind switch
                    {
                        JsonValueKind.Number => v.TryGetInt32(out var i) ? (object)i : v.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Array => v.EnumerateArray()
                            .Select(e => e.ValueKind == JsonValueKind.Number 
                                ? (e.TryGetInt32(out var ai) ? (object)ai : e.GetDouble())
                                : e.GetString() ?? "")
                            .ToArray(),
                        _ => v.GetString() ?? ""
                    };
                }
            }
        }

        var toolResult = tool.Execute(input);

        // If the tool declares TargetExtensionId, look for extension in target assembly
        if (tool.TargetExtensionId is not null && _targetAssembly is not null)
        {
            var extension = FindToolExtension(_targetAssembly, tool.TargetExtensionId);
            if (extension is not null)
            {
                var extensionResult = extension.Execute(input);
                // Merge: extension overwrites generic keys on conflict
                foreach (var (k, v) in extensionResult)
                    toolResult[k] = v;
            }
            else
            {
                // Log warning — expected extension but not found
                Console.WriteLine($"[ModuleRenderer] WARN: Expected IToolExtension for '{tool.TargetExtensionId}' in target assembly but none found.");
            }
        }

        return toolResult;
    }

    // ── Discovery ─────────────────────────────────────────────────────────────

    private static IToolExtension? FindToolExtension(Assembly targetAssembly, string toolId)
    {
        var extensionTypes = targetAssembly.GetTypes()
            .Where(t => typeof(IToolExtension).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var type in extensionTypes)
        {
            try
            {
                var instance = (IToolExtension)Activator.CreateInstance(type)!;
                if (instance.ToolId == toolId)
                    return instance;
            }
            catch { /* skip */ }
        }

        return null;
    }

    private Dictionary<string, ITool> DiscoverTools()
    {
        var result = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);

        // Scan plugin DLLs for tools
        var toolsDir = Path.Combine(_pluginsPath, "Tools");

        if (!Directory.Exists(toolsDir))
            return result;

        foreach (var dllPath in Directory.GetFiles(toolsDir, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                var asm = Assembly.LoadFrom(dllPath);
                var dllName = Path.GetFileNameWithoutExtension(dllPath);
                
                foreach (var type in asm.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract || !typeof(ITool).IsAssignableFrom(type))
                        continue;

                    if (Activator.CreateInstance(type) is ITool tool)
                    {
                        // Index by ToolId (for backward compatibility)
                        result[tool.ToolId] = tool;
                        
                        // Also index by DLL name (without extension) for simplified codegen.json references
                        // Example: "Retruxel.Tool.TilemapPreprocessor" instead of "tilemap_preprocessor"
                        result[dllName] = tool;
                    }
                }
            }
            catch
            {
                // Skip malformed DLLs
            }
        }

        return result;
    }

    private Dictionary<string, CodeGenManifest> DiscoverCodeGens()
    {
        var result = new Dictionary<string, CodeGenManifest>(StringComparer.OrdinalIgnoreCase);
        var codeGensDir = Path.Combine(_pluginsPath, "CodeGens");

        if (!Directory.Exists(codeGensDir))
            return result;

        // Scan all subdirectories recursively — no naming convention enforced
        foreach (var dir in Directory.GetDirectories(codeGensDir, "*", SearchOption.AllDirectories))
        {
            var manifestPath = Path.Combine(dir, "codegen.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var manifest = LoadManifest(manifestPath, dir);
                if (manifest is null) continue;

                result[Key(manifest.TargetId, manifest.ModuleId)] = manifest;
            }
            catch
            {
                // Malformed codegen.json — skip silently
            }
        }

        return result;
    }

    private static CodeGenManifest? LoadManifest(string manifestPath, string dir)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var raw = JsonSerializer.Deserialize<CodeGenManifestRaw>(
                       File.ReadAllText(manifestPath), opts);

        if (raw is null || raw.ModuleId is null || raw.TargetId is null || raw.Template is null)
            return null;

        var templatePath = Path.Combine(dir, raw.Template);
        if (!File.Exists(templatePath)) return null;

        return new CodeGenManifest
        {
            ModuleId = raw.ModuleId,
            TargetId = raw.TargetId,
            Version = raw.Version ?? "1.0.0",
            TemplatePath = templatePath,
            IsSystemModule = raw.IsSystemModule,
            Variables = ParseVariables(raw.Variables)
        };
    }

    private static Dictionary<string, VariableDefinition> ParseVariables(
        Dictionary<string, JsonElement>? raw)
    {
        var result = new Dictionary<string, VariableDefinition>(StringComparer.OrdinalIgnoreCase);
        if (raw is null) return result;

        foreach (var (name, element) in raw)
        {
            var def = new VariableDefinition { Name = name };

            if (element.TryGetProperty("from", out var from))
                def.From = from.GetString() ?? "module";

            if (element.TryGetProperty("path", out var path))
                def.Path = path.GetString();

            if (element.TryGetProperty("tool", out var tool))
                def.ToolId = tool.GetString();

            if (element.TryGetProperty("parseBool", out var pb))
                def.ParseBool = pb.GetBoolean();

            // Default: literal or array
            if (element.TryGetProperty("default", out var defVal))
            {
                def.Default = defVal.ValueKind == JsonValueKind.Array
                    ? defVal.EnumerateArray().Select(e => e.GetString() ?? "").ToArray()
                    : defVal.ValueKind == JsonValueKind.Number
                        ? (object)(defVal.TryGetInt32(out var i) ? i : defVal.GetDouble())
                        : defVal.GetString() ?? "";
            }

            // Tool input mapping: { "inputKey": "moduleJsonPath" }
            if (element.TryGetProperty("toolInput", out var ti) &&
                ti.ValueKind == JsonValueKind.Object)
            {
                def.ToolInput = new Dictionary<string, string>();
                foreach (var kv in ti.EnumerateObject())
                    def.ToolInput[kv.Name] = kv.Value.GetString() ?? kv.Name;
            }

            result[name] = def;
        }

        return result;
    }

    private static string Key(string targetId, string moduleId)
        => $"{targetId}::{moduleId}".ToLowerInvariant();

    // ── Internal models ───────────────────────────────────────────────────────

    private class CodeGenManifest
    {
        public string ModuleId { get; init; } = "";
        public string TargetId { get; init; } = "";
        public string Version { get; init; } = "1.0.0";
        public string TemplatePath { get; init; } = "";
        public bool IsSystemModule { get; init; } = false;
        public Dictionary<string, VariableDefinition> Variables { get; init; } = new();
    }

    private class VariableDefinition
    {
        public string Name { get; set; } = "";
        public string From { get; set; } = "module";
        public string? Path { get; set; }
        public string? ToolId { get; set; }
        public bool ParseBool { get; set; }
        public object? Default { get; set; }
        public Dictionary<string, string>? ToolInput { get; set; }
    }

    // Raw deserialization shape — variables are left as JsonElement for flexible parsing
    private class CodeGenManifestRaw
    {
        public string? ModuleId { get; set; }
        public string? TargetId { get; set; }
        public string? Version { get; set; }
        public string? Template { get; set; }
        public bool IsSystemModule { get; set; } = false;
        public Dictionary<string, JsonElement>? Variables { get; set; }
    }
}
