using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Modules.Text;

namespace Retruxel.Target.NES.Modules.Text;

/// <summary>
/// Code generator for TextDisplayModule on NES target.
/// Uses neslib (cc65) functions: vram_adr() and vram_put().
/// </summary>
public class NesTextDisplayCodeGen
{
    public static List<GeneratedFile> GenerateCode(TextDisplayModule module, string trigger)
    {
        var files = new List<GeneratedFile>();

        // Validate coordinates for NES (256x240 / 8x8 = 32x30 tiles)
        if (module.X < 0 || module.X > 31)
            throw new ArgumentException($"X position {module.X} out of range (0-31)");
        if (module.Y < 0 || module.Y > 29)
            throw new ArgumentException($"Y position {module.Y} out of range (0-29)");

        // Generate C code using neslib
        var code = $$"""
            // Text Display Module - NES
            // Position: ({{module.X}}, {{module.Y}})
            // Text: "{{module.Text}}"
            
            void text_display_{{module.X}}_{{module.Y}}(void) {
                // Set VRAM address for nametable position
                vram_adr(NTADR_A({{module.X}}, {{module.Y}}));
                
                // Write text string to VRAM
                vram_put("{{EscapeString(module.Text)}}");
            }
            """;

        files.Add(new GeneratedFile
        {
            FileName = $"text_display_{module.X}_{module.Y}.c",
            Content = code,
            FileType = GeneratedFileType.Source,
            SourceModuleId = "text.display"
        });

        return files;
    }

    private static string EscapeString(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
