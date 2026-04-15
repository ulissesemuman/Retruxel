using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.Text.Json;

namespace Retruxel.CodeGen.TextDisplay.NES;

/// <summary>
/// NES-specific code generator for the text.display module.
/// Uses template-based code generation from text_display.c.rtrx.
/// </summary>
public class NesTextDisplayCodeGen : ICodeGenPlugin
{
    public string TargetId => "nes";
    public string? ModuleId => "text.display";
    public string DisplayName => "Text Display";
    public string Category => "Graphics";
    public bool IsSingleton => false;

    private const int MaxColumns = 32;
    private const int MaxRows = 30;
    private static int _instanceCounter = 0;
    private static string? _templateCache;

    public static void ResetCounter() => _instanceCounter = 0;

    public IEnumerable<GeneratedFile> Generate(string moduleJson)
    {
        using var doc = JsonDocument.Parse(moduleJson);
        var root = doc.RootElement;

        var x = root.TryGetProperty("x", out var xProp) ? xProp.GetInt32() : 0;
        var y = root.TryGetProperty("y", out var yProp) ? yProp.GetInt32() : 0;
        var text = root.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? "" : "";
        var instanceId = _instanceCounter++;

        // Load template once and cache
        _templateCache ??= LoadTemplate();

        // Convert ASCII to tile indices (subtract 0x20)
        var tileData = string.Join(", ", text.Select(c => $"0x{((byte)c - 0x20):X2}"));

        var variables = new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["x"] = x,
            ["y"] = y,
            ["text"] = text,
            ["tileData"] = tileData,
            ["length"] = text.Length
        };

        yield return new GeneratedFile
        {
            FileName = $"text_display_{instanceId}.h",
            FileType = GeneratedFileType.Header,
            SourceModuleId = "text.display",
            Content = TemplateEngine.RenderBlock(_templateCache, "header", variables)
        };

        yield return new GeneratedFile
        {
            FileName = $"text_display_{instanceId}.c",
            FileType = GeneratedFileType.Source,
            SourceModuleId = "text.display",
            Content = TemplateEngine.RenderBlock(_templateCache, "source", variables)
        };
    }

    public IEnumerable<string> Validate(string moduleJson)
    {
        using var doc = JsonDocument.Parse(moduleJson);
        var root = doc.RootElement;

        var x = root.TryGetProperty("x", out var xProp) ? xProp.GetInt32() : 0;
        var y = root.TryGetProperty("y", out var yProp) ? yProp.GetInt32() : 0;
        var text = root.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? "" : "";

        if (x < 0 || x >= MaxColumns)
            yield return $"text.display: x={x} is out of range (0–{MaxColumns - 1})";

        if (y < 0 || y >= MaxRows)
            yield return $"text.display: y={y} is out of range (0–{MaxRows - 1})";

        if (x + text.Length > MaxColumns)
            yield return $"text.display: text overflows screen width (x={x} + length={text.Length} > {MaxColumns})";
    }

    private static string LoadTemplate()
    {
        // Try embedded resource first
        var assembly = typeof(NesTextDisplayCodeGen).Assembly;
        var resourceName = "Retruxel.CodeGen.TextDisplay.NES.Templates.text_display.c.rtrx";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // Fallback: load from file system (development mode)
        var assemblyPath = Path.GetDirectoryName(assembly.Location)!;
        var templatePath = Path.Combine(assemblyPath, "Templates", "text_display.c.rtrx");
        
        if (File.Exists(templatePath))
            return File.ReadAllText(templatePath);

        throw new FileNotFoundException($"Template not found: {resourceName}");
    }
}
