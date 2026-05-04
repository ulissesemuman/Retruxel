using Retruxel.Core.Interfaces;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Resolves variables from different sources (module, project, scene, tool, settings).
/// Handles variable transformation and filtering.
/// </summary>
internal class VariableResolver
{
    private readonly Dictionary<string, ITool> _tools;
    private System.Reflection.Assembly? _targetAssembly;
    private Dictionary<string, object> _globalVariables = new();
    private Models.SceneData? _currentScene;

    public VariableResolver(Dictionary<string, ITool> tools, System.Reflection.Assembly? targetAssembly)
    {
        _tools = tools;
        _targetAssembly = targetAssembly;
    }

    /// <summary>
    /// Sets the current scene for palette slot resolution.
    /// </summary>
    public void SetCurrentScene(Models.SceneData? scene)
        => _currentScene = scene;

    /// <summary>
    /// Sets global variables available to all CodeGen templates.
    /// Used for injecting TextAnalyzer results (fontStartTile, fontTileData, etc.).
    /// </summary>
    public void SetGlobalVariables(Dictionary<string, object> variables)
        => _globalVariables = variables;

    /// <summary>
    /// Updates the target assembly for tool extension discovery.
    /// </summary>
    public void SetTargetAssembly(System.Reflection.Assembly? targetAssembly)
        => _targetAssembly = targetAssembly;

    public Dictionary<string, object> ResolveForModule(
        Dictionary<string, VariableDefinition> variables,
        string moduleJson,
        string? projectPath = null)
    {
        var result = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(projectPath))
        {
            result["projectPath"] = projectPath;
            System.Diagnostics.Debug.WriteLine($"VariableResolver: projectPath = {projectPath}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("WARNING: projectPath is null or empty!");
        }

        using var doc = JsonDocument.Parse(moduleJson);
        var root = doc.RootElement;

        // Inject global variables first
        foreach (var (key, value) in _globalVariables)
        {
            result[key] = value;
        }

        // FIRST PASS: Resolve all "module" variables before invoking tools
        // This ensures tool inputs can reference module properties
        foreach (var (varName, varDef) in variables)
        {
            if (varDef.From == "module")
            {
                result[varName] = ReadModuleValue(root, varDef);
            }
        }

        // SECOND PASS: Resolve tools and other sources
        foreach (var (varName, varDef) in variables)
        {
            switch (varDef.From)
            {
                case "module":
                    // Already resolved in first pass
                    break;

                case "asset":
                    result[varName] = ResolveAssetValue(root, varDef);
                    break;

                case "tool":
                    var toolValues = InvokeTool(varDef, root, result);
                    result[varName] = toolValues;
                    break;

                case "global":
                    if (_globalVariables.TryGetValue(varDef.Path ?? varName, out var globalValue))
                        result[varName] = globalValue;
                    else
                        result[varName] = varDef.Default ?? "";
                    break;

                case "computed":
                    result[varName] = EvaluateComputedExpression(varDef, result);
                    break;
            }
        }

        return result;
    }

