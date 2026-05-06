using Retruxel.Core.Text;
using Retruxel.Core.Models;
using Retruxel.Tool.FontImporter;
using Retruxel.Tool.LiveLink;
using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.TextArrayEditor;

public partial class TextArrayEditorWindow
{
    #region Font Management

    private void PopulateFontCategories()
    {
        var categories = new List<FontCategoryItem>
        {
            new() { Name = "All Characters (425)", CharSet = DefaultFont.SupportedCharacters, IsBuiltIn = true },
            new() { Name = "Basic Latin (95)", CharSet = DefaultFont.GetBasicLatinCharacters(), IsBuiltIn = true },
            new() { Name = "Extended Latin (96)", CharSet = DefaultFont.GetExtendedLatinCharacters(), IsBuiltIn = true },
            new() { Name = "Box Drawing (29)", CharSet = DefaultFont.GetBoxDrawingCharacters(), IsBuiltIn = true },
            new() { Name = "Block Elements (32)", CharSet = DefaultFont.GetBlockElementsCharacters(), IsBuiltIn = true },
            new() { Name = "Greek (58)", CharSet = DefaultFont.GetGreekCharacters(), IsBuiltIn = true },
            new() { Name = "Hiragana (86)", CharSet = DefaultFont.GetHiraganaCharacters(), IsBuiltIn = true },
            new() { Name = "Miscellaneous (3)", CharSet = DefaultFont.GetMiscellaneousCharacters(), IsBuiltIn = true },
            new() { Name = "SGA (26)", CharSet = DefaultFont.GetSGACharacters(), IsBuiltIn = true },
            new() { Name = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", CharSet = [], IsBuiltIn = true, IsSeparator = true },
            new() { Name = "⬇ More Fonts (u8g2 repository)...", CharSet = [], IsBuiltIn = false, IsMoreFonts = true }
        };

        CmbFontCategory.ItemsSource = categories;
        CmbFontCategory.SelectedIndex = 0;
    }

    private async void CmbFontCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbFontCategory.SelectedItem is not FontCategoryItem category)
            return;

        if (category.IsMoreFonts)
        {
            await ExpandMoreFontsAsync();
            return;
        }

        if (category.IsSeparator)
        {
            CmbFontCategory.SelectedIndex = 0;
            return;
        }

        if (category.IsRepositoryFont)
        {
            await RenderRepositoryFontPreviewAsync(category.FontIdentifier);
            return;
        }

