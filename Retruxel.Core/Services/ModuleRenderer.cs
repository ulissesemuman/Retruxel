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
    private ModuleRegistry? _moduleRegistry;

    // Per-render instance counters for non-singleton modules
    private readonly Dictionary<string, int> _instanceCounters = new();

    public ModuleRenderer(string pluginsPath, Assembly? targetAssembly = null, IProgress<string>? progress = null)
    {
        _pluginsPath = pluginsPath;
        _targetAssembly = targetAssembly;
        _tools = DiscoverTools();
        _codeGens = DiscoverCodeGens(progress);

        // Debug: log discovered CodeGens
        progress?.Report($"DEBUG: ModuleRenderer initialized with {_codeGens.Count} CodeGens");
        foreach (var (key, manifest) in _codeGens)
        {
            progress?.Report($"DEBUG: CodeGen registered: {key} (IsSystemModule={manifest.IsSystemModule})");
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
    /// Sets the module registry for querying singleton status.
    /// </summary>
    public void SetModuleRegistry(ModuleRegistry? registry)
        => _moduleRegistry = registry;

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
        IEnumerable<GeneratedFile> moduleFiles,
        IProgress<string>? progress = null)
    {
        var key = Key(targetId, "main");
        progress?.Report($"DEBUG: Looking for CodeGen key: {key}");

        if (!_codeGens.TryGetValue(key, out var manifest))
        {
            progress?.Report($"DEBUG: CodeGen not found for key: {key}");
            return null;
        }

        if (!manifest.IsSystemModule)
        {
            progress?.Report($"DEBUG: CodeGen found but IsSystemModule=false");
            return null;
        }

        progress?.Report($"DEBUG: Found system module CodeGen, rendering...");

        var filesList = moduleFiles.ToList();
        progress?.Report($"DEBUG: Total module files: {filesList.Count}");
        foreach (var f in filesList)
        {
            progress?.Report($"DEBUG: File: {f.FileName}, Type: {f.FileType}, Module: {f.SourceModuleId}");
        }

        // Build variable dictionary from project and moduleFiles
        var variables = new Dictionary<string, object>
        {
            ["projectName"] = project.Name,
            ["targetId"] = project.TargetId,
            ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
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
                    variables[varName] = ResolveSettingsValue(varDef);
                    break;

                case "constant":
                    variables[varName] = varDef.Default ?? new string[0];
                    break;

                case "moduleFiles":
                    var result = ProcessModuleFiles(filesList, varDef, variables);
                    variables[varName] = result;
                    if (result is List<string> list)
                        progress?.Report($"DEBUG: Variable '{varName}' has {list.Count} items");
                    break;

                case "updateCalls":
                    // Generate update calls for modules that have _update() functions
                    var updateCalls = GenerateUpdateCalls(filesList, progress);
                    variables[varName] = updateCalls;
                    progress?.Report($"DEBUG: Generated {updateCalls.Count} update calls");
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
        // Query ModuleRegistry if available, otherwise use module's IsSingleton
        var effectiveSingleton = _moduleRegistry?.IsModuleSingleton(moduleId) ?? isSingleton;

        if (!effectiveSingleton)
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
            FileName = effectiveSingleton
                ? $"{manifest.ModuleId.Replace('.', '_')}.h"
                : $"{manifest.ModuleId.Replace('.', '_')}_{variables["instanceId"]}.h",
            FileType = GeneratedFileType.Header,
            SourceModuleId = moduleId,
            Content = TemplateEngine.RenderBlock(template, "header", variables)
        };

        yield return new GeneratedFile
        {
            FileName = effectiveSingleton
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

    private object ResolveSettingsValue(VariableDefinition varDef)
    {
        try
        {
            var settings = SettingsService.Load();
            if (varDef.Path is null)
                return varDef.Default ?? false;

            // Navigate nested properties: "General.ShowMadeWithSplash"
            var parts = varDef.Path.Split('.');
            object? current = settings;

            foreach (var part in parts)
            {
                if (current is null) break;
                var prop = current.GetType().GetProperty(part);
                if (prop is null) break;
                current = prop.GetValue(current);
            }

            return current ?? varDef.Default ?? false;
        }
        catch
        {
            return varDef.Default ?? false;
        }
    }

    private object ProcessModuleFiles(
        List<GeneratedFile> files,
        VariableDefinition varDef,
        Dictionary<string, object> existingVariables)
    {
        // Filter files based on varDef.Path (simple expression evaluation)
        var filtered = files.AsEnumerable();

        if (!string.IsNullOrEmpty(varDef.Path))
        {
            Console.WriteLine($"[ProcessModuleFiles] Filter: {varDef.Path}");
            Console.WriteLine($"[ProcessModuleFiles] Files before filter: {files.Count}");
            filtered = filtered.Where(f => EvaluateFileFilter(f, varDef.Path, existingVariables));
            var filteredList = filtered.ToList();
            Console.WriteLine($"[ProcessModuleFiles] Files after filter: {filteredList.Count}");
            filtered = filteredList;
        }

        // Group by sourceModuleId if varDef has groupBy
        if (varDef.GroupBy is not null)
        {
            var grouped = filtered.GroupBy(f => GetFileProperty(f, varDef.GroupBy));
            filtered = grouped.Select(g => g.First());
        }

        // Transform each file into a string using varDef.Transform
        if (!string.IsNullOrEmpty(varDef.Transform))
        {
            return filtered.Select(f => TransformFile(f, varDef.Transform, existingVariables)).ToList();
        }

        // No transform - return file names
        return filtered.Select(f => f.FileName).ToList();
    }

    private bool EvaluateFileFilter(GeneratedFile file, string filter, Dictionary<string, object> variables)
    {
        try
        {
            // Replace file properties with actual values
            var expr = filter
                .Replace("fileType", $"'{file.FileType}'")
                .Replace("fileName", $"'{file.FileName}'")
                .Replace("sourceModuleId", $"'{file.SourceModuleId}'");

            Console.WriteLine($"[EvaluateFileFilter] File: {file.FileName}, Type: {file.FileType}, Module: {file.SourceModuleId}");
            Console.WriteLine($"[EvaluateFileFilter] Filter: {filter}");
            Console.WriteLine($"[EvaluateFileFilter] After replace: {expr}");

            // Split by && and evaluate each condition
            var parts = expr.Split(new[] { "&&" }, StringSplitOptions.TrimEntries);
            var result = parts.All(part =>
            {
                // Handle == comparison
                if (part.Contains("=="))
                {
                    var tokens = part.Split(new[] { "==" }, StringSplitOptions.TrimEntries);
                    if (tokens.Length == 2)
                    {
                        var left = tokens[0].Trim('\'');
                        var right = tokens[1].Trim('\'');
                        var match = left == right;
                        Console.WriteLine($"[EvaluateFileFilter] Compare: '{left}' == '{right}' = {match}");
                        return match;
                    }
                }

                // Handle != comparison
                if (part.Contains("!="))
                {
                    var tokens = part.Split(new[] { "!=" }, StringSplitOptions.TrimEntries);
                    if (tokens.Length == 2)
                    {
                        var left = tokens[0].Trim('\'');
                        var right = tokens[1].Trim('\'');
                        var match = left != right;
                        Console.WriteLine($"[EvaluateFileFilter] Compare: '{left}' != '{right}' = {match}");
                        return match;
                    }
                }

                return true;
            });

            Console.WriteLine($"[EvaluateFileFilter] Final result: {result}");
            return result;
        }
        catch
        {
            return true; // On error, include the file
        }
    }

    private string TransformFile(GeneratedFile file, string transform, Dictionary<string, object> variables)
    {
        // Replace placeholders in transform string:
        // "#include \"{{fileName}}\""
        // "    {{sourceModuleId.Replace('.', '_')}}_init();"
        // "    {{Path.GetFileNameWithoutExtension(fileName)}}_init();"

        var result = transform
            .Replace("{{fileName}}", file.FileName)
            .Replace("{{sourceModuleId}}", file.SourceModuleId);

        // Handle sourceModuleId.Replace('.', '_')
        if (result.Contains("sourceModuleId.Replace"))
        {
            result = result.Replace("{{sourceModuleId.Replace('.', '_')}}", file.SourceModuleId.Replace(".", "_"));
        }

        // Handle Path.GetFileNameWithoutExtension(fileName)
        if (result.Contains("Path.GetFileNameWithoutExtension"))
        {
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            result = result.Replace("{{Path.GetFileNameWithoutExtension(fileName)}}", baseName);
        }

        return result;
    }

    private static string GetFileProperty(GeneratedFile file, string propertyName)
    {
        return propertyName.ToLower() switch
        {
            "sourcemoduleid" => file.SourceModuleId,
            "filename" => file.FileName,
            "filetype" => file.FileType.ToString(),
            _ => file.SourceModuleId
        };
    }

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
            JsonValueKind.Array => prop.EnumerateArray().Select(e => e.ToString()).ToList(),
            JsonValueKind.Object => prop,
            _ => prop.GetString() ?? varDef.Default ?? ""
        };

        // If variable name ends with "Length" and value is string, return its length
        if (varDef.Name.EndsWith("Length", StringComparison.OrdinalIgnoreCase) && value is string str)
            return str.Length;

        // If value is a Base64 string (palette colors), convert to hex array format
        if (value is string base64Str && !string.IsNullOrEmpty(base64Str) &&
            (varDef.Name.Contains("Color", StringComparison.OrdinalIgnoreCase) ||
             varDef.Name.Contains("Palette", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var bytes = Convert.FromBase64String(base64Str);
                var hexValues = bytes.Select((b, i) =>
                    i == bytes.Length - 1 ? $"0x{b:X2}" : $"0x{b:X2},");
                return string.Join(" ", hexValues);
            }
            catch
            {
                // Not Base64, return as-is
            }
        }

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

        // 1. Collect defaults from tool
        var input = tool.GetDefaultParameters();

        // 2. If extension exists, let it override defaults before tool executes
        IToolExtension? extension = null;
        if (tool.TargetExtensionId is not null && _targetAssembly is not null)
        {
            extension = FindToolExtension(_targetAssembly, tool.TargetExtensionId);
            if (extension is not null)
            {
                foreach (var (k, v) in extension.GetDefaultParameters())
                    input[k] = v;
            }
            else
            {
                Console.WriteLine($"[ModuleRenderer] WARN: Expected IToolExtension for '{tool.TargetExtensionId}' in target assembly but none found.");
            }
        }

        // 3. Merge module JSON values (highest priority — explicit project values override defaults)
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

        // 4. Execute generic tool with fully resolved input
        var toolResult = tool.Execute(input);

        // 5. Execute extension post-processing (receives tool output merged into input)
        if (extension is not null)
        {
            // Pass tool output as additional context for the extension
            var extensionInput = new Dictionary<string, object>(input);
            foreach (var (k, v) in toolResult)
                extensionInput[k] = v;

            var extensionResult = extension.Execute(extensionInput);

            // 6. Merge: extension overwrites generic keys on conflict
            foreach (var (k, v) in extensionResult)
                toolResult[k] = v;
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

    private Dictionary<string, CodeGenManifest> DiscoverCodeGens(IProgress<string>? progress = null)
    {
        var result = new Dictionary<string, CodeGenManifest>(StringComparer.OrdinalIgnoreCase);
        var codeGensDir = Path.Combine(_pluginsPath, "CodeGens");

        progress?.Report($"DEBUG: Scanning CodeGens directory: {codeGensDir}");
        progress?.Report($"DEBUG: Directory exists: {Directory.Exists(codeGensDir)}");

        if (!Directory.Exists(codeGensDir))
            return result;

        // Scan all subdirectories recursively — no naming convention enforced
        var dirs = Directory.GetDirectories(codeGensDir, "*", SearchOption.AllDirectories);
        progress?.Report($"DEBUG: Found {dirs.Length} subdirectories in CodeGens");

        foreach (var dir in dirs)
        {
            var manifestPath = Path.Combine(dir, "codegen.json");
            if (!File.Exists(manifestPath)) continue;

            progress?.Report($"DEBUG: Found manifest: {manifestPath}");

            try
            {
                var manifest = LoadManifest(manifestPath, dir, progress);
                if (manifest is null) continue;

                result[Key(manifest.TargetId, manifest.ModuleId)] = manifest;
            }
            catch (Exception ex)
            {
                progress?.Report($"ERROR: Failed to load manifest {manifestPath}: {ex.Message}");
            }
        }

        return result;
    }

    private static CodeGenManifest? LoadManifest(string manifestPath, string dir, IProgress<string>? progress = null)
    {
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var raw = JsonSerializer.Deserialize<CodeGenManifestRaw>(
                           File.ReadAllText(manifestPath), opts);

            if (raw is null || raw.ModuleId is null || raw.TargetId is null || raw.Template is null)
            {
                progress?.Report($"WARN: Invalid manifest at {manifestPath} (moduleId={raw?.ModuleId}, targetId={raw?.TargetId}, template={raw?.Template})");
                return null;
            }

            var templatePath = Path.Combine(dir, raw.Template);
            if (!File.Exists(templatePath))
            {
                progress?.Report($"WARN: Template not found: {templatePath}");
                return null;
            }

            progress?.Report($"DEBUG: Loaded CodeGen: {raw.TargetId}::{raw.ModuleId} (IsSystemModule={raw.IsSystemModule})");

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
        catch (Exception ex)
        {
            progress?.Report($"ERROR: Failed to load manifest {manifestPath}: {ex.Message}");
            return null;
        }
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

            if (element.TryGetProperty("groupBy", out var groupBy))
                def.GroupBy = groupBy.GetString();

            if (element.TryGetProperty("transform", out var transform))
                def.Transform = transform.GetString();

            // Default: literal, boolean, number, or array
            if (element.TryGetProperty("default", out var defVal))
            {
                def.Default = defVal.ValueKind switch
                {
                    JsonValueKind.Array => defVal.EnumerateArray().Select(e => e.GetString() ?? "").ToArray(),
                    JsonValueKind.Number => defVal.TryGetInt32(out var i) ? (object)i : defVal.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => defVal.GetString() ?? ""
                };
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

    /// <summary>
    /// Generates update calls for modules that have _update() functions.
    /// Modules with update: input, animation, entity, enemy, scroll.
    /// Modules without update: palette, tilemap, sprite, physics (only init).
    /// </summary>
    private static List<string> GenerateUpdateCalls(List<GeneratedFile> files, IProgress<string>? progress = null)
    {
        var modulesWithUpdate = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "input", "animation", "entity", "enemy", "scroll"
        };

        var updateCalls = new List<string>();
        var processedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (file.FileType != GeneratedFileType.Header)
                continue;

            var moduleId = file.SourceModuleId;
            if (string.IsNullOrEmpty(moduleId) || processedModules.Contains(moduleId))
                continue;

            if (modulesWithUpdate.Contains(moduleId))
            {
                var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                updateCalls.Add($"        {baseName}_update();");
                processedModules.Add(moduleId);
                progress?.Report($"DEBUG: Added update call for {moduleId} ({baseName})");
            }
        }

        return updateCalls;
    }

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
        public string? GroupBy { get; set; }
        public string? Transform { get; set; }
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
