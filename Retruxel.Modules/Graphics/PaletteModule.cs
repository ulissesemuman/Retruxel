using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Palette module — loads BG and sprite color palettes into the VDP.
/// The SMS VDP supports 2 palettes of 16 colors each.
/// Each color is stored as a 6-bit SMS register value (2 bits per R/G/B channel).
///
/// SMS color format byte: 0b00BBGGRR
///   RR = red   (0-3 → 0, 85, 170, 255)
///   GG = green (0-3 → 0, 85, 170, 255)
///   BB = blue  (0-3 → 0, 85, 170, 255)
///
/// JSON format:
/// {
///   "module": "sms.palette",
///   "bgColors":     [0,0,0,21,42,63, ...],   // 16 SMS color bytes for BG
///   "spriteColors": [0,0,0,21,42,63, ...]    // 16 SMS color bytes for sprites
/// }
/// </summary>
public class PaletteModule : ILogicModule
{
    public string ModuleId    => "sms.palette";
    public string DisplayName => "Palette";
    public string Category    => "Graphics";
    public ModuleType Type    => ModuleType.Logic;
    public string[] Compatibility => ["sms", "gg"];

    // 16 SMS color register values (0x00–0x3F) for background tiles
    public byte[] BgColors     { get; set; } = new byte[16];

    // 16 SMS color register values (0x00–0x3F) for sprites
    public byte[] SpriteColors { get; set; } = new byte[16];

    public PaletteModule()
    {
        // Default: black background, white foreground
        BgColors[0]     = 0x00; // transparent / black
        BgColors[1]     = 0x3F; // white
        SpriteColors[0] = 0x00;
        SpriteColors[1] = 0x3F;
    }

    public ModuleManifest GetManifest() => new()
    {
        ModuleId      = ModuleId,
        Version       = "1.0.0",
        Type          = ModuleType.Logic,
        Compatibility = Compatibility,
        Parameters    =
        [
            new ParameterDefinition
            {
                Name         = "bgColors",
                DisplayName  = "BG Palette",
                Description  = "16 SMS color register values (0x00–0x3F) for background tiles.",
                Type         = ParameterType.String,
                DefaultValue = "0,63,0,0,0,0,0,0,0,0,0,0,0,0,0,0"
            },
            new ParameterDefinition
            {
                Name         = "spriteColors",
                DisplayName  = "Sprite Palette",
                Description  = "16 SMS color register values (0x00–0x3F) for sprites.",
                Type         = ParameterType.String,
                DefaultValue = "0,63,0,0,0,0,0,0,0,0,0,0,0,0,0,0"
            }
        ]
    };

    // Code generation delegated to the target translator
    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()
    {
        var data = new
        {
            module       = ModuleId,
            bgColors     = BgColors,
            spriteColors = SpriteColors
        };
        return JsonSerializer.Serialize(data);
    }

    public void Deserialize(string json)
    {
        var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("bgColors", out var bg))
            BgColors = bg.EnumerateArray()
                         .Select(e => e.GetByte())
                         .ToArray();

        if (root.TryGetProperty("spriteColors", out var sp))
            SpriteColors = sp.EnumerateArray()
                             .Select(e => e.GetByte())
                             .ToArray();
    }

    public string GetValidationSample() => Serialize();
}
