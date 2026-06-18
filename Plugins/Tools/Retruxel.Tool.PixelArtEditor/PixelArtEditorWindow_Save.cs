using Retruxel.Core.Models;
using System;
using System.Windows;

namespace Retruxel.Tool.PixelArtEditor;

public partial class PixelArtEditorWindow
{
    /// <summary>
    /// Writes the current tile pixels back into the asset's MapIndex in memory.
    /// Called after every mouse-up so changes are visible immediately in the tileset preview.
    /// </summary>
    private void WritePixelsBackToAsset()
    {
        if (_currentAsset?.GenerationParams?.MapIndex == null) return;

        var gp   = _currentAsset.GenerationParams;
        int ts   = _tileSize;
        int cols = Math.Max(1, gp.OptimizedWidth / ts);
        int tc   = _selectedTileIndex % cols;
        int tr   = _selectedTileIndex / cols;

        for (int py = 0; py < ts; py++)
        for (int px = 0; px < ts; px++)
        {
            int dstX = tc * ts + px;
            int dstY = tr * ts + py;
            int idx  = dstY * gp.OptimizedWidth + dstX;
            if (idx < gp.MapIndex.Length)
                gp.MapIndex[idx] = _pixels[px, py];
        }

        // Refresh tileset panel to show the edited tile
        RenderTilesetCanvas();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_currentAsset == null)
        {
            MessageBox.Show("No asset loaded.", "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Flush current tile before saving
        WritePixelsBackToAsset();

        SavedAsset = _currentAsset;
        DialogResult = true;
        Close();
    }
}