    public object ResolveSettingsValue(VariableDefinition varDef)
    {
        try
        {
            var settings = SettingsService.Load();
            if (varDef.Path is null)
                return varDef.Default ?? false;

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

    private static object ReadModuleValue(JsonElement root, VariableDefinition varDef)
    {
        if (!root.TryGetProperty(varDef.Path ?? varDef.Name, out var prop))
            return varDef.Default ?? "";

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

        if (varDef.Name.EndsWith("Length", StringComparison.OrdinalIgnoreCase) && value is string str)
            return str.Length;

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
        var assetIdPath = varDef.ToolInput?["assetId"] ?? varDef.Path ?? "assetId";
        if (!root.TryGetProperty(assetIdPath, out var assetIdProp))
            return varDef.Default ?? "";

        var assetId = assetIdProp.GetString();
        if (string.IsNullOrEmpty(assetId))
            return varDef.Default ?? "";

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

        var input = tool.GetDefaultParameters();

        if (resolvedVariables.TryGetValue("projectPath", out var projectPathObj))
        {
            input["projectPath"] = projectPathObj;
            System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': projectPath = {projectPathObj}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"WARNING: Tool '{varDef.ToolId}' invoked without projectPath!");
        }

        IToolExtension? extension = null;
        if (tool.TargetExtensionId is not null && _targetAssembly is not null)
        {
            System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': Looking for extension '{tool.TargetExtensionId}' in assembly '{_targetAssembly.GetName().Name}'");
            extension = FindToolExtension(_targetAssembly, tool.TargetExtensionId);
            if (extension is not null)
            {
                System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': Extension found! Type = {extension.GetType().Name}");
                foreach (var (k, v) in extension.GetDefaultParameters())
                    input[k] = v;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': Extension NOT found");
            }
        }
        else
        {
            if (tool.TargetExtensionId is null)
                System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': No TargetExtensionId specified");
            if (_targetAssembly is null)
                System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': No target assembly available");
        }

        if (varDef.ToolInput is not null)
        {
            System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': Processing toolInput with {varDef.ToolInput.Count} parameters");

            foreach (var (inputKey, valueSource) in varDef.ToolInput)
            {
                System.Diagnostics.Debug.WriteLine($"  - {inputKey} = '{valueSource}'");

                if (resolvedVariables.ContainsKey(valueSource))
                {
                    var value = resolvedVariables[valueSource];
                    input[inputKey] = value;
                    System.Diagnostics.Debug.WriteLine($"    → Resolved from variables: '{value}'");
                }
                else if (moduleRoot.TryGetProperty(valueSource, out var v))
                {
                    var value = v.ValueKind switch
                    {
                        JsonValueKind.Number => v.TryGetInt32(out var i) ? (object)i : v.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Array => Helpers.ArrayConversionHelper.ToIntArray(v),
                        _ => v.GetString() ?? ""
                    };
                    input[inputKey] = value;
                    System.Diagnostics.Debug.WriteLine($"    → Resolved from moduleRoot: '{value}'");
                }
                else
                {
                    input[inputKey] = valueSource;
                    System.Diagnostics.Debug.WriteLine($"    → Using literal value: '{valueSource}'");
                }
            }
        }

        Dictionary<string, object> toolResult;
        try
        {
            // Inject palette colors from scene if tool is PngToTiles or TilePacker
            if ((varDef.ToolId == "PngToTiles" || varDef.ToolId == "TilePacker") && _currentScene != null)
            {
                var paletteSlot = 0;
                if (moduleRoot.TryGetProperty("paletteSlot", out var slotProp) && slotProp.TryGetInt32(out var slot))
                    paletteSlot = slot;

                if (paletteSlot < _currentScene.PaletteSlots.Count)
                {
                    input["paletteColors"] = _currentScene.PaletteSlots[paletteSlot].Colors;
                    System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': Injected paletteColors from scene slot {paletteSlot}");
                }
            }

            toolResult = tool.Execute(input);

            // Log tool execution for debugging
            System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}' executed successfully");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Tool '{varDef.ToolId}' failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ERROR: {errorMsg}");
            Console.WriteLine($"ERROR: {errorMsg}");
            return new() { ["error"] = errorMsg, ["tilesHex"] = "/* " + errorMsg + " */" };
        }

        if (extension is not null)
        {
            System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': Invoking extension with {toolResult.Count} result keys");
            var extensionInput = new Dictionary<string, object>(input);
            foreach (var (k, v) in toolResult)
                extensionInput[k] = v;

            var extensionResult = extension.Execute(extensionInput);
            System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': Extension returned {extensionResult.Count} keys");

            foreach (var (k, v) in extensionResult)
            {
                toolResult[k] = v;
                System.Diagnostics.Debug.WriteLine($"  - {k} = {v?.ToString()?.Substring(0, Math.Min(50, v?.ToString()?.Length ?? 0))}...");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Tool '{varDef.ToolId}': No extension to invoke");
        }

        return toolResult;
    }

    private static IToolExtension? FindToolExtension(System.Reflection.Assembly targetAssembly, string toolId)
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

    /// <summary>
    /// Evaluates computed expressions for variables.
    /// Currently supports: arrays.Any(a => a.languages.Count > 1)
    /// </summary>
    public object EvaluateComputedExpression(VariableDefinition varDef, Dictionary<string, object> resolvedVariables)
    {
        if (string.IsNullOrEmpty(varDef.Value))
            return varDef.Default ?? false;

        // Check for multi-language detection pattern
        if (varDef.Value.Contains("languages.Count > 1", StringComparison.OrdinalIgnoreCase))
        {
            if (!resolvedVariables.TryGetValue("arrays", out var arraysObj))
                return false;

            if (arraysObj is not IEnumerable<object> arrays)
                return false;

            foreach (var arrayObj in arrays)
            {
                if (arrayObj is JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty("languages", out var languagesProp) &&
                        languagesProp.ValueKind == JsonValueKind.Array)
                    {
                        var count = languagesProp.GetArrayLength();
                        if (count > 1)
                            return true;
                    }
                }
            }

            return false;
        }

        return varDef.Default ?? false;
    }
}
