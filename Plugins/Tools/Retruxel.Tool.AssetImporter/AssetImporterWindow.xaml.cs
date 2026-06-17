using Microsoft.Win32;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Lib.WPFImageProcessing;
using Retruxel.Tool.AssetImporter.Services;
using Retruxel.Tool.AssetProcessor;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Retruxel.Lib.ImageProcessing.ColorMatching;
using ToolRegistry = Retruxel.Core.Services.ToolRegistry;

namespace Retruxel.Tool.AssetImporter;

/// <summary>
/// Asset importer window.
/// Allows the user to drop or browse for a PNG, preview the color reduction,
/// name the asset and confirm the import.
///
/// Usage:
///   var window = new AssetImporterWindow(target, projectPath);
///   if (window.ShowDialog() == true)
///       project.Assets.Add(window.ImportedAsset!);
/// </summary>
public partial class AssetImporterWindow : Window
{
    private readonly ITarget _target;
    private readonly string _projectPath;
    private readonly SceneData? _currentScene;
    private readonly List<RadioButton> _planeRadioButtons = new();

    private string? _sourcePngPath;
    private SKBitmap? _reducedPreview;
    private bool _isInitialized = false;
    private int _transparentColorIndex = 0;
    private int _chosenPaletteSlot = 0;
    private List<HardwareColor> _oldColors;

    /// <summary>
    /// The imported asset entry. Only set after a successful import (DialogResult = true).
    /// </summary>
    public AssetEntry? ImportedAsset { get; private set; }

    public AssetImporterWindow(ITarget target, string projectPath, SceneData? currentScene = null)
    {
        InitializeComponent();

        _target = target;
        _projectPath = projectPath;
        _currentScene = currentScene;

        TxtTargetLabel.Text = target.DisplayName.ToUpper();
        GeneratePlaneControls();
        ApplyLocalization();

        _isInitialized = true;
    }

    private void ApplyLocalization()
    {
        var loc = ServiceLocator.Localization;
        TxtTitle.Text = loc.Translate("assetimporter.title");
    }

    /// <summary>
    /// Generates one radio button per hardware plane defined in the target.
    /// Falls back to a single "BG" button if the target has no planes defined.
    /// </summary>
    private void GeneratePlaneControls()
    {
        var planes = _target.Specs.Planes;
        if (planes == null || planes.Length == 0)
        {
            var rb = new RadioButton
            {
                Content = "BG",
                GroupName = "PlaneSelector",
                IsChecked = true,
                Style = (Style)FindResource("SegmentedRadio"),
                Margin = new Thickness(0, 0, 8, 0),
                Tag = "bg"
            };
            _planeRadioButtons.Add(rb);
            RegionSelector.Children.Add(rb);
            return;
        }

        for (int i = 0; i < planes.Length; i++)
        {
            var plane = planes[i];
            var rb = new RadioButton
            {
                Content = plane.Label.ToUpper(),
                GroupName = "PlaneSelector",
                IsChecked = i == 0,
                Style = (Style)FindResource("SegmentedRadio"),
                Margin = new Thickness(0, 0, 8, 0),
                Tag = plane.Id
            };
            _planeRadioButtons.Add(rb);
            RegionSelector.Children.Add(rb);
        }
    }

    /// <summary>Returns the PlaneId of the currently selected plane radio button.</summary>
    private string GetSelectedPlaneId()
    {
        var selected = _planeRadioButtons.FirstOrDefault(rb => rb.IsChecked == true);
        return selected?.Tag as string ?? _target.Specs.Planes.FirstOrDefault()?.Id ?? "bg";
    }

    /// <summary>Pre-selects a specific plane by PlaneId before the window is shown.</summary>
    public void PreSelectPlane(string planeId)
    {
        var rb = _planeRadioButtons.FirstOrDefault(r => r.Tag as string == planeId);
        if (rb != null)
        {
            rb.IsChecked = true;
            foreach (var other in _planeRadioButtons.Where(r => r != rb))
                other.IsChecked = false;
        }
    }

    /// <summary>Backward-compatible overload — maps a legacy regionId to the closest PlaneId.</summary>
    public void PreSelectRegion(string regionId) => PreSelectPlane(regionId);

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Close();

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
        => Close();

    private void RbSource_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        if (RbSourceFile == null || RbSourceEmulator == null || DropHint == null || EmulatorHint == null || DropZone == null) return;