        RenderFontPreview(category.CharSet);
    }

    private async Task ExpandMoreFontsAsync()
    {
        var currentItems = CmbFontCategory.ItemsSource as List<FontCategoryItem>;
        if (currentItems is null) return;

        // Check if already expanded
        if (currentItems.Any(i => i.IsRepositoryFont))
        {
            CmbFontCategory.SelectedIndex = 0;
            return;
        }

        // Show loading
        var moreFontsItem = currentItems.FirstOrDefault(i => i.IsMoreFonts);
        if (moreFontsItem is not null)
            moreFontsItem.Name = "⏳ Loading fonts from repository...";
        CmbFontCategory.Items.Refresh();

        // Fetch fonts from repository
        var repoFonts = await FontRepository.FetchAvailableFontsAsync();

        if (repoFonts.Count == 0)
        {
            if (moreFontsItem is not null)
                moreFontsItem.Name = "❌ Failed to load fonts";
            CmbFontCategory.Items.Refresh();
            return;
        }

        // Remove "More Fonts" item
        currentItems.RemoveAll(i => i.IsMoreFonts);

        // Add repository fonts
        foreach (var font in repoFonts)
        {
            currentItems.Add(new FontCategoryItem
            {
                Name = $"📦 {font.Name} ({font.GlyphCount})",
                CharSet = [],
                IsRepositoryFont = true,
                FontIdentifier = font.FontIdentifier,
                Copyright = font.Copyright
            });
        }

        CmbFontCategory.Items.Refresh();
        CmbFontCategory.SelectedIndex = 0;
    }

    private void RenderFontPreview(IEnumerable<char> characters)
    {
        var charList = characters.OrderBy(c => (int)c).ToList();
        if (charList.Count == 0)
        {
            ImgFontPreview.Source = null;
            return;
        }

        var lines = new List<string>();
        for (int i = 0; i < charList.Count; i += 32)
        {
            var lineChars = charList.Skip(i).Take(32);
            lines.Add(string.Join("", lineChars));
        }

        var text = string.Join("\n", lines);

        var skBitmap = DefaultFont.RenderString(text, SKColors.White, SKColors.Transparent);
        if (skBitmap is null)
        {
            ImgFontPreview.Source = null;
            return;
        }

        var scaledBitmap = new SKBitmap(skBitmap.Width * 2, skBitmap.Height * 2);
        using (var canvas = new SKCanvas(scaledBitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(skBitmap, SKRect.Create(0, 0, scaledBitmap.Width, scaledBitmap.Height),
                new SKPaint { FilterQuality = SKFilterQuality.None });
        }

        var bitmapSource = ConvertSkBitmapToBitmapSource(scaledBitmap);
        ImgFontPreview.Source = bitmapSource;

        TxtFontPreviewTitle.Text = $"FONT PREVIEW ({charList.Count} GLYPHS)";

        // Draw grid overlay
        DrawGridOverlay(scaledBitmap.Width, scaledBitmap.Height);

        skBitmap.Dispose();
        scaledBitmap.Dispose();
    }

    private async Task RenderRepositoryFontPreviewAsync(string fontIdentifier)
    {
        ImgFontPreview.Source = null;
        TxtFontPreviewTitle.Text = "⏳ Loading font...";

        var fontData = await FontRepository.DownloadFontDataAsync(fontIdentifier);
        if (fontData is null || fontData.Length < 2)
        {
            TxtFontPreviewTitle.Text = "❌ Failed to load font";
            return;
        }

        int startChar = fontData[0];
        int endChar = fontData[1];
        int glyphCount = endChar - startChar + 1;

        var glyphs = new List<(char, byte[])>();
        for (int i = 0; i < glyphCount; i++)
        {
            int offset = 2 + (i * 8);
            if (offset + 8 > fontData.Length)
                break;

            var glyphData = new byte[8];
            Array.Copy(fontData, offset, glyphData, 0, 8);

            char ch = (char)(startChar + i);
            glyphs.Add((ch, glyphData));
        }

        var charList = glyphs.Select(g => g.Item1).ToList();
        var lines = new List<string>();
        for (int i = 0; i < charList.Count; i += 30)
        {
            var lineChars = charList.Skip(i).Take(30);
            lines.Add(string.Join("", lineChars));
        }

        var maxWidth = lines.Max(line => line.Length);
        var height = lines.Count;
        var bitmap = new SKBitmap(maxWidth * 8, height * 8, SKColorType.Rgba8888, SKAlphaType.Premul);

        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);

            for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
            {
                var line = lines[lineIdx];
                for (int charIdx = 0; charIdx < line.Length; charIdx++)
                {
                    var ch = line[charIdx];
                    var glyph = glyphs.FirstOrDefault(g => g.Item1 == ch).Item2;
                    if (glyph is null) continue;

                    for (int col = 0; col < 8; col++)
                    {
                        for (int row = 0; row < 8; row++)
                        {
                            if (((glyph[col] >> row) & 1) == 1)
                                bitmap.SetPixel(charIdx * 8 + col, lineIdx * 8 + row, SKColors.White);
                        }
                    }
                }
            }
        }

        var scaledBitmap = new SKBitmap(bitmap.Width * 2, bitmap.Height * 2);
        using (var canvas = new SKCanvas(scaledBitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(bitmap, SKRect.Create(0, 0, scaledBitmap.Width, scaledBitmap.Height),
                new SKPaint { FilterQuality = SKFilterQuality.None });
        }

        var bitmapSource = ConvertSkBitmapToBitmapSource(scaledBitmap);
        ImgFontPreview.Source = bitmapSource;

        TxtFontPreviewTitle.Text = $"FONT PREVIEW ({glyphCount} GLYPHS)";

        // Draw grid overlay
        DrawGridOverlay(scaledBitmap.Width, scaledBitmap.Height);

        bitmap.Dispose();
        scaledBitmap.Dispose();
    }

    private void DrawGridOverlay(int width, int height)
    {
        GridOverlay.Children.Clear();
        GridOverlay.Width = width;
        GridOverlay.Height = height;

        var gridBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));

        // Vertical lines
        for (int x = 0; x <= width; x += 16)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = height,
                Stroke = gridBrush,
                StrokeThickness = 1
            };
            GridOverlay.Children.Add(line);
        }

        // Horizontal lines
        for (int y = 0; y <= height; y += 16)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = 0,
                Y1 = y,
                X2 = width,
                Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1
            };
            GridOverlay.Children.Add(line);
        }
    }

    private void BtnBrowseFont_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*",
            Title = "Select Font PNG"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                MessageBox.Show(
                    $"PNG font loading: {Path.GetFileName(dlg.FileName)}\n\n" +
                    "This feature will load the PNG as 8x8 tiles for font composition.\n" +
                    "Integration with TilePickerControl is pending.",
                    "Font PNG",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load PNG: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void BtnImportFromTtf_Click(object sender, RoutedEventArgs e)
    {
        var fontImporter = new FontImporterWindow();
        if (fontImporter.ShowDialog() == true && fontImporter.Result is not null)
        {
            MessageBox.Show(
                $"Font imported successfully!\n\n" +
                $"Glyphs: {fontImporter.Result.Codepoints.Count}\n" +
                $"Size: {fontImporter.Result.TileWidth}x{fontImporter.Result.TileHeight}\n\n" +
                "Note: Integration with TextArray module is not yet implemented.\n" +
                "The imported font data is available but needs to be connected to the text system.",
                "Font Import Result",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void BtnLiveLink_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var liveLinkInput = new Dictionary<string, object>
            {
                ["mode"] = "capture",
                ["callerId"] = "TextArrayEditor",
                ["targetId"] = "sms"
            };

            var liveLinkWindow = new LiveLinkWindow(liveLinkInput)
            {
                Owner = this
            };

            if (liveLinkWindow.ShowDialog() == true && liveLinkWindow.ModuleData != null)
            {
                if (liveLinkWindow.ModuleData.TryGetValue("importedAssetData", out var assetDataObj) &&
                    assetDataObj is ImportedAssetData importedData)
                {
                    TxtLiveLinkStatus.Text = $"LiveLink: captured {importedData.Tiles?.Length ?? 0} tiles";
                    TxtLiveLinkStatus.Foreground = (Brush)FindResource("BrushPrimary");

                    MessageBox.Show(
                        $"LiveLink capture successful!\n\n" +
                        $"Tiles: {importedData.Tiles?.Length ?? 0}\n" +
                        $"Palette: {importedData.Palette?.Length ?? 0} colors\n\n" +
                        "Note: Integration with TilePickerControl is pending.\n" +
                        "The captured font tiles will be available for composition.",
                        "LiveLink Capture",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            else
            {
                TxtLiveLinkStatus.Text = "LiveLink: capture cancelled";
                TxtLiveLinkStatus.Foreground = (Brush)FindResource("BrushOnSurfaceVariant");
            }
        }
        catch (Exception ex)
        {
            TxtLiveLinkStatus.Text = $"LiveLink: error - {ex.Message}";
            TxtLiveLinkStatus.Foreground = (Brush)FindResource("BrushError");

            MessageBox.Show(
                $"LiveLink capture failed:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void PopulateAsciiMap()
    {
        var items = new List<AsciiMapItem>();

        for (int i = 32; i < 128; i++)
        {
            var ch = (char)i;
            var tileIndex = i - 32;

            items.Add(new AsciiMapItem
            {
                Char = $"'{ch}'",
                Dec = i.ToString(),
                Tile = $"#{tileIndex:D3}"
            });
        }

        AsciiMapList.ItemsSource = items;
    }

    private class FontCategoryItem
    {
        public string Name { get; set; } = "";
        public IEnumerable<char> CharSet { get; set; } = [];
        public bool IsBuiltIn { get; set; }
        public bool IsMoreFonts { get; set; }
        public bool IsSeparator { get; set; }
        public bool IsRepositoryFont { get; set; }
        public string FontIdentifier { get; set; } = "";
        public string Copyright { get; set; } = "";
    }

    private class AsciiMapItem
    {
        public string Char { get; set; } = "";
        public string Dec { get; set; } = "";
        public string Tile { get; set; } = "";
    }

    #endregion
}
