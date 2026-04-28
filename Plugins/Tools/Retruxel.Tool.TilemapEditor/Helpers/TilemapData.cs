using System;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Manages tilemap layer data and operations.
/// </summary>
public class TilemapData
{
    private List<int[]> _layers = new();
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
            var layer = new int[width * height];
            Array.Fill(layer, -1);
            _layers.Add(layer);
        }
    }

    public int[] GetLayer(int index) => _layers[index];

    public void SetTile(int layerIndex, int x, int y, int tileId)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;

        int index = y * _width + x;
        _layers[layerIndex][index] = tileId;
    }

    public int GetTile(int layerIndex, int x, int y)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return -1;
        if (x < 0 || x >= _width || y < 0 || y >= _height) return -1;

        int index = y * _width + x;
        return _layers[layerIndex][index];
    }

    public void ClearLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        Array.Fill(_layers[layerIndex], -1);
    }

    public void FillLayer(int layerIndex, int tileId)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        Array.Fill(_layers[layerIndex], tileId);
    }

    public void Resize(int newWidth, int newHeight)
    {
        int newSize = newWidth * newHeight;

        for (int i = 0; i < _layers.Count; i++)
        {
            var newLayer = new int[newSize];
            Array.Fill(newLayer, -1);

            // Copy existing data
            for (int y = 0; y < Math.Min(_height, newHeight); y++)
            {
                for (int x = 0; x < Math.Min(_width, newWidth); x++)
                {
                    int oldIndex = y * _width + x;
                    int newIndex = y * newWidth + x;
                    newLayer[newIndex] = _layers[i][oldIndex];
                }
            }

            _layers[i] = newLayer;
        }

        _width = newWidth;
        _height = newHeight;
    }
}
