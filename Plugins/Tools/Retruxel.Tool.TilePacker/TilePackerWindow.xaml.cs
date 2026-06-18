using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Lib.WPFImageProcessing;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Retruxel.Tool.TilePacker;

public partial class TilePackerWindow : Window
{
    private readonly byte[] _indexMap;
    private readonly int _imageWidth;
    private readonly int _imageHeight;
    private readonly int _tileWidth;
    private readonly int _tileHeight;
    private readonly IReadOnlyList<HardwareColor> _palette;

    private readonly TilePackerTool _packer = new();

    public TilePackResult? Result { get; private set; }

    public TilePackerWindow(
        byte[] indexMap,
        int imageWidth,
        int imageHeight,
        int tileWidth,
        int tileHeight,
        IReadOnlyList<HardwareColor> palette,
        string? assetLabel = null)
    {
        _indexMap    = indexMap;
        _imageWidth  = imageWidth;
        _imageHeight = imageHeight;
        _tileWidth   = tileWidth;
        _tileHeight  = tileHeight;
        _palette     = palette;

        InitializeComponent();

        if (!string.IsNullOrEmpty(assetLabel))
            TxtSubtitle.Text = assetLabel;

        RunPackAndRefresh();
    }

    private void PackOptions_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        RunPackAndRefresh();
    }

    private void RunPackAndRefresh()
    {
        var input = new Dictionary<string, object>
        {
            ["indexMap"]       = _indexMap,
            ["imageWidth"]     = _imageWidth,
            ["imageHeight"]    = _imageHeight,
            ["tileWidth"]      = _tileWidth,
            ["tileHeight"]     = _tileHeight,
            ["enableFlipH"]    = ChkFlipH.IsChecked == true,
            ["enableFlipV"]    = ChkFlipV.IsChecked == true,
            ["enableRotation"] = ChkRotation.IsChecked == true
        };

        var output = _packer.Execute(input);
        Result = output["result"] as TilePackResult;
        if (Result is null) return;

        RefreshPreviews(Result);
        RefreshStats(Result);
        RefreshMapping(Result);
    }

    private void RefreshPreviews(TilePackResult result)
    {
        var originalBitmap = IndexedBitmapRenderer.Render(
            _indexMap, _palette, _imageWidth, _imageHeight);
        ImgOriginal.Source = ImageProcessing.ConvertSkBitmapToBitmapSource(originalBitmap);

        var optimizedMap = TilePackerPreviewBuilder.BuildOptimizedMapIndex(
            result.UniqueTiles, _tileWidth, _tileHeight);

        if (optimizedMap.Length > 0)
        {
            var optimizedBitmap = IndexedBitmapRenderer.Render(
                optimizedMap, _palette, _tileWidth, result.UniqueTiles.Count * _tileHeight);
            ImgOptimized.Source = ImageProcessing.ConvertSkBitmapToBitmapSource(optimizedBitmap);
        }
        else
        {
            ImgOptimized.Source = null;
        }
    }

    private void RefreshStats(TilePackResult result)
    {
        int saved    = result.OriginalTileCount - result.OptimizedTileCount;
        double savedPct = result.OriginalTileCount > 0
            ? (1.0 - result.CompressionRatio) * 100.0
            : 0;

        TxtStats.Text =
            $"Original: {result.OriginalTileCount}  ·  " +
            $"Unique: {result.OptimizedTileCount}  ·  " +
            $"Saved: {saved} ({savedPct:F1}%)";
    }

    private void RefreshMapping(TilePackResult result)
    {
        int tilesPerRow = _imageWidth / _tileWidth;
        GridMapping.ItemsSource = TilePackerPreviewBuilder.BuildMappingRows(result, tilesPerRow);
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = Result is not null;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
