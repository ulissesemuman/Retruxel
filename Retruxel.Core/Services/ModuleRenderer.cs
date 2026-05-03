using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Helpers;
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
        Dictionary<Core.Interfaces.IModule, string> triggersByElement,
        IProgress<string>? progress = null)
    {
        var key = Key(targetId, "main");

        if (!_codeGens.TryGetValue(key, out var manifest))
            return null;

        if (!manifest.IsSystemModule)
            return null;

        var filesList = moduleFiles.ToList();

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
                    else if (varDef.Path == "initialSceneId")
                        variables[varName] = project.InitialSceneId;
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
                    // Special case: hasTextDisplay and hasGameVars should resolve to boolean
                    if (varName == "hasTextDisplay")
                    {
                        var hasText = filesList.Any(f => f.SourceModuleId == "text.display");
                        variables[varName] = hasText;
                    }
                    else if (varName == "hasGameVars")
                    {
                        var hasGameVars = filesList.Any(f => f.SourceModuleId == "gamevar");
                        variables[varName] = hasGameVars;
                    }
                    else
                    {
                        var result = ProcessModuleFiles(filesList, varDef, variables);
                        variables[varName] = result;
                    }
                    break;

                case "projectModules":
                    variables[varName] = ProcessProjectModules(project, varDef);
                    break;

                case "updateCalls":
                    var updateCalls = GenerateUpdateCalls(filesList);
                    variables[varName] = updateCalls;
                    break;

                case "inputCalls":
                    var inputCalls = GenerateInputCalls(filesList);
                    variables[varName] = inputCalls;
                    break;
                    
                case "onStartCalls":
                    var onStartCalls = GenerateEventCalls(filesList, triggersByElement, "OnStart", _moduleRegistry);
                    variables[varName] = onStartCalls;
                    break;
                    
                case "onVBlankCalls":
                    var onVBlankCalls = GenerateEventCalls(filesList, triggersByElement, "OnVBlank", _moduleRegistry);
                    // Extract just the call strings for onVBlankCalls (template uses {{this}})
                    variables[varName] = onVBlankCalls
                        .Cast<Dictionary<string, object>>()
                        .Select(d => d["call"].ToString())
                        .ToList();
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
    /// Renders a scene initialization file using the declarative CodeGen for the target.
    /// Returns a single GeneratedFile (scene_&lt;name&gt;.c with embedded header).
    /// </summary>
    public GeneratedFile? RenderSceneFile(
        string targetId,
        SceneData scene,
        IProgress<string>? progress = null)
    {
        var key = Key(targetId, "scene");

        if (!_codeGens.TryGetValue(key, out var manifest))
            return null;

        // Build variable dictionary
        var variables = new Dictionary<string, object>
        {
            ["sceneName"] = scene.SceneName,
            ["sceneNameUpper"] = scene.SceneName.ToUpperInvariant()
        };

        // Get modules from scene - need to instantiate from registry
        var sceneModules = new List<IModule>();
        foreach (var element in scene.Elements)
        {
            // Skip if no module registry available
            if (_moduleRegistry == null) continue;

            IModule? moduleTemplate = null;
            if (_moduleRegistry.GraphicModules.TryGetValue(element.ModuleId, out var gm))
                moduleTemplate = gm;
            else if (_moduleRegistry.LogicModules.TryGetValue(element.ModuleId, out var lm))
                moduleTemplate = lm;
            else if (_moduleRegistry.AudioModules.TryGetValue(element.ModuleId, out var am))
                moduleTemplate = am;

            if (moduleTemplate == null) continue;

            var moduleType = moduleTemplate.GetType();
            var module = (IModule)Activator.CreateInstance(moduleType)!;
            
            var moduleStateJson = element.ModuleState.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                                  element.ModuleState.ValueKind != System.Text.Json.JsonValueKind.Null
                ? element.ModuleState.GetRawText()
                : "{}";
            
            module.Deserialize(moduleStateJson);
            sceneModules.Add(module);
        }

        // Resolve variables from manifest
        foreach (var (varName, varDef) in manifest.Variables)
        {
            if (varDef.From == "scene")
            {
                if (varDef.Path == "name")
                    variables[varName] = varDef.Transform == "upper" 
                        ? scene.SceneName.ToUpperInvariant() 
                        : scene.SceneName;
            }
            else if (varDef.From == "sceneModules")
            {
                variables[varName] = ProcessSceneModules(sceneModules, varDef);
            }
        }

        // Load and render template
        var template = File.ReadAllText(manifest.TemplatePath);
        var content = TemplateEngine.Render(template, variables);

        return new GeneratedFile
        {
            FileName = $"scene_{scene.SceneName}.c",
            Content = content,
            FileType = GeneratedFileType.Source,
            SourceModuleId = "scene"
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
        bool isSingleton,
        string? projectPath = null)
    {
        var key = Key(targetId, moduleId);
        if (!_codeGens.TryGetValue(key, out var manifest))
            yield break;

        // Build variable dictionary
        var variables = ResolveVariables(manifest, moduleJson, projectPath);

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
            filtered = filtered.Where(f => EvaluateFileFilter(f, varDef.Path, existingVariables));
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

            // Split by && and evaluate each condition
            var parts = expr.Split(new[] { "&&" }, StringSplitOptions.TrimEntries);
            return parts.All(part =>
            {
                // Handle == comparison
                if (part.Contains("=="))
                {
                    var tokens = part.Split(new[] { "==" }, StringSplitOptions.TrimEntries);
                    if (tokens.Length == 2)
                    {
                        var left = tokens[0].Trim('\'');
                        var right = tokens[1].Trim('\'');
                        return left == right;
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
                        return left != right;
                    }
                }

                return true;
            });
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
        string moduleJson,
        string? projectPath = null)
    {
        var result = new Dictionary<string, object>();

        // Add projectPath to context if available
        if (!string.IsNullOrEmpty(projectPath))
            result["projectPath"] = projectPath;

        using var doc = JsonDocument.Parse(moduleJson);
        var root = doc.RootElement;

        foreach (var (varName, varDef) in manifest.Variables)
        {
            switch (varDef.From)
            {
                case "module":
                    result[varName] = ReadModuleValue(root, varDef);
                    break;

                case "asset":
                    result[varName] = ResolveAssetValue(root, varDef);
                    break;

                case "tool":
                    var toolValues = InvokeTool(varDef, root, result);
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

    private object ResolveAssetValue(JsonElement root, VariableDefinition varDef)
    {
        // Get asset ID from module JSON
        var assetIdPath = varDef.ToolInput?["assetId"] ?? varDef.Path ?? "assetId";
        if (!root.TryGetProperty(assetIdPath, out var assetIdProp))
            return varDef.Default ?? "";

        var assetId = assetIdProp.GetString();
        if (string.IsNullOrEmpty(assetId))
            return varDef.Default ?? "";

        // Load project to find asset
        // This requires access to the project context - we'll need to pass it through
        // For now, return empty string as fallback
        return varDef.Default ?? "";
    }

    private Dictionary<string, object> InvokeTool(
        VariableDefinition varDef,
        JsonElement moduleRoot,
        Dictionary<string, object> resolvedVariables)
    {
        if (varDef.ToolId is null)
            return new();

        if (!_tools.TryGetValue(varDef.ToolId, out var tool))
            return new() { [varDef.Name] = $"/* tool '{varDef.ToolId}' not found */" };

        // 1. Collect defaults from tool
        var input = tool.GetDefaultParameters();

        // 1.5. Inject projectPath automatically if available
        if (resolvedVariables.TryGetValue("projectPath", out var projectPathObj))
            input["projectPath"] = projectPathObj;

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

        // 3. Merge tool input values from codegen.json
        if (varDef.ToolInput is not null)
        {
            foreach (var (inputKey, valueSource) in varDef.ToolInput)
            {
                // Check if valueSource is a reference to another variable
                if (resolvedVariables.ContainsKey(valueSource))
                {
                    input[inputKey] = resolvedVariables[valueSource];
                }
                // Otherwise try to read from module JSON
                else if (moduleRoot.TryGetProperty(valueSource, out var v))
                {
                    input[inputKey] = v.ValueKind switch
                    {
                        JsonValueKind.Number => v.TryGetInt32(out var i) ? (object)i : v.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Array => ArrayConversionHelper.ToIntArray(v),
                        _ => v.GetString() ?? ""
                    };
                }
                // Otherwise use the literal value
                else
                {
                    input[inputKey] = valueSource;
                }
            }
        }

        // 4. Execute generic tool with fully resolved input
        Dictionary<string, object> toolResult;
        try
        {
            toolResult = tool.Execute(input);
        }
        catch (Exception ex)
        {
            return new() { ["error"] = ex.Message };
        }

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

    // ── Variable resolution ───────────────────────────────────────────────────

    private static List<string> ProcessProjectModules(RetruxelProject project, VariableDefinition varDef)
    {
        // Get all module IDs from all scenes
        var allModuleIds = project.Scenes
            .SelectMany(s => s.Elements)
            .Select(e => e.ModuleId)
            .ToList();

        // Apply filter if specified
        if (!string.IsNullOrEmpty(varDef.Path))
        {
            allModuleIds = allModuleIds.Where(moduleId => EvaluateModuleIdFilter(moduleId, varDef.Path)).ToList();
        }

        // Apply transform
        if (varDef.Transform == "moduleId")
        {
            return allModuleIds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return allModuleIds;
    }

    private static bool EvaluateModuleIdFilter(string moduleId, string filter)
    {
        // Simple filter evaluation: "moduleId == 'input' || moduleId == 'physics'"
        var parts = filter.Split(new[] { "||" }, StringSplitOptions.TrimEntries);
        return parts.Any(part =>
        {
            if (part.Contains("=="))
            {
                var tokens = part.Split(new[] { "==" }, StringSplitOptions.TrimEntries);
                if (tokens.Length == 2)
                {
                    var left = tokens[0].Trim();
                    var right = tokens[1].Trim().Trim('\'', '"');
                    
                    if (left == "moduleId")
                        return moduleId.Equals(right, StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        });
    }

    private static object ProcessSceneModules(List<IModule> modules, VariableDefinition varDef)
    {
        // Apply filter if specified
        var filtered = modules.AsEnumerable();
        if (!string.IsNullOrEmpty(varDef.Path))
        {
            filtered = filtered.Where(m => EvaluateModuleFilter(m, varDef.Path));
        }

        // Apply transform
        if (varDef.Transform == "initCall")
        {
            return filtered.Select(m => new Dictionary<string, object>
            {
                ["header"] = $"{m.ModuleId.Replace(".", "_")}.h",
                ["call"] = $"{m.ModuleId.Replace(".", "_")}_init"
            }).ToList();
        }
        else if (varDef.Transform == "hasAny")
        {
            return filtered.Any();
        }

        return filtered.Select(m => m.ModuleId).ToList();
    }

    private static bool EvaluateModuleFilter(IModule module, string filter)
    {
        // Simple filter evaluation: "moduleId == 'input' || moduleId == 'physics'"
        var parts = filter.Split(new[] { "||" }, StringSplitOptions.TrimEntries);
        return parts.Any(part =>
        {
            if (part.Contains("=="))
            {
                var tokens = part.Split(new[] { "==" }, StringSplitOptions.TrimEntries);
                if (tokens.Length == 2)
                {
                    var left = tokens[0].Trim();
                    var right = tokens[1].Trim().Trim('\'', '"');
                    
                    if (left == "moduleId")
                        return module.ModuleId.Equals(right, StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        });
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
            catch (Exception ex)
            {
                // Skip malformed DLLs - only log if it's a real error
                if (!ex.Message.Contains("BadImageFormatException"))
                    Console.WriteLine($"[ModuleRenderer] WARN: Failed to load tool from {dllPath}: {ex.Message}");
            }
        }

        return result;
    }

    private Dictionary<string, CodeGenManifest> DiscoverCodeGens(IProgress<string>? progress = null)
    {
        var result = new Dictionary<string, CodeGenManifest>(StringComparer.OrdinalIgnoreCase);
        var codeGensDir = Path.Combine(_pluginsPath, "CodeGens");

        if (!Directory.Exists(codeGensDir))
            return result;

        // Scan all subdirectories recursively — no naming convention enforced
        var dirs = Directory.GetDirectories(codeGensDir, "*", SearchOption.AllDirectories);

        foreach (var dir in dirs)
        {
            var manifestPath = Path.Combine(dir, "codegen.json");
            if (!File.Exists(manifestPath)) continue;

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
                return null;

            var templatePath = Path.Combine(dir, raw.Template);
            if (!File.Exists(templatePath))
                return null;

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
    /// 
    /// With state-based rendering, graphic modules need _update() to keep dirty flags set.
    /// Otherwise Engine_ClearDirtyFlags() will clear them and the module disappears.
    /// 
    /// Modules with update:
    ///   - Logic: animation, entity, enemy, scroll, input
    ///   - Graphics: text.display, sprite (if animated)
    /// 
    /// Modules without update (static, only need init):
    ///   - tilemap (loaded once, stays in VRAM)
    ///   - palette (loaded once, stays in CRAM)
    ///   - physics (only provides collision detection)
    /// 
    /// Note: This is a pragmatic solution. Ideally, GameState would have
    /// 'persistent' flags to avoid clearing dirty flags for static modules.
    /// </summary>
    private static List<string> GenerateUpdateCalls(List<GeneratedFile> files)
    {
        var modulesWithUpdate = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Logic modules (always need update)
            "animation", 
            "entity", 
            "enemy", 
            "scroll"
            // text.display removed: no longer has _update(), uses _set() for runtime changes
            // sprite removed: sprite rendering is driven by entity/enemy _update(), not standalone
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
            }
        }

        return updateCalls;
    }

    /// <summary>
    /// Generates input update calls separately to ensure they execute first in the main loop.
    /// </summary>
    private static List<string> GenerateInputCalls(List<GeneratedFile> files)
    {
        var inputCalls = new List<string>();
        var processedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (file.FileType != GeneratedFileType.Header)
                continue;

            var moduleId = file.SourceModuleId;
            if (string.IsNullOrEmpty(moduleId) || processedModules.Contains(moduleId))
                continue;

            if (moduleId.Equals("input", StringComparison.OrdinalIgnoreCase))
            {
                var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                inputCalls.Add($"        {baseName}_update();");
                processedModules.Add(moduleId);
            }
        }

        return inputCalls;
    }
    
    /// <summary>
    /// Generates event-specific initialization or update calls based on trigger.
    /// Filters modules by their assigned trigger (OnStart, OnVBlank, etc.).
    /// Returns objects with 'call' and 'isGraphicModule' properties.
    /// Palettes are sorted first to ensure they load before tiles.
    /// </summary>
    private static List<object> GenerateEventCalls(
        List<GeneratedFile> files,
        Dictionary<Core.Interfaces.IModule, string> triggersByElement,
        string targetTrigger,
        ModuleRegistry? moduleRegistry)
    {
        var calls = new List<object>();
        var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        var graphicModuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "tilemap", "sprite", "text.display", "palette", "background", "animation"
        };

        // Collect all calls first
        var allCalls = new List<(string fileName, string moduleId, string baseName, bool isGraphic, bool isTextDisplay, bool isPalette)>();

        foreach (var file in files)
        {
            if (file.FileType != GeneratedFileType.Header)
                continue;

            var moduleId = file.SourceModuleId;
            if (string.IsNullOrEmpty(moduleId))
                continue;
                
            // Skip system modules
            if (moduleId.Equals("retruxel.core", StringComparison.OrdinalIgnoreCase) ||
                moduleId.Equals("retruxel.splash", StringComparison.OrdinalIgnoreCase) ||
                moduleId.Equals("retruxel.engine", StringComparison.OrdinalIgnoreCase))
                continue;

            // Check if this file's module instance has the target trigger
            var hasTargetTrigger = triggersByElement.Any(kvp => 
                kvp.Key.ModuleId == moduleId && 
                kvp.Value.Equals(targetTrigger, StringComparison.OrdinalIgnoreCase));

            if (!hasTargetTrigger)
                continue;

            // Avoid duplicate calls for singleton modules
            if (processedFiles.Contains(file.FileName))
                continue;

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var isGraphic = graphicModuleIds.Contains(moduleId);
            var isTextDisplay = moduleId.Equals("text.display", StringComparison.OrdinalIgnoreCase);
            var isPalette = moduleId.Equals("palette", StringComparison.OrdinalIgnoreCase);
            
            allCalls.Add((file.FileName, moduleId, baseName, isGraphic, isTextDisplay, isPalette));
            processedFiles.Add(file.FileName);
        }

        // Sort: palettes first, text.display last, everything else in between
        var sortedCalls = allCalls
            .OrderByDescending(c => c.isPalette)
            .ThenBy(c => c.isTextDisplay)
            .ToList();

        // Convert to output format
        foreach (var (fileName, moduleId, baseName, isGraphic, isTextDisplay, isPalette) in sortedCalls)
        {
            calls.Add(new Dictionary<string, object>
            {
                ["call"] = $"    {baseName}_init();",
                ["isGraphicModule"] = isGraphic,
                ["isTextDisplay"] = isTextDisplay
            });
        }

        return calls;
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