        if (RbSourceFile.IsChecked == true)
        {
            DropHint.Visibility = string.IsNullOrEmpty(_sourcePngPath) ? Visibility.Visible : Visibility.Collapsed;
            EmulatorHint.Visibility = Visibility.Collapsed;
            DropZone.AllowDrop = true;
        }
        else if (RbSourceEmulator.IsChecked == true)
        {
            DropHint.Visibility = Visibility.Collapsed;
            EmulatorHint.Visibility = string.IsNullOrEmpty(_sourcePngPath) ? Visibility.Visible : Visibility.Collapsed;
            DropZone.AllowDrop = false;
        }
    }

    private void BtnCaptureEmulator_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var toolRegistry = (ToolRegistry)ServiceLocator.ToolRegistry;
            var liveLinkTool = toolRegistry.GetTool("retruxel.tool.livelink");

            if (liveLinkTool == null)
            {
                ShowValidation("LiveLink tool not found. Make sure it's installed.");
                return;
            }

            var result = liveLinkTool.Execute(new Dictionary<string, object>
            {
                ["mode"] = "capture",
                ["targetId"] = _target.TargetId
            });

            if (result != null && result.ContainsKey("captureResult"))
                ProcessEmulatorCapture(result["captureResult"]);
        }
        catch (Exception ex)
        {
            ShowValidation($"Emulator capture failed: {ex.Message}");
        }
    }

    private void ProcessEmulatorCapture(object captureData)
    {
        ShowValidation("Emulator capture processing not yet implemented.");
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var loc = ServiceLocator.Localization;
        var dialog = new OpenFileDialog
        {
            Title = loc.Translate("assetimporter.dialog.select_png"),
            Filter = loc.Translate("assetimporter.dialog.png_filter")
        };

        if (dialog.ShowDialog() != true) return;
        LoadSourceImage(dialog.FileName);
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var png = files.FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

        if (png is null)
        {
            ShowValidation(ServiceLocator.Localization.Translate("assetimporter.error.only_png"));
            return;
        }

        LoadSourceImage(png);
    }

    private void LoadSourceImage(string pngPath)
    {
        _reducedPreview?.Dispose();
        _reducedPreview = null;

        try
        {
            using var stream = File.OpenRead(pngPath);
            using var bitmap = SKBitmap.Decode(stream);
            var loc = ServiceLocator.Localization;

            if (bitmap is null) { ShowValidation(loc.Translate("assetimporter.error.decode_failed")); return; }
            if (bitmap.Width % 8 != 0 || bitmap.Height % 8 != 0)
            {
                ShowValidation(string.Format(loc.Translate("assetimporter.error.dimensions"), bitmap.Width, bitmap.Height));
                return;
            }

            _sourcePngPath = pngPath;
            ImgSource.Source = ImageProcessing.ConvertSkBitmapToBitmapSource(LoadBitmapFromPath(pngPath));
            ImgSource.Visibility = Visibility.Visible;
            DropHint.Visibility = Visibility.Collapsed;

            var tileCount = (bitmap.Width / 8) * (bitmap.Height / 8);
            TxtSourceInfo.Text = string.Format(loc.Translate("assetimporter.info.source"), bitmap.Width, bitmap.Height, tileCount);
            TxtTileCount.Text = string.Format(loc.Translate("assetimporter.info.tiles"), tileCount);

            if (string.IsNullOrEmpty(TxtAssetName.Text))
                TxtAssetName.Text = Path.GetFileNameWithoutExtension(pngPath);

            GenerateReducedPreview(pngPath);
            ClearValidation();
            UpdateImportButton();
        }
        catch (Exception ex)
        {
            ShowValidation(string.Format(ServiceLocator.Localization.Translate("assetimporter.error.loading"), ex.Message));
        }
    }

    private void GenerateReducedPreview(string pngPath)
    {
        try
        {
            _reducedPreview?.Dispose();
            _reducedPreview = Services.AssetImporter.ReduceColorsToHardware(pngPath, _target);

            ImgReduced.Source = ImageProcessing.ConvertSkBitmapToBitmapSource(_reducedPreview);
            ImgReduced.Visibility = Visibility.Visible;
            ReducedHint.Visibility = Visibility.Collapsed;

            var uniqueColors = CountUniqueColors(_reducedPreview);
            TxtReducedInfo.Text = string.Format(ServiceLocator.Localization.Translate("assetimporter.info.colors"), uniqueColors, _target.DisplayName);
        }
        catch (Exception ex)
        {
            TxtReducedInfo.Text = string.Format(ServiceLocator.Localization.Translate("assetimporter.error.preview"), ex.Message);
        }
    }

    private void TxtAssetName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ValidateAssetName();
        UpdateImportButton();
    }

    private bool ValidateAssetName()
    {
        var loc = ServiceLocator.Localization;
        var name = TxtAssetName.Text.Trim();

        if (string.IsNullOrEmpty(name)) { ShowValidation(loc.Translate("assetimporter.error.name_empty")); return false; }
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { ShowValidation(loc.Translate("assetimporter.error.name_invalid")); return false; }
        if (name.Contains(' ')) { ShowValidation(loc.Translate("assetimporter.error.name_spaces")); return false; }

        ClearValidation();
        return true;
    }

    private void UpdateImportButton()
        => BtnImport.IsEnabled = _sourcePngPath is not null && ValidateAssetName();

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        if (_sourcePngPath is null || _reducedPreview is null) return;

        var assetName = TxtAssetName.Text.Trim();
        var planeId = GetSelectedPlaneId();

        try
        {
            SKBitmap skBitmap = LoadBitmapFromPath(_sourcePngPath);

            int colorsPerSlot = _target.GetColorsPerSlot();

            var optimizationWindow = new PaletteOptimizationWindow(
                skBitmap,
                colorsPerSlot,
                DistanceMode.LAB,
                _target)
            {
                Owner = this
            };

            if (optimizationWindow.ShowDialog() != true) return;

            var optimizedBitmap = optimizationWindow.OptimizedBitmap;
            var optimizedPalette = optimizationWindow.OptimizedPalette;
            var selectedDiversity = optimizationWindow.SelectedDiversity;
            var colorSpace = optimizationWindow.ColorSpace;
            var palette = optimizedPalette.Select(c => new HardwareColor(c.R, c.G, c.B)).ToList();

            // mapIndex produced by the optimizer (assetColor indices 0..N-1)
            var mapIndex = optimizationWindow.MapIndex;

            // Show palette import dialog and potentially rewrite mapIndex
            if (_currentScene != null)
                mapIndex = ShowPaletteImportDialog(palette, mapIndex, _currentScene, _target);

            // Remap for transparent color swap (ReplaceSlot path)
            if (_transparentColorIndex != 0 && _chosenPaletteSlot != -1)
            {
                var slot = _currentScene!.PaletteSlots[_chosenPaletteSlot];
                var slotColors = slot.Colors.Select(HardwareColor.FromHex).ToList();

                mapIndex = IndexedBitmapRenderer.EncodeFromMapIndex(
                    mapIndex,
                    _oldColors,
                    slotColors,
                    _reducedPreview.Width,
                    _reducedPreview.Height);
            }

            optimizedBitmap.Dispose();

            ImportedAsset = Services.AssetImporter.Import(
                assetName,
                _sourcePngPath,
                _projectPath,
                planeId,
                _target,
                _chosenPaletteSlot,
                mapIndex,
                selectedDiversity,
                colorSpace,
                palette);

            DialogResult = true;
            Close();
        }
        catch (AssetImportException ex)
        {
            ShowValidation(ex.Message);
        }
        catch (Exception ex)
        {
            ShowValidation(string.Format(ServiceLocator.Localization.Translate("assetimporter.error.unexpected"), ex.Message));
        }
    }

    private void ShowValidation(string message) => TxtValidation.Text = message;
    private void ClearValidation() => TxtValidation.Text = string.Empty;

    private static SKBitmap? LoadBitmapFromPath(string path)
    {
        using var stream = File.OpenRead(path);
        return SKBitmap.Decode(stream);
    }

    private static int CountUniqueColors(SKBitmap bitmap)
    {
        var colors = new HashSet<uint>();
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                var p = bitmap.GetPixel(x, y);
                if (p.Alpha > 0) colors.Add((uint)((p.Red << 16) | (p.Green << 8) | p.Blue));
            }
        return colors.Count;
    }

    protected override void OnClosed(EventArgs e)
    {
        _reducedPreview?.Dispose();
        base.OnClosed(e);
    }

    /// <summary>
    /// Shows the palette import dialog. Returns the (possibly remapped) mapIndex.
    /// For MergeSlot: rewrites every pixel index so it points to the correct position
    /// in the merged slot palette.
    /// </summary>
    private byte[] ShowPaletteImportDialog(
        IReadOnlyList<HardwareColor> palette,
        byte[] mapIndex,
        SceneData currentScene,
        ITarget target)
    {
        var hexColors = palette.Select(c => c.ToHex()).ToList();

        var dialog = new PaletteImportDialog(hexColors, currentScene, target) { Owner = this };
        if (dialog.ShowDialog() != true) return mapIndex;

        _transparentColorIndex = dialog.TransparentColorIndex;

        switch (dialog.Result)
        {
            case PaletteImportResult.ReplaceSlot:
            {
                var slot = currentScene.PaletteSlots[dialog.ChosenSlot];
                _oldColors = slot.Colors.Select(HardwareColor.FromHex).ToList();
                _chosenPaletteSlot = slot.SlotIndex;

                slot.Colors.Clear();
                for (int i = 0; i < Math.Min(hexColors.Count, target.GetColorsPerSlot()); i++)
                    slot.Colors.Add(hexColors[i]);

                break;
            }

            case PaletteImportResult.MergeSlot:
            {
                var slot = currentScene.PaletteSlots[dialog.ChosenSlot];
                _chosenPaletteSlot = slot.SlotIndex;

                // Apply merged palette to slot
                slot.Colors.Clear();
                foreach (var c in dialog.MergedColors!)
                    slot.Colors.Add(c);

                // Rewrite mapIndex: each pixel value is an assetColor index → remap to slot index
                var remap = dialog.MergeRemap!;
                var remapped = new byte[mapIndex.Length];
                for (int p = 0; p < mapIndex.Length; p++)
                    remapped[p] = (byte)remap[mapIndex[p]];

                return remapped;
            }

            case PaletteImportResult.KeepCurrent:
                break;
        }

        return mapIndex;
    }
}
