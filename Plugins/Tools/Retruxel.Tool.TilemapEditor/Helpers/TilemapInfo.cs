using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Manages plane layer data using a sparse dictionary.
/// Tiles are stored by (x, y) coordinate — only non-empty cells consume memory.
/// This supports unbounded map editing: the user can place tiles at any position
/// within the hardware limits (MaxWidth × MaxHeight from PlaneSpecs).
///
/// Width and Height are computed from the bounding box of placed tiles,
/// not from a fixed allocation. They represent the minimum rectangle
/// that encloses all non-empty tiles in the layer.
///
/// Internal to TilemapEditor — not shared with other tools.
/// </summary>
public class PlaneInfo
{
    /// <summary>
    /// Sparse tile storage per layer: layerIndex → { (x,y) → TileEntry }
    /// Only non-empty tiles are stored.
    /// </summary>
    private List<Dictionary<(int x, int y), TileEntry>> _layers = new();

    private int _layerCount;

    // ── Public geometry ───────────────────────────────────────────────────────

    /// <summary>
    /// Bounding box width in tiles across all layers.
    /// Computed from the rightmost non-empty tile + 1.
    /// Returns 0 when all layers are empty.
    /// </summary>
    public int Width => ComputeExtentX();

    /// <summary>
    /// Bounding box height in tiles across all layers.
    /// Computed from the bottommost non-empty tile + 1.
    /// Returns 0 when all layers are empty.
    /// </summary>
    public int Height => ComputeExtentY();

    public int LayerCount => _layerCount;

    // ── Initialization ────────────────────────────────────────────────────────

    public void Initialize(int layerCount)
    {
        _layerCount = layerCount;
        _layers.Clear();
        for (int i = 0; i < layerCount; i++)
            _layers.Add(new Dictionary<(int, int), TileEntry>());
    }

    // ── Tile access ───────────────────────────────────────────────────────────

    public TileEntry GetTile(int layerIndex, int x, int y)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return TileEntry.Empty;
        return _layers[layerIndex].TryGetValue((x, y), out var entry) ? entry : TileEntry.Empty;
    }

    public int GetTileIndex(int layerIndex, int x, int y)
        => GetTile(layerIndex, x, y).TileIndex;

    public void SetTile(int layerIndex, int x, int y, TileEntry entry)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        if (x < 0 || y < 0) return;

        if (entry.IsEmpty)
            _layers[layerIndex].Remove((x, y));
        else
            _layers[layerIndex][(x, y)] = entry.Clone();
    }

    public void SetTile(int layerIndex, int x, int y, int tileIndex)
        => SetTile(layerIndex, x, y, new TileEntry { TileIndex = tileIndex });

    // ── Layer operations ──────────────────────────────────────────────────────

    public void ClearLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        _layers[layerIndex].Clear();
    }

    public void FillLayer(int layerIndex, TileEntry entry, int width, int height)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        _layers[layerIndex].Clear();

        if (entry.IsEmpty) return;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _layers[layerIndex][(x, y)] = entry.Clone();
    }

    public void FillLayer(int layerIndex, int tileIndex, int width, int height)
        => FillLayer(layerIndex, new TileEntry { TileIndex = tileIndex }, width, height);

    // ── Layer snapshot for codegen/save ──────────────────────────────────────

    /// <summary>
    /// Returns a flat row-major TileEntry array of the bounding box for the given layer.
    /// Empty positions within the bounding box are filled with TileEntry.Empty.
    /// This is the format expected by the build pipeline and JSON serializer.
    /// </summary>
    public TileEntry[] GetLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return Array.Empty<TileEntry>();

        int w = Width;
        int h = Height;
        if (w == 0 || h == 0) return Array.Empty<TileEntry>();

        var result = new TileEntry[w * h];
        for (int i = 0; i < result.Length; i++)
            result[i] = TileEntry.Empty;

        foreach (var kv in _layers[layerIndex])
        {
            int x = kv.Key.x;
            int y = kv.Key.y;
            if (x < w && y < h)
                result[y * w + x] = kv.Value.Clone();
        }

        return result;
    }

    /// <summary>
    /// Returns all non-empty tile positions in a layer as a sparse list.
    /// Useful for rendering (only iterate what is placed).
    /// </summary>
    public IEnumerable<(int x, int y, TileEntry entry)> GetLayerSparse(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) yield break;
        foreach (var kv in _layers[layerIndex])
            yield return (kv.Key.x, kv.Key.y, kv.Value);
    }

    // ── Load from flat array (backward compat) ────────────────────────────────

    /// <summary>
    /// Loads a layer from a flat row-major array with the given dimensions.
    /// Used when deserializing existing saved planes.
    /// </summary>
    public void LoadLayer(int layerIndex, TileEntry[] tiles, int width, int height)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        _layers[layerIndex].Clear();

        int count = Math.Min(tiles.Length, width * height);
        for (int i = 0; i < count; i++)
        {
            if (tiles[i].IsEmpty) continue;
            int x = i % width;
            int y = i / width;
            _layers[layerIndex][(x, y)] = tiles[i].Clone();
        }
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    private int ComputeExtentX()
    {
        int max = -1;
        foreach (var layer in _layers)
            foreach (var key in layer.Keys)
                if (key.x > max) max = key.x;
        return max + 1;
    }

    private int ComputeExtentY()
    {
        int max = -1;
        foreach (var layer in _layers)
            foreach (var key in layer.Keys)
                if (key.y > max) max = key.y;
        return max + 1;
    }

    /// <summary>
    /// Returns the bounding box of non-empty tiles in a specific layer.
    /// Returns (0,0,0,0) if the layer is empty.
    /// </summary>
    public (int minX, int minY, int maxX, int maxY) GetLayerBounds(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count || _layers[layerIndex].Count == 0)
            return (0, 0, 0, 0);

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var key in _layers[layerIndex].Keys)
        {
            if (key.x < minX) minX = key.x;
            if (key.y < minY) minY = key.y;
            if (key.x > maxX) maxX = key.x;
            if (key.y > maxY) maxY = key.y;
        }

        return (minX, minY, maxX, maxY);
    }
}
