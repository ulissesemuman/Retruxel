using Retruxel.Core.Text;
using Retruxel.Modules.Graphics;
using SkiaSharp;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 8, 12, 8)
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

    private void BtnBrowseFont_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Custom fonts are not yet supported. Using built-in font8x8.",
            "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnImportFromTtf_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Font importer not yet implemented.", "Coming Soon",
            MessageBoxButton.OK, MessageBoxImage.Information);
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

        Grid.SetRow(btnPanel, 4);
        grid.Children.Add(btnPanel);

        Content = grid;
    }
}
