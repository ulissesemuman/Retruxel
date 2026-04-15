using Microsoft.Win32;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Tool.AssetImporter.Services;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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
    }

    /// <summary>Pre-selects the Sprites radio button before the window is shown.</summary>
    public void PreSelectSprites()
    {
        RbSprites.IsChecked = true;
        RbTiles.IsChecked = false;
    }

    // ── Title bar ─────────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Close();

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
        => Close();

    // ── File selection ────────────────────────────────────────────────────────

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
        var dialog = new OpenFileDialog
        {
            Title = loc.Get("assetimporter.dialog.select_png"),
            Filter = loc.Get("assetimporter.dialog.png_filter")
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
            var loc = LocalizationService.Instance;
            ShowValidation(loc.Get("assetimporter.error.only_png"));
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

            var loc = LocalizationService.Instance;

            if (bitmap is null)
            {
                ShowValidation(loc.Get("assetimporter.error.decode_failed"));
                return;
            }

            if (bitmap.Width % 8 != 0 || bitmap.Height % 8 != 0)
            {
                ShowValidation(string.Format(loc.Get("assetimporter.error.dimensions"), bitmap.Width, bitmap.Height));
                return;
            }

            // Show source preview
            _sourcePngPath = pngPath;
            ImgSource.Source = LoadBitmapFromPath(pngPath);
            ImgSource.Visibility = Visibility.Visible;
            DropHint.Visibility = Visibility.Collapsed;

            var tileCount = (bitmap.Width / 8) * (bitmap.Height / 8);
            TxtSourceInfo.Text = string.Format(loc.Get("assetimporter.info.source"), bitmap.Width, bitmap.Height, tileCount);
            TxtTileCount.Text = string.Format(loc.Get("assetimporter.info.tiles"), tileCount);

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
            var loc = LocalizationService.Instance;
            ShowValidation(string.Format(loc.Get("assetimporter.error.loading"), ex.Message));
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
            var loc = LocalizationService.Instance;
            var uniqueColors = CountUniqueColors(_reducedPreview);
            TxtReducedInfo.Text = string.Format(loc.Get("assetimporter.info.colors"), uniqueColors, _target.DisplayName);
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            TxtReducedInfo.Text = string.Format(loc.Get("assetimporter.error.preview"), ex.Message);
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
        var loc = LocalizationService.Instance;
        var name = TxtAssetName.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            ShowValidation(loc.Get("assetimporter.error.name_empty"));
            return false;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            ShowValidation(loc.Get("assetimporter.error.name_invalid"));
            return false;
        }

        if (name.Contains(' '))
        {
            ShowValidation(loc.Get("assetimporter.error.name_spaces"));
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
        var assetType = RbTiles.IsChecked == true ? AssetType.Tiles : AssetType.Sprites;

        // Rename source to match the asset name before import
        var tempPath = _sourcePngPath;
        var nameFromFile = Path.GetFileNameWithoutExtension(_sourcePngPath);

        if (!string.Equals(nameFromFile, assetName, StringComparison.OrdinalIgnoreCase))
        {
            // Copy to a temp file with the correct name so AssetImporter picks up the right Id
            tempPath = Path.Combine(Path.GetTempPath(), assetName + ".png");
            File.Copy(_sourcePngPath, tempPath, overwrite: true);
        }

        try
        {
            ImportedAsset = Services.AssetImporter.Import(tempPath, _projectPath, assetType, _target);
            DialogResult = true;
            Close();
        }
        catch (AssetImportException ex)
        {
            ShowValidation(ex.Message);
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            ShowValidation(string.Format(loc.Get("assetimporter.error.unexpected"), ex.Message));
        }
        finally
        {
            // Clean up temp file if created
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
