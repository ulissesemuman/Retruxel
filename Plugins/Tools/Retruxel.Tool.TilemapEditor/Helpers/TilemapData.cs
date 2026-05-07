using Retruxel.Core.Models;
using System;
using System.Collections.Generic;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Manages tilemap layer data and operations.
/// </summary>
public class TilemapData
{
    private List<TileEntry[]> _layers = new();
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;
    public int LayerCount => _layers.Count;

    public void Initialize(int width, int height, int layerCount)
    {
        _width = width;
        _height = height;
        _layers.Clear();

        for (int i = 0; i < layerCount; i++)
        {
            var layer = new TileEntry[width * height];
            for (int j = 0; j < layer.Length; j++)
                layer[j] = TileEntry.Empty;
            _layers.Add(layer);
        }
    }

    public TileEntry[] GetLayer(int index) => _layers[index];

    public void SetTile(int layerIndex, int x, int y, TileEntry entry)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;

        int index = y * _width + x;
        _layers[layerIndex][index] = entry.Clone();
    }

    // Backward-compat overload for simple tile placement (no flip)
    public void SetTile(int layerIndex, int x, int y, int tileIndex)
        => SetTile(layerIndex, x, y, new TileEntry { TileIndex = tileIndex });

    public TileEntry GetTile(int layerIndex, int x, int y)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return TileEntry.Empty;
        if (x < 0 || x >= _width || y < 0 || y >= _height) return TileEntry.Empty;

        int index = y * _width + x;
        return _layers[layerIndex][index];
    }

    public int GetTileIndex(int layerIndex, int x, int y)
        => GetTile(layerIndex, x, y).TileIndex;

    public void ClearLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        for (int i = 0; i < _layers[layerIndex].Length; i++)
            _layers[layerIndex][i] = TileEntry.Empty;
    }

    public void FillLayer(int layerIndex, TileEntry entry)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        for (int i = 0; i < _layers[layerIndex].Length; i++)
            _layers[layerIndex][i] = entry.Clone();
    }

    // Backward-compat overload
    public void FillLayer(int layerIndex, int tileIndex)
        => FillLayer(layerIndex, new TileEntry { TileIndex = tileIndex });

    public void Resize(int newWidth, int newHeight)
    {
        int newSize = newWidth * newHeight;

        for (int i = 0; i < _layers.Count; i++)
        {
            var newLayer = new TileEntry[newSize];
            for (int j = 0; j < newLayer.Length; j++)
                newLayer[j] = TileEntry.Empty;

            // Copy existing data
            for (int y = 0; y < Math.Min(_height, newHeight); y++)
            {
                for (int x = 0; x < Math.Min(_width, newWidth); x++)
                {
                    int oldIndex = y * _width + x;
                    int newIndex = y * newWidth + x;
                    newLayer[newIndex] = _layers[i][oldIndex].Clone();
                }
            }

            _layers[i] = newLayer;
        }

        _width = newWidth;
        _height = newHeight;
    }
}
