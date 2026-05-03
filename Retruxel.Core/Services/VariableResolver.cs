using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Resolves variables from different sources (module, project, scene, tool, settings).
/// Handles variable transformation and filtering.
/// </summary>
internal class VariableResolver
{
    private readonly Dictionary<string, ITool> _tools;
    private readonly System.Reflection.Assembly? _targetAssembly;

    public VariableResolver(Dictionary<string, ITool> tools, System.Reflection.Assembly? targetAssembly)
    {
        _tools = tools;
        _targetAssembly = targetAssembly;
    }

    public Dictionary<string, object> ResolveForModule(
        Dictionary<string, VariableDefinition> variables,
        string moduleJson,
        string? projectPath = null)
    {
        var result = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(projectPath))
            result["projectPath"] = projectPath;

        using var doc = JsonDocument.Parse(moduleJson);
        var root = doc.RootElement;

        foreach (var (varName, varDef) in variables)
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
                    result[varName] = toolValues;
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
            input["projectPath"] = projectPathObj;

        IToolExtension? extension = null;
        if (tool.TargetExtensionId is not null && _targetAssembly is not null)
        {
            extension = FindToolExtension(_targetAssembly, tool.TargetExtensionId);
            if (extension is not null)
            {
                foreach (var (k, v) in extension.GetDefaultParameters())
                    input[k] = v;
            }
        }

        if (varDef.ToolInput is not null)
        {
            foreach (var (inputKey, valueSource) in varDef.ToolInput)
            {
                if (resolvedVariables.ContainsKey(valueSource))
                {
                    input[inputKey] = resolvedVariables[valueSource];
                }
                else if (moduleRoot.TryGetProperty(valueSource, out var v))
                {
                    input[inputKey] = v.ValueKind switch
                    {
                        JsonValueKind.Number => v.TryGetInt32(out var i) ? (object)i : v.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Array => Helpers.ArrayConversionHelper.ToIntArray(v),
                        _ => v.GetString() ?? ""
                    };
                }
                else
                {
                    input[inputKey] = valueSource;
                }
            }
        }

        Dictionary<string, object> toolResult;
        try
        {
            toolResult = tool.Execute(input);
        }
        catch (Exception ex)
        {
            return new() { ["error"] = ex.Message };
        }

        if (extension is not null)
        {
            var extensionInput = new Dictionary<string, object>(input);
            foreach (var (k, v) in toolResult)
                extensionInput[k] = v;

            var extensionResult = extension.Execute(extensionInput);

            foreach (var (k, v) in extensionResult)
                toolResult[k] = v;
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
}
