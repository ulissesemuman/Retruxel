using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Discovers and loads CodeGen manifests from the plugins/CodeGens/ folder.
/// Handles manifest parsing and validation.
/// </summary>
internal static class CodeGenDiscovery
{
    public static Dictionary<string, CodeGenManifest> DiscoverCodeGens(string pluginsPath, IProgress<string>? progress = null)
    {
        var result = new Dictionary<string, CodeGenManifest>(StringComparer.OrdinalIgnoreCase);
        var codeGensDir = Path.Combine(pluginsPath, "CodeGens");

        if (!Directory.Exists(codeGensDir))
            return result;

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
