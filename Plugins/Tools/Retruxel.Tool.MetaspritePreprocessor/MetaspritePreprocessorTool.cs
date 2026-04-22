using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.MetaspritePreprocessor;

/// <summary>
/// Generic metasprite preprocessor tool.
/// A metasprite is a logical sprite composed of multiple hardware 8×8 tiles
/// arranged at relative X/Y offsets from an origin point (usually top-left of the bounding box).
///
/// This is required for characters like Kung Fu Master where the player and enemies
/// are 16×24 or larger, built from several 8×8 hardware sprites per frame.
///
/// Input:
///   frames  — array of frame definitions, each containing a list of tile entries:
///             { tileIndex, offsetX, offsetY }
///
/// Output (consumed by the .rtrx template):
///   frameCount        — total number of frames
///   tileEntryCount    — total number of tile entries across all frames
///   frameOffsets      — C array: start index in the flat tile entry table per frame
///   frameTileCounts   — C array: number of tiles per frame
///   tileIndices       — C array: tile indices (flat, all frames concatenated)
///   offsetsX          — C array: X offsets (flat, all frames concatenated)
///   offsetsY          — C array: Y offsets (flat, all frames concatenated)
///   maxTilesPerFrame  — max tiles in any single frame (used to size SAT budget warnings)
/// </summary>
public class MetaspritePreprocessorTool : ITool
{
    public string ToolId      => "metasprite_preprocessor";
    public string DisplayName => "Metasprite Preprocessor";
    public string Description => "Processes multi-tile sprite frames and generates flat C arrays with per-frame offsets";
    public string Category    => "Graphics";
    public string? TargetId   => null;
    public object? Icon       => null;
    public string? Shortcut   => null;
    public bool IsStandalone  => false;
    public bool RequiresProject => false;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var frames = GetFrames(input);

        var frameOffsetsList    = new List<int>();
        var frameTileCountsList = new List<int>();
        var tileIndicesList     = new List<int>();
        var offsetsXList        = new List<int>();
        var offsetsYList        = new List<int>();

        int cursor = 0;
        int maxTilesPerFrame = 0;

        foreach (var frame in frames)
        {
            frameOffsetsList.Add(cursor);
            frameTileCountsList.Add(frame.Count);

            if (frame.Count > maxTilesPerFrame)
                maxTilesPerFrame = frame.Count;

            foreach (var entry in frame)
            {
                tileIndicesList.Add(entry.TileIndex);
                offsetsXList.Add(entry.OffsetX);
                offsetsYList.Add(entry.OffsetY);
                cursor++;
            }
        }

        return new Dictionary<string, object>
        {
            ["frameCount"]       = frames.Count,
            ["tileEntryCount"]   = cursor,
            ["maxTilesPerFrame"] = maxTilesPerFrame,

            // C array strings — ready to drop into .rtrx templates
            ["frameOffsets"]    = FormatIntArray(frameOffsetsList),
            ["frameTileCounts"] = FormatIntArray(frameTileCountsList),
            ["tileIndices"]     = FormatIntArray(tileIndicesList),
            ["offsetsX"]        = FormatIntArray(offsetsXList),
            ["offsetsY"]        = FormatIntArray(offsetsYList)
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses frames from input. Each frame is an array of tile entries.
    /// Accepts two formats:
    ///   1. object[][] — already parsed (coming from module JSON via ModuleRenderer)
    ///   2. Dictionary<string,object>[] per entry with keys tileIndex, offsetX, offsetY
    /// </summary>
    private static List<List<TileEntry>> GetFrames(Dictionary<string, object> input)
    {
        var result = new List<List<TileEntry>>();

        if (!input.TryGetValue("frames", out var framesObj) || framesObj is not object[] rawFrames)
            return result;

        foreach (var rawFrame in rawFrames)
        {
            var frameEntries = new List<TileEntry>();

            if (rawFrame is not object[] rawTiles)
            {
                result.Add(frameEntries);
                continue;
            }

            foreach (var rawTile in rawTiles)
            {
                if (rawTile is not Dictionary<string, object> dict)
                    continue;

                frameEntries.Add(new TileEntry
                {
                    TileIndex = GetInt(dict, "tileIndex"),
                    OffsetX   = GetInt(dict, "offsetX"),
                    OffsetY   = GetInt(dict, "offsetY")
                });
            }

            result.Add(frameEntries);
        }

        return result;
    }

    private static string FormatIntArray(List<int> values)
        => string.Join(", ", values);

    private static int GetInt(Dictionary<string, object> dict, string key, int def = 0)
    {
        if (!dict.TryGetValue(key, out var v)) return def;
        return v switch
        {
            int i    => i,
            long l   => (int)l,
            double d => (int)d,
            _        => def
        };
    }

    private class TileEntry
    {
        public int TileIndex { get; set; }
        public int OffsetX   { get; set; }
        public int OffsetY   { get; set; }
    }
}
