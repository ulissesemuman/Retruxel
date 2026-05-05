using Retruxel.Core.Text;
using Retruxel.Modules.Graphics;
using Retruxel.Core.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Retruxel.Tool.FontImporter;
using Retruxel.Tool.LiveLink;
using Microsoft.Win32;
using System.IO;

namespace Retruxel.Tool.TextArrayEditor;

/// <summary>
/// Text Array Editor Window - manages multilingual string arrays with font preview.
/// </summary>
public partial class TextArrayEditorWindow : Window
{
    private readonly TextArrayModule _module;
    private readonly string _projectPath;
    private TextArrayState _state;
    private int _activeLanguageIndex = 0;
    private int _selectedStringIndex = -1;

    /// <summary>
    /// Module data to be returned to the invoker.
    /// </summary>
    public Dictionary<string, object>? ModuleData { get; private set; }

    public TextArrayEditorWindow(TextArrayModule module, string projectPath)
    {
        InitializeComponent();
        _module = module;
        _projectPath = projectPath;

        // Deserialize module state
        var json = _module.Serialize();
        _state = System.Text.Json.JsonSerializer.Deserialize<TextArrayState>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        }) ?? new TextArrayState();

        // Initialize UI
        TxtArrayNameInput.Text = _state.Name;
        TxtArrayName.Text = $"— {_state.Name}";

        RefreshLanguageTabs();
        RefreshStringsList();
        PopulateFontCategories();
        PopulateAsciiMap();

        // Set initial tab
        ActivateTab(TabStrings, BtnTabStrings);
    }

    #region Window Management

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Check for unsaved changes
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Confirm if there are unsaved changes
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        SaveAndClose();
    }

    private void SaveAndClose()
    {
        // Validate array name
        if (!IsValidIdentifier(_state.Name))
        {
            MessageBox.Show("Array name must be a valid C identifier (letters, numbers, underscore only).",
                "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate that all languages have the same string count
        if (_state.Languages.Count > 0)
        {
            var expectedCount = _state.Languages[0].Strings.Count;
            if (_state.Languages.Any(lang => lang.Strings.Count != expectedCount))
            {
                MessageBox.Show("All languages must have the same number of strings.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        // Check if using repository font and show copyright warning
        if (CmbFontCategory.SelectedItem is FontCategoryItem selectedFont && selectedFont.IsRepositoryFont)
        {
            var copyrightInfo = string.IsNullOrEmpty(selectedFont.Copyright) 
                ? "Copyright information not available" 
                : selectedFont.Copyright;

            var result = MessageBox.Show(
                $"You are using a font from the u8g2 repository.\n\n" +
                $"FONT: {selectedFont.Name}\n" +
                $"COPYRIGHT: {copyrightInfo}\n\n" +
                "IMPORTANT NOTICE:\n" +
                "• This font may be subject to copyright restrictions\n" +
                "• You must respect the font's license terms\n" +
                "• Credit the original author when required\n" +
                "• Visit the u8g2 repository for full copyright information:\n" +
                "  https://github.com/olikraus/u8g2\n\n" +
                "By clicking YES, you acknowledge that you are responsible for:\n" +
                "• Verifying the font's license\n" +
                "• Complying with copyright requirements\n" +
                "• Providing proper attribution if needed\n\n" +
                "Do you want to continue?",
                "Repository Font - Copyright Notice",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;
        }

        // Serialize to ModuleData for VisualToolInvoker
        ModuleData = new Dictionary<string, object>
        {
            ["name"] = _state.Name,
            ["languages"] = _state.Languages.Select(lang => new Dictionary<string, object>
            {
                ["code"] = lang.Code,
                ["strings"] = lang.Strings
            }).ToList(),
            ["fontAssetId"] = _state.FontAssetId ?? ""
        };

        DialogResult = true;
        Close();
    }

    #endregion

    #region Tab Management

    private void BtnTabStrings_Click(object sender, RoutedEventArgs e)
    {
        ActivateTab(TabStrings, BtnTabStrings);
    }

    private void BtnTabFont_Click(object sender, RoutedEventArgs e)
    {
        ActivateTab(TabFont, BtnTabFont);
    }

    private void ActivateTab(UIElement tabContent, Button tabButton)
    {
        // Hide all tabs
        TabStrings.Visibility = Visibility.Collapsed;
        TabFont.Visibility = Visibility.Collapsed;

        // Reset all tab buttons
        BtnTabStrings.Foreground = (Brush)FindResource("BrushOnSurfaceVariant");
        BtnTabFont.Foreground = (Brush)FindResource("BrushOnSurfaceVariant");

        // Show selected tab
        tabContent.Visibility = Visibility.Visible;
        tabButton.Foreground = (Brush)FindResource("BrushPrimary");
    }

    #endregion

    #region Array Name

    private void TxtArrayNameInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        var newName = TxtArrayNameInput.Text;

        // Validate identifier
        if (IsValidIdentifier(newName))
        {
            _state.Name = newName;
            TxtArrayName.Text = $"— {newName}";
            TxtArrayNameInput.Foreground = (Brush)FindResource("BrushOnSurface");
        }
        else
        {
            TxtArrayNameInput.Foreground = (Brush)FindResource("BrushError");
        }
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    #endregion

    #region Language Management

    private void RefreshLanguageTabs()
    {
        LanguageTabs.Children.Clear();

        for (int i = 0; i < _state.Languages.Count; i++)
        {
            var lang = _state.Languages[i];
            var index = i; // Capture for closure

            var btn = new Button
            {
                Content = $"[{lang.Code}]",
                Style = (Style)FindResource("ButtonGhost"),
                Padding = new Thickness(12, 0, 12, 0),
                Height = 32,
                Margin = new Thickness(0, 0, 4, 0)
            };

            if (index == _activeLanguageIndex)
                btn.Foreground = (Brush)FindResource("BrushPrimary");
            else
                btn.Foreground = (Brush)FindResource("BrushOnSurfaceVariant");

            btn.Click += (s, e) => SelectLanguage(index);

            LanguageTabs.Children.Add(btn);
        }

        // Add [+] button
        var addBtn = new Button
        {
            Content = "+",
            Style = (Style)FindResource("ButtonGhost"),
            Padding = new Thickness(12, 0, 12, 0),
            Height = 32,
            Foreground = (Brush)FindResource("BrushPrimary")
        };
        addBtn.Click += BtnAddLanguage_Click;
        LanguageTabs.Children.Add(addBtn);
    }

    private void SelectLanguage(int index)
    {
        _activeLanguageIndex = index;
        RefreshLanguageTabs();
        RefreshStringsList();
    }

    private void BtnAddLanguage_Click(object sender, RoutedEventArgs e)
    {
        // Prompt for language code
        var dialog = new TextInputDialog("New Language", "Enter language code (e.g., 'en', 'pt', 'jp'):");
        if (dialog.ShowDialog() == true)
        {
            var code = dialog.InputText.Trim();
            if (string.IsNullOrEmpty(code))
                return;

            // Check if language already exists
            if (_state.Languages.Any(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"Language '{code}' already exists.", "Duplicate Language",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create new language with same string count as existing languages
            var stringCount = _state.Languages.Count > 0 ? _state.Languages[0].Strings.Count : 1;
            var newLang = new TextLanguage
            {
                Code = code,
                Strings = Enumerable.Repeat("", stringCount).ToList()
            };

            _state.Languages.Add(newLang);
            _activeLanguageIndex = _state.Languages.Count - 1;

            RefreshLanguageTabs();
            RefreshStringsList();
        }
    }

    #endregion

    #region Strings Management

    private void RefreshStringsList()
    {
        StringsList.Children.Clear();

        if (_state.Languages.Count == 0 || _activeLanguageIndex >= _state.Languages.Count)
            return;

        var currentLang = _state.Languages[_activeLanguageIndex];

        for (int i = 0; i < currentLang.Strings.Count; i++)
        {
            var index = i; // Capture for closure
            var stringValue = currentLang.Strings[i];

            var grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 1),
                Background = (Brush)FindResource("BrushSurfaceContainerLow")
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            // Index
            var txtIndex = new TextBlock
            {
                Text = i.ToString("D3"),
                Style = (Style)FindResource("TextCode"),
                Foreground = (Brush)FindResource("BrushPrimary"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 8, 12, 8)
            };
            Grid.SetColumn(txtIndex, 0);
            grid.Children.Add(txtIndex);

            // String input
            var txtInput = new TextBox
            {
                Text = stringValue,
                //Background = Brushes.Transparent,
                //BorderThickness = new Thickness(0),
                //VerticalAlignment = VerticalAlignment.Center,
                //Margin = new Thickness(12, 8, 12, 8)
            };
            txtInput.TextChanged += (s, e) =>
            {
                currentLang.Strings[index] = txtInput.Text;
                if (index == _selectedStringIndex)
                    RenderPreview(txtInput.Text);
            };
            txtInput.GotFocus += (s, e) =>
            {
                _selectedStringIndex = index;
                RenderPreview(txtInput.Text);
            };
            Grid.SetColumn(txtInput, 1);
            grid.Children.Add(txtInput);

            // Delete button
            var btnDelete = new Button
            {
                Content = "✕",
                Style = (Style)FindResource("ButtonGhost"),
                Foreground = (Brush)FindResource("BrushError"),
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 4, 0)
            };
            btnDelete.Click += (s, e) => RemoveStringAtIndex(index);
            Grid.SetColumn(btnDelete, 2);
            grid.Children.Add(btnDelete);

            StringsList.Children.Add(grid);
        }
    }

    private void BtnAddString_Click(object sender, RoutedEventArgs e)
    {
        // Add empty string to all languages
        foreach (var lang in _state.Languages)
        {
            lang.Strings.Add("");
        }

        RefreshStringsList();
    }

    private void RemoveStringAtIndex(int index)
    {
        // Confirm deletion
        var result = MessageBox.Show(
            $"Remove string at index {index} from ALL languages?",
            "Confirm Deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        // Remove from all languages
        foreach (var lang in _state.Languages)
        {
            if (index < lang.Strings.Count)
                lang.Strings.RemoveAt(index);
        }

        RefreshStringsList();
    }

    #endregion

    #region Preview

    private void RenderPreview(string text)
    {
        PreviewCanvas.Children.Clear();

        if (string.IsNullOrEmpty(text))
        {
            TxtPreviewWarning.Visibility = Visibility.Visible;
            TxtPreviewWarning.Text = "Type a string to see preview";
            return;
        }

        // Check for unsupported characters
        var unsupportedChars = text.Where(c => !DefaultFont.Supports(c)).Distinct().ToList();
        if (unsupportedChars.Count > 0)
        {
            TxtPreviewWarning.Visibility = Visibility.Visible;
            TxtPreviewWarning.Text = $"Unsupported characters: {string.Join(", ", unsupportedChars.Select(c => $"'{c}'"))}";
        }
        else
        {
            TxtPreviewWarning.Visibility = Visibility.Collapsed;
        }

        // Render using DefaultFont with SkiaSharp
        var skBitmap = DefaultFont.RenderString(text, SKColors.White, SKColors.Transparent);
        if (skBitmap is null) return;

        // Scale 2x for preview
        var scaledBitmap = new SKBitmap(skBitmap.Width * 2, skBitmap.Height * 2);
        using (var canvas = new SKCanvas(scaledBitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(skBitmap, SKRect.Create(0, 0, scaledBitmap.Width, scaledBitmap.Height),
                new SKPaint { FilterQuality = SKFilterQuality.None });
        }

        // Convert SKBitmap to WPF BitmapSource
        var bitmapSource = ConvertSkBitmapToBitmapSource(scaledBitmap);

        var img = new System.Windows.Controls.Image
        {
            Source = bitmapSource,
            Stretch = Stretch.None
        };

        PreviewCanvas.Children.Add(img);

        skBitmap.Dispose();
        scaledBitmap.Dispose();
    }

    private static BitmapSource ConvertSkBitmapToBitmapSource(SKBitmap skBitmap)
    {
        var info = skBitmap.Info;
        var pixels = skBitmap.GetPixels();

        var bitmap = new WriteableBitmap(info.Width, info.Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.Lock();

        unsafe
        {
            var src = (byte*)pixels.ToPointer();
            var dst = (byte*)bitmap.BackBuffer.ToPointer();
            var stride = bitmap.BackBufferStride;

            for (int y = 0; y < info.Height; y++)
            {
                for (int x = 0; x < info.Width; x++)
                {
                    var srcOffset = (y * info.Width + x) * 4;
                    var dstOffset = y * stride + x * 4;

                    // RGBA → BGRA
                    dst[dstOffset + 0] = src[srcOffset + 2]; // B
                    dst[dstOffset + 1] = src[srcOffset + 1]; // G
                    dst[dstOffset + 2] = src[srcOffset + 0]; // R
                    dst[dstOffset + 3] = src[srcOffset + 3]; // A
                }
            }
        }

        bitmap.AddDirtyRect(new Int32Rect(0, 0, info.Width, info.Height));
        bitmap.Unlock();

        return bitmap;
    }

    #endregion

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
        var repoFonts = await Core.Text.FontRepository.FetchAvailableFontsAsync();

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

        var fontData = await Core.Text.FontRepository.DownloadFontDataAsync(fontIdentifier);
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

        var gridBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255));

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
                // Load PNG as external font source
                // TODO: Integrate with TilePickerControl when implemented
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
            // Open LiveLink in capture mode for font tiles
            var liveLinkInput = new Dictionary<string, object>
            {
                ["mode"] = "capture",
                ["callerId"] = "TextArrayEditor",
                ["targetId"] = "sms" // Default to SMS, can be made configurable
            };

            var liveLinkWindow = new LiveLinkWindow(liveLinkInput)
            {
                Owner = this
            };

            if (liveLinkWindow.ShowDialog() == true && liveLinkWindow.ModuleData != null)
            {
                // Extract captured data
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

    #endregion

    #region ASCII Map

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

    private class AsciiMapItem
    {
        public string Char { get; set; } = "";
        public string Dec { get; set; } = "";
        public string Tile { get; set; } = "";
    }

    #endregion

    #region State Classes

    private class TextArrayState
    {
        public string Name { get; set; } = "strings";
        public List<TextLanguage> Languages { get; set; } = new()
        {
            new TextLanguage { Code = "default", Strings = [""] }
        };
        public string? FontAssetId { get; set; }
    }

    private class TextLanguage
    {
        public string Code { get; set; } = "default";
        public List<string> Strings { get; set; } = [];
    }

    #endregion
}

/// <summary>
/// Simple text input dialog for language code entry.
/// </summary>
public class TextInputDialog : Window
{
    public string InputText { get; private set; } = "";

    public TextInputDialog(string title, string prompt)
    {
        Title = title;
        Width = 400;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)FindResource("BrushSurface");

        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var txtPrompt = new TextBlock
        {
            Text = prompt,
            Style = (Style)FindResource("TextBody"),
            Foreground = (Brush)FindResource("BrushOnSurface")
        };
        Grid.SetRow(txtPrompt, 0);
        grid.Children.Add(txtPrompt);

        var txtInput = new TextBox();
        Grid.SetRow(txtInput, 2);
        grid.Children.Add(txtInput);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var btnCancel = new Button
        {
            Content = "CANCEL",
            Style = (Style)FindResource("ButtonGhost"),
            Padding = new Thickness(16, 0, 16, 0),
            Height = 32,
            Margin = new Thickness(0, 0, 8, 0)
        };
        btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
        btnPanel.Children.Add(btnCancel);

        var btnOk = new Button
        {
            Content = "OK",
            Style = (Style)FindResource("ButtonPrimary"),
            Padding = new Thickness(24, 0, 24, 0),
            Height = 32
        };
        btnOk.Click += (s, e) =>
        {
            InputText = txtInput.Text;
            DialogResult = true;
            Close();
        };
        btnPanel.Children.Add(btnOk);
    }
}
