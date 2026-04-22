using Microsoft.Win32;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Tool.AssetImporter.Services;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
    private readonly List<RadioButton> _regionRadioButtons = new();

    private string? _sourcePngPath;
    private SKBitmap? _reducedPreview;

    /// <summary>
    /// The imported asset entry. Only set after a successful import (DialogResult = true).
    /// </summary>
    public AssetEntry? ImportedAsset { get; private set; }

    public AssetImporterWindow(ITarget target, string projectPath)
    {
        InitializeComponent();

        _target = target;
        _projectPath = projectPath;

        TxtTargetLabel.Text = target.DisplayName.ToUpper();
        GenerateRegionControls();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        var loc = ServiceLocator.Localization;
        TxtTitle.Text = loc.Translate("assetimporter.title");
        // Labels already set in XAML
    }

    private void GenerateRegionControls()
    {
        var regions = _target.Specs.VramRegions;
        if (regions == null || regions.Length == 0) return;

        for (int i = 0; i < regions.Length; i++)
        {
            var region = regions[i];
            var rb = new RadioButton
            {
                Content = region.Label.ToUpper(),
                GroupName = "VramRegion",
                IsChecked = i == 0,
                Style = (Style)FindResource("SegmentedRadio"),
                Margin = new Thickness(0, 0, 8, 0),
                Tag = region.Id
            };
            _regionRadioButtons.Add(rb);
            RegionSelector.Children.Add(rb);
        }
    }

    private string GetSelectedRegionId()
    {
        var selected = _regionRadioButtons.FirstOrDefault(rb => rb.IsChecked == true);
        return selected?.Tag as string ?? _target.Specs.VramRegions[0].Id;
    }

    /// <summary>Pre-selects a specific VRAM region by ID before the window is shown.</summary>
    public void PreSelectRegion(string regionId)
    {
        var rb = _regionRadioButtons.FirstOrDefault(r => r.Tag as string == regionId);
        if (rb != null)
        {
            rb.IsChecked = true;
            foreach (var other in _regionRadioButtons.Where(r => r != rb))
                other.IsChecked = false;
        }
    }

    // ── Title bar ─────────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Close();

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
        => Close();

    // ── Source selection ──────────────────────────────────────────────────────

    private void RbSource_Changed(object sender, RoutedEventArgs e)
    {
        if (RbSourceFile?.IsChecked == true)
        {
            DropHint.Visibility = _sourcePngPath == null ? Visibility.Visible : Visibility.Collapsed;
            EmulatorHint.Visibility = Visibility.Collapsed;
            DropZone.AllowDrop = true;
        }
        else if (RbSourceEmulator?.IsChecked == true)
        {
            DropHint.Visibility = Visibility.Collapsed;
            EmulatorHint.Visibility = _sourcePngPath == null ? Visibility.Visible : Visibility.Collapsed;
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
            {
                ProcessEmulatorCapture(result["captureResult"]);
            }
        }
        catch (Exception ex)
        {
            ShowValidation($"Emulator capture failed: {ex.Message}");
        }
    }

    private void ProcessEmulatorCapture(object captureData)
    {
        // TODO: Convert CaptureResult to PNG and process
        // For now, show placeholder
        var loc = ServiceLocator.Localization;
        ShowValidation("Emulator capture processing not yet implemented.");
    }

    // ── File selection ────────────────────────────────────────────────────────

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
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var png = files.FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

        if (png is null)
        {
            var loc = ServiceLocator.Localization;
            ShowValidation(loc.Translate("assetimporter.error.only_png"));
            return;
        }

        LoadSourceImage(png);
    }

    // ── Image loading & preview ───────────────────────────────────────────────

    private void LoadSourceImage(string pngPath)
    {
        _reducedPreview?.Dispose();
        _reducedPreview = null;

        try
        {
            // Validate dimensions before preview
            using var stream = File.OpenRead(pngPath);
            using var bitmap = SKBitmap.Decode(stream);

            var loc = ServiceLocator.Localization;

            if (bitmap is null)
            {
                ShowValidation(loc.Translate("assetimporter.error.decode_failed"));
                return;
            }

            if (bitmap.Width % 8 != 0 || bitmap.Height % 8 != 0)
            {
                ShowValidation(string.Format(loc.Translate("assetimporter.error.dimensions"), bitmap.Width, bitmap.Height));
                return;
            }

            // Show source preview
            _sourcePngPath = pngPath;
            ImgSource.Source = LoadBitmapFromPath(pngPath);
            ImgSource.Visibility = Visibility.Visible;
            DropHint.Visibility = Visibility.Collapsed;

            var tileCount = (bitmap.Width / 8) * (bitmap.Height / 8);
            TxtSourceInfo.Text = string.Format(loc.Translate("assetimporter.info.source"), bitmap.Width, bitmap.Height, tileCount);
            TxtTileCount.Text = string.Format(loc.Translate("assetimporter.info.tiles"), tileCount);

            // Auto-fill asset name from filename
            if (string.IsNullOrEmpty(TxtAssetName.Text))
                TxtAssetName.Text = Path.GetFileNameWithoutExtension(pngPath);

            // Generate reduced preview
            GenerateReducedPreview(pngPath);

            ClearValidation();
            UpdateImportButton();
        }
        catch (Exception ex)
        {
            var loc = ServiceLocator.Localization;
            ShowValidation(string.Format(loc.Translate("assetimporter.error.loading"), ex.Message));
        }
    }

    private void GenerateReducedPreview(string pngPath)
    {
        try
        {
            _reducedPreview?.Dispose();
            _reducedPreview = Services.AssetImporter.PreviewReduction(pngPath, _target);

            ImgReduced.Source = SkiaBitmapToWpf(_reducedPreview);
            ImgReduced.Visibility = Visibility.Visible;
            ReducedHint.Visibility = Visibility.Collapsed;

            // Count unique colors in reduced image
            var loc = ServiceLocator.Localization;
            var uniqueColors = CountUniqueColors(_reducedPreview);
            TxtReducedInfo.Text = string.Format(loc.Translate("assetimporter.info.colors"), uniqueColors, _target.DisplayName);
        }
        catch (Exception ex)
        {
            var loc = ServiceLocator.Localization;
            TxtReducedInfo.Text = string.Format(loc.Translate("assetimporter.error.preview"), ex.Message);
        }
    }

    // ── Asset name validation ─────────────────────────────────────────────────

    private void TxtAssetName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ValidateAssetName();
        UpdateImportButton();
    }

    private bool ValidateAssetName()
    {
        var loc = ServiceLocator.Localization;
        var name = TxtAssetName.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            ShowValidation(loc.Translate("assetimporter.error.name_empty"));
            return false;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            ShowValidation(loc.Translate("assetimporter.error.name_invalid"));
            return false;
        }

        if (name.Contains(' '))
        {
            ShowValidation(loc.Translate("assetimporter.error.name_spaces"));
            return false;
        }

        ClearValidation();
        return true;
    }

    private void UpdateImportButton()
    {
        BtnImport.IsEnabled = _sourcePngPath is not null && ValidateAssetName();
    }

    // ── Import ────────────────────────────────────────────────────────────────

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        if (_sourcePngPath is null) return;

        var assetName = TxtAssetName.Text.Trim();
        var regionId = GetSelectedRegionId();

        var tempPath = _sourcePngPath;
        var nameFromFile = Path.GetFileNameWithoutExtension(_sourcePngPath);

        if (!string.Equals(nameFromFile, assetName, StringComparison.OrdinalIgnoreCase))
        {
            tempPath = Path.Combine(Path.GetTempPath(), assetName + ".png");
            File.Copy(_sourcePngPath, tempPath, overwrite: true);
        }

        try
        {
            ImportedAsset = Services.AssetImporter.Import(tempPath, _projectPath, regionId, _target);
            DialogResult = true;
            Close();
        }
        catch (AssetImportException ex)
        {
            ShowValidation(ex.Message);
        }
        catch (Exception ex)
        {
            var loc = ServiceLocator.Localization;
            ShowValidation(string.Format(loc.Translate("assetimporter.error.unexpected"), ex.Message));
        }
        finally
        {
            if (tempPath != _sourcePngPath && File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowValidation(string message)
        => TxtValidation.Text = message;

    private void ClearValidation()
        => TxtValidation.Text = string.Empty;

    private static BitmapImage LoadBitmapFromPath(string path)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = new Uri(path);
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private static BitmapSource SkiaBitmapToWpf(SKBitmap skiaBitmap)
    {
        using var image = SKImage.FromBitmap(skiaBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());

        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.StreamSource = stream;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private static int CountUniqueColors(SKBitmap bitmap)
    {
        var colors = new HashSet<uint>();
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                var p = bitmap.GetPixel(x, y);
                if (p.Alpha > 0)
                    colors.Add((uint)((p.Red << 16) | (p.Green << 8) | p.Blue));
            }
        return colors.Count;
    }

    protected override void OnClosed(EventArgs e)
    {
        _reducedPreview?.Dispose();
        base.OnClosed(e);
    }
}
