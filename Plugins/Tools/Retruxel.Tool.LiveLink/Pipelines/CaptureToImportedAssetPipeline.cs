using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Tool.LiveLink.Services;

namespace Retruxel.Tool.LiveLink.Pipelines;

/// <summary>
/// Converts LiveLink CaptureResult to standardized ImportedAssetData format.
/// </summary>
public class CaptureToImportedAssetPipeline : AssetPipelineBase<CaptureResult, ImportedAssetData>
{
    public override string PipelineId => "livelink.capture_to_imported";
    public override string DisplayName => "LiveLink Capture → Imported Asset";
    public override string Description => "Converts emulator capture data to standardized asset format";

    public override ImportedAssetData ProcessTyped(CaptureResult input, Dictionary<string, object>? options = null)
    {
        var result = new ImportedAssetData
        {
            Tiles = input.Tiles,
            TilemapData = ConvertNametableToTilemapData(input.Nametable),
            Palette = input.Palette,
            TileWidth = input.TileWidth,
            TileHeight = input.TileHeight,
            MapWidth = input.NametableWidth,
            MapHeight = input.NametableHeight,
            SourceTargetId = input.TargetId,
            CapturedAt = DateTime.Now,
            Metadata = new Dictionary<string, object>(input.Metadata)
        };

        // Detect bits per pixel from tile data
        if (input.Tiles.Length > 0 && input.Tiles[0].Length > 0)
        {
            var maxValue = input.Tiles.SelectMany(t => t).Max();
            result.BitsPerPixel = maxValue switch
            {
                <= 1 => 1,   // SG-1000 (2 colors)
                <= 3 => 2,   // NES/GB (4 colors)
                <= 15 => 4,  // SMS/GG (16 colors)
                _ => 8       // SNES/GBA (256 colors)
            };
        }

        // Extract source emulator from options if provided
        if (options?.TryGetValue("sourceEmulator", out var emulator) == true)
        {
            result.SourceEmulator = emulator.ToString() ?? string.Empty;
        }

        // Extract destination target if provided
        if (options?.TryGetValue("destinationTarget", out var destTarget) == true)
        {
            result.DestinationTargetId = destTarget.ToString();
        }

        return result;
    }

    private int[] ConvertNametableToTilemapData(ushort[] nametable)
    {
        // Convert ushort nametable entries to int tilemap data
        // For SMS: nametable entry = tile index (lower 9 bits) + attributes (upper 7 bits)
        // We extract just the tile index for now
        var tilemapData = new int[nametable.Length];
        
        for (int i = 0; i < nametable.Length; i++)
        {
            // Extract tile index (bits 0-8)
            tilemapData[i] = nametable[i] & 0x1FF;
        }

        return tilemapData;
    }
}
