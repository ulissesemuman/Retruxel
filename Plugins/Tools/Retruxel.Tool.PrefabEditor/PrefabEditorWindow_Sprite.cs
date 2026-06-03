using Retruxel.Core.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.PrefabEditor;

public partial class PrefabEditorWindow
{
    private void PopulateSprite()
    {
        // Asset dropdown
        CmbAsset.Items.Clear();
        CmbAsset.Items.Add(new ComboBoxItem { Content = "— none —", Tag = string.Empty });

        foreach (var asset in _project.Assets)
        {
            CmbAsset.Items.Add(new ComboBoxItem
            {
                Content = asset.FileName,
                Tag     = asset.Id
            });
        }

        // Select current asset
        var selectedIndex = 0;
        for (int i = 1; i < CmbAsset.Items.Count; i++)
        {
            if (CmbAsset.Items[i] is ComboBoxItem item && item.Tag as string == _prefab.SpriteAssetId)
            {
                selectedIndex = i;
                break;
            }
        }
        CmbAsset.SelectedIndex = selectedIndex;

        // Palette slots
        CmbPaletteSlot.Items.Clear();
        int slotCount = _target.GetPaletteSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            var slotType = _target.GetPaletteSlotType(i);
            CmbPaletteSlot.Items.Add(new ComboBoxItem
            {
                Content = $"Slot {i} — {slotType}",
                Tag     = i
            });
        }
        CmbPaletteSlot.SelectedIndex = Math.Clamp(_prefab.PaletteSlot, 0, slotCount - 1);

        // Dimensions
        TxtWidthTiles.Text  = _prefab.WidthTiles.ToString();
        TxtHeightTiles.Text = _prefab.HeightTiles.ToString();

        RefreshSpritePreview();
    }

    private void CmbAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbAsset.SelectedItem is ComboBoxItem item)
        {
            _prefab.SpriteAssetId = item.Tag as string ?? string.Empty;
            RefreshSpritePreview();
        }
    }

    private void CmbPaletteSlot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPaletteSlot.SelectedItem is ComboBoxItem item && item.Tag is int slot)
        {
            _prefab.PaletteSlot = slot;
            RefreshSpritePreview();
        }
    }

    private void BtnOpenSpriteEditor_Click(object sender, RoutedEventArgs e)
    {
        var scene = _project.Scenes.FirstOrDefault();

        var changed = Retruxel.Services.VisualToolInvoker.OpenSpriteEditorForPrefab(
            _prefab, _target, _project, _project.ProjectPath, scene);

        if (changed)
        {
            PopulateSprite();
            ValidateAll();
        }
    }

    private void RefreshSpritePreview()
    {
        if (string.IsNullOrEmpty(_prefab.SpriteAssetId))
        {
            ImgSpritePreview.Source  = null;
            TxtNoSprite.Visibility   = Visibility.Visible;
            return;
        }

        var asset = _project.Assets.FirstOrDefault(a => a.Id == _prefab.SpriteAssetId);
        if (asset?.GenerationParams?.MapIndex is not { Length: > 0 } mapIndex)
        {
            ImgSpritePreview.Source  = null;
            TxtNoSprite.Visibility   = Visibility.Visible;
            return;
        }

        TxtNoSprite.Visibility = Visibility.Collapsed;

        // Resolve palette colors from the first scene that has palette slots,
        // or fall back to a greyscale ramp if none exist
        var paletteColors = _project.Scenes
            .SelectMany(s => s.PaletteSlots)
            .FirstOrDefault(s => s.SlotIndex == _prefab.PaletteSlot)
            ?.Colors
            ?? [];

        try
        {
            int w = asset.GenerationParams.OptimizedWidth;
            int h = asset.GenerationParams.OptimizedHeight;
            var bitmap = RenderPreviewBitmap(mapIndex, paletteColors, w, h);
            ImgSpritePreview.Source = bitmap;
        }
        catch
        {
            ImgSpritePreview.Source = null;
        }
    }

    /// <summary>
    /// Renders a MapIndex byte array to a WPF BitmapSource using the given palette.
    /// Mirrors RenderMapIndexToBitmap in SceneEditorView_Preview.
    /// </summary>
    private static BitmapSource RenderPreviewBitmap(
        byte[] mapIndex,
        System.Collections.Generic.IReadOnlyList<string> paletteHex,
        int width, int height)
    {
        var palette = new uint[Math.Max(1, paletteHex.Count)];
        for (int i = 0; i < paletteHex.Count; i++)
        {
            var hex = paletteHex[i].TrimStart('#');
            if (hex.Length >= 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                palette[i] = (uint)((255u << 24) | ((uint)r << 16) | ((uint)g << 8) | b);
            }
        }

        var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        wb.Lock();
        unsafe
        {
            var dst = (uint*)wb.BackBuffer.ToPointer();
            int max = palette.Length - 1;
            for (int i = 0; i < mapIndex.Length && i < width * height; i++)
                dst[i] = palette[Math.Min(mapIndex[i], max)];
        }
        wb.AddDirtyRect(new Int32Rect(0, 0, width, height));
        wb.Unlock();
        wb.Freeze();
        return wb;
    }
}
