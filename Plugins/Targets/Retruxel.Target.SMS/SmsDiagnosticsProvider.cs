using Retruxel.Core.Models;
using System.Text.RegularExpressions;

namespace Retruxel.Target.SMS;

/// <summary>
/// Analyzes SMS build output and generates hardware usage diagnostics.
/// Counts tiles, estimates RAM usage, measures ROM size proxy, and tracks module count.
/// </summary>
public class SmsDiagnosticsProvider
{
    public BuildDiagnosticsReport Analyze(BuildDiagnosticInput input)
    {
        var metrics = new List<BuildDiagnosticMetric>();

        // VRAM.Tiles — count unique tiles in generated source files
        var tileCount = CountTiles(input.SourceFiles);
        metrics.Add(new BuildDiagnosticMetric.Builder()
            .WithMetricId("vram.tiles")
            .WithDisplayName("VRAM Tiles")
            .WithCategory("VRAM")
            .WithCurrent(tileCount)
            .WithMax(input.Specs.MaxTilesInVram)
            .WithWarningThreshold(0.80)
            .WithErrorThreshold(1.0)
            .Build());

        // RAM.Variables — estimate RAM usage from static variable declarations
        var ramUsage = EstimateRamUsage(input.SourceFiles);
        metrics.Add(new BuildDiagnosticMetric.Builder()
            .WithMetricId("ram.variables")
            .WithDisplayName("RAM Usage")
            .WithCategory("RAM")
            .WithCurrent(ramUsage)
            .WithMax(input.Specs.RamBytes)
            .WithWarningThreshold(0.75)
            .WithErrorThreshold(0.95)
            .WithDetail("Estimated — actual usage verified at link time")
            .Build());

        // ROM.SourceLines — count total lines of C code as ROM size proxy
        var lineCount = CountSourceLines(input.SourceFiles);
        metrics.Add(new BuildDiagnosticMetric.Builder()
            .WithMetricId("rom.sourcelines")
            .WithDisplayName("ROM Source Lines")
            .WithCategory("ROM")
            .WithCurrent(lineCount)
            .WithMax(32768) // Reasonable limit for 32KB ROM
            .WithWarningThreshold(0.70)
            .WithErrorThreshold(0.90)
            .WithDetail("Line count is a proxy — actual ROM size verified at link time")
            .Build());

        // Modules.Count — count distinct modules in the build
        var moduleCount = CountModules(input.SourceFiles);
        metrics.Add(new BuildDiagnosticMetric.Builder()
            .WithMetricId("modules.count")
            .WithDisplayName("Module Count")
            .WithCategory("Modules")
            .WithCurrent(moduleCount)
            .WithMax(10) // Reasonable limit to avoid RAM overload
            .WithWarningThreshold(0.80)
            .WithErrorThreshold(1.0)
            .Build());

        return new BuildDiagnosticsReport(metrics);
    }

    /// <summary>
    /// Counts tiles in generated source files.
    /// Each 32 bytes = 1 tile (SMS 4bpp planar 8×8).
    /// Looks for "const unsigned char" array declarations in tilemap/sprite modules.
    /// </summary>
    private int CountTiles(IReadOnlyList<GeneratedFile> sourceFiles)
    {
        try
        {
            var totalBytes = 0;

            // Look for tile data in tilemap and sprite modules
            var tileFiles = sourceFiles.Where(f =>
                f.SourceModuleId.Contains("tilemap", StringComparison.OrdinalIgnoreCase) ||
                f.SourceModuleId.Contains("sprite", StringComparison.OrdinalIgnoreCase));

            foreach (var file in tileFiles)
            {
                // Match: const unsigned char name[] = { 0x00, 0x01, ... };
                var matches = Regex.Matches(file.Content, @"const\s+unsigned\s+char\s+\w+\[\]\s*=\s*\{([^}]+)\}");
                
                foreach (Match match in matches)
                {
                    // Count hex values (0xNN)
                    var hexValues = Regex.Matches(match.Groups[1].Value, @"0x[0-9A-Fa-f]{2}");
                    totalBytes += hexValues.Count;
                }
            }

            // SMS tiles are 32 bytes each (4bpp planar, 8×8 pixels)
            return totalBytes / 32;
        }
        catch
        {
            return 0; // If parsing fails, return 0
        }
    }

    /// <summary>
    /// Estimates RAM usage by counting static variable declarations.
    /// This is a rough proxy — actual usage is verified at link time.
    /// </summary>
    private int EstimateRamUsage(IReadOnlyList<GeneratedFile> sourceFiles)
    {
        try
        {
            var staticCount = 0;

            foreach (var file in sourceFiles)
            {
                // Count "static" declarations as proxy for RAM usage
                staticCount += Regex.Matches(file.Content, @"\bstatic\s+").Count;
            }

            // Rough estimate: 16 bytes per static variable on average
            return staticCount * 16;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Counts total lines of C code (excluding comments and empty lines).
    /// Used as a proxy for ROM size.
    /// </summary>
    private int CountSourceLines(IReadOnlyList<GeneratedFile> sourceFiles)
    {
        try
        {
            var totalLines = 0;

            foreach (var file in sourceFiles)
            {
                var lines = file.Content.Split('\n');
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    // Skip empty lines and comment-only lines
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//"))
                        continue;
                    totalLines++;
                }
            }

            return totalLines;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Counts distinct modules in the build (excluding retruxel.core).
    /// </summary>
    private int CountModules(IReadOnlyList<GeneratedFile> sourceFiles)
    {
        try
        {
            return sourceFiles
                .Select(f => f.SourceModuleId)
                .Where(id => id != "retruxel.core" && !string.IsNullOrEmpty(id))
                .Distinct()
                .Count();
        }
        catch
        {
            return 0;
        }
    }
}
