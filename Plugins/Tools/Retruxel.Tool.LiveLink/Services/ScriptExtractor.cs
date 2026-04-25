using System.IO;
using System.Reflection;

namespace Retruxel.Tool.LiveLink.Services;

/// <summary>
/// Extracts embedded Lua scripts to disk for emulator loading.
/// </summary>
public static class ScriptExtractor
{
    private static readonly string ScriptsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Retruxel", "LiveLink", "Scripts");

    public static string GetScriptPath(string emulatorId)
    {
        return emulatorId switch
        {
            "mesen" => ExtractScript("retruxel_mesen.lua"),
            "mesen-s" => ExtractScript("retruxel_mesen_s.lua"),
            "bgb" => ExtractScript("retruxel_bgb.lua"),
            "mgba" => ExtractScript("retruxel_mgba.lua"),
            _ => string.Empty
        };
    }

    private static string ExtractScript(string scriptName)
    {
        Directory.CreateDirectory(ScriptsPath);
        
        var outputPath = Path.Combine(ScriptsPath, scriptName);
        
        // Always extract to ensure latest version
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Retruxel.Tool.LiveLink.Resources.{scriptName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

        using var fileStream = File.Create(outputPath);
        stream.CopyTo(fileStream);

        return outputPath;
    }
}
