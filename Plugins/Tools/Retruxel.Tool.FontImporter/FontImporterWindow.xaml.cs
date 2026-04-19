using Microsoft.Win32;

using Retruxel.Tool.FontImporter.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Tool.FontImporter;

public partial class FontImporterWindow : Window
{
    // State

    private string? _ttfPath;
    private int _tileWidth = 8;
    private int _tileHeight = 8;

    private readonly HashSet<int> _selectedCodepoints = [];
    private readonly Dictionary<int, Border> _cellMap = [];

    // Result exposed to caller after successful import
    public FontImportResult? Result { get; private set; }

    // Init

    public FontImporterWindow()
    {
        InitializeComponent();

        var loc = LocalizationService.Instance;
        StatusLabel.Text = loc.Get("fontimporter.status.no_file");

        BuildCharGrid();
        SelectAsciiBasicRange();   // pre-select ASCII basic on open
        UpdateStats();
    }

    // Title bar

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }

    // File browse

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
        var dlg = new OpenFileDialog
        {
            Title = loc.Get("fontimporter.dialog.select_font"),
            Filter = loc.Get("fontimporter.dialog.font_filter")
        };

        if (dlg.ShowDialog() != true) return;

        _ttfPath = dlg.FileName;
        TxtFileName.Text = System.IO.Path.GetFileName(_ttfPath);
        StatusLabel.Text = loc.Get("fontimporter.status.loaded");
        StatusLabel.Foreground = (Brush)FindResource("BrushPrimary");

        RegenerateAllGlyphs();
        UpdateImportButton();
    }

    // Tile size

    private void SizePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var size = int.Parse((string)btn.Tag);

        _tileWidth = size;
        _tileHeight = size;
        TxtWidth.Text = size.ToString();
        TxtHeight.Text = size.ToString();

        // Visual: mark active preset
        foreach (var b in new[] { BtnSize8, BtnSize16, BtnSize32 })
            b.Style = (Style)FindResource("ButtonSecondary");
        btn.Style = (Style)FindResource("ButtonPrimary");

        RegenerateAllGlyphs();
        UpdateStats();
    }

    private void TileSize_Changed(object sender, TextChangedEventArgs e)
    {
        if (!int.TryParse(TxtWidth.Text, out var w) || w < 4 || w > 64) return;
        if (!int.TryParse(TxtHeight.Text, out var h) || h < 4 || h > 64) return;

        _tileWidth = w;
        _tileHeight = h;

        RegenerateAllGlyphs();
        UpdateStats();
    }

    // Character grid

    /// <summary>
    /// Builds the full Unicode BMP character grid (U+0020 to U+00FF for now).
    /// Rows of 16 — matching the spritesheet output layout.
    /// </summary>
    private void BuildCharGrid()
    {
        CharGrid.Children.Clear();
        _cellMap.Clear();

        // Range: Printable ASCII + Latin-1 Supplement
        for (int cp = 0x0020; cp <= 0x00FF; cp++)
        {
            var codepoint = cp;
            var cell = BuildCell(codepoint);
            _cellMap[codepoint] = cell;
            CharGrid.Children.Add(cell);
        }
    }

    private Border BuildCell(int codepoint)
    {
        var isSelected = _selectedCodepoints.Contains(codepoint);

        var cell = new Border
        {
            Width = 40,
            Height = 40,
            Background = isSelected
                ? new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26))
                : new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Margin = new Thickness(2),
            Cursor = Cursors.Hand,
            Tag = codepoint
        };

        if (isSelected)
        {
            cell.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
            cell.BorderThickness = new Thickness(0, 2, 0, 0);
        }

        var panel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Glyph render (or placeholder if no font loaded)
        var glyphImage = new Image
        {
            Width = 20,
            Height = 20,
            Tag = codepoint
        };
        RenderOptions.SetBitmapScalingMode(glyphImage, BitmapScalingMode.NearestNeighbor);
        panel.Children.Add(glyphImage);

        // Unicode label
        panel.Children.Add(new TextBlock
        {
            Text = $"{codepoint:X2}",
            FontSize = 8,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75)),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        cell.Child = panel;

        cell.MouseLeftButtonDown += (_, _) => ToggleCell(codepoint);
        cell.MouseEnter += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                ToggleCell(codepoint, forceSelect: true);

            UpdateGlyphPreview(codepoint);
        };

        return cell;
    }

    private void ToggleCell(int codepoint, bool forceSelect = false)
    {
        if (forceSelect)
            _selectedCodepoints.Add(codepoint);
        else if (_selectedCodepoints.Contains(codepoint))
            _selectedCodepoints.Remove(codepoint);
        else
            _selectedCodepoints.Add(codepoint);

        RefreshCellStyle(codepoint);
        UpdateStats();
        UpdateImportButton();
        UpdateSheetPreview();
    }

    private void RefreshCellStyle(int codepoint)
    {
        if (!_cellMap.TryGetValue(codepoint, out var cell)) return;

        var isSelected = _selectedCodepoints.Contains(codepoint);
        cell.Background = new SolidColorBrush(isSelected
            ? Color.FromRgb(0x26, 0x26, 0x26)
            : Color.FromRgb(0x1E, 0x1E, 0x1E));
        cell.BorderBrush = isSelected
            ? new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71))
            : Brushes.Transparent;
        cell.BorderThickness = isSelected
            ? new Thickness(0, 2, 0, 0)
            : new Thickness(0);
    }

    // Quick selection

    private void SelectAsciiBasic_Click(object sender, RoutedEventArgs e)
    {
        SelectAsciiBasicRange();
        UpdateStats();
        UpdateImportButton();
        UpdateSheetPreview();
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var cp in _cellMap.Keys) _selectedCodepoints.Add(cp);
        foreach (var cp in _cellMap.Keys) RefreshCellStyle(cp);
        UpdateStats();
        UpdateImportButton();
        UpdateSheetPreview();
    }

    private void ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        _selectedCodepoints.Clear();
        foreach (var cp in _cellMap.Keys) RefreshCellStyle(cp);
        UpdateStats();
        UpdateImportButton();
        UpdateSheetPreview();
    }

    private void SelectAsciiBasicRange()
    {
        for (int cp = 0x0020; cp <= 0x007E; cp++)
        {
            _selectedCodepoints.Add(cp);
            RefreshCellStyle(cp);
        }
    }

    // Stats & previews 

    private void UpdateStats()
    {
        var count = _selectedCodepoints.Count;
        var rows = (int)Math.Ceiling(count / 16.0);

        LblSelectedCount.Text = count.ToString();
        LblSpritesheetSize.Text = $"16×{rows} tiles";
        LblRows.Text = rows.ToString();
        LblOutputSize.Text = count > 0
            ? $"{_tileWidth * 16}×{_tileHeight * rows}px"
            : "—";
    }

    private void UpdateImportButton()
    {
        var loc = LocalizationService.Instance;
        BtnImport.IsEnabled = _ttfPath is not null && _selectedCodepoints.Count > 0;
        LblFooterMessage.Text = _ttfPath is null ? loc.Get("fontimporter.error.select_file") : "";
    }

    private void UpdateGlyphPreview(int codepoint)
    {
        LblGlyphChar.Text = char.ConvertFromUtf32(codepoint);
        LblGlyphCode.Text = $"U+{codepoint:X4}";

        if (_ttfPath is null) return;

        var bmp = FontRasterizer.RenderGlyph(_ttfPath, codepoint, _tileWidth, _tileHeight);
        if (bmp is not null)
            GlyphPreview.Source = bmp;
    }

    private void UpdateSheetPreview()
    {
        if (_ttfPath is null || _selectedCodepoints.Count == 0)
        {
            SheetPreview.Source = null;
            return;
        }

        var ordered = _selectedCodepoints.OrderBy(cp => cp).ToList();
        var sheet = FontRasterizer.RenderSpritesheet(
            _ttfPath, ordered, _tileWidth, _tileHeight, columnsPerRow: 16);
        SheetPreview.Source = sheet;
    }

    private void RegenerateAllGlyphs()
    {
        if (_ttfPath is null) return;

        foreach (var (cp, cell) in _cellMap)
        {
            if (cell.Child is not StackPanel panel) continue;
            if (panel.Children[0] is not Image img) continue;

            var bmp = FontRasterizer.RenderGlyph(_ttfPath, cp, _tileWidth, _tileHeight);
            if (bmp is not null)
                img.Source = bmp;
        }

        UpdateSheetPreview();
    }

    // Import

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_ttfPath is null || _selectedCodepoints.Count == 0) return;

        try
        {
            var loc = LocalizationService.Instance;
            LblFooterMessage.Text = loc.Get("fontimporter.status.generating");
            LblFooterMessage.Foreground = (Brush)FindResource("BrushTertiary");

            var ordered = _selectedCodepoints.OrderBy(cp => cp).ToList();
            var sheet = FontRasterizer.RenderSpritesheet(
                _ttfPath, ordered, _tileWidth, _tileHeight, columnsPerRow: 16);

            Result = new FontImportResult
            {
                SourceTtfPath = _ttfPath,
                TileWidth = _tileWidth,
                TileHeight = _tileHeight,
                Codepoints = ordered,
                Spritesheet = sheet,
                ColumnsPerRow = 16
            };

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            LblFooterMessage.Text = string.Format(loc.Get("fontimporter.error.generation"), ex.Message);
            LblFooterMessage.Foreground = (Brush)FindResource("BrushError");
        }
    }
}
