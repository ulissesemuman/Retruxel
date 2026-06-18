using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.PixelArtEditor;

public partial class PixelArtEditorWindow : Window
{
    public PixelArtEditorWindow()
    {
        InitializeComponent();

        // UI defaults
        TxtTileName.Text = "-- tile --";
        TxtModeInfo.Text = "PENCIL";
        TxtPixelCoord.Text = "(0,0)";
        TxtPixelColor.Text = "Color";
        TxtViewportInfo.Text = "";

        // Palette combo placeholder
        if (CmbPalette.Items.Count == 0)
        {
            CmbPalette.Items.Add("(palette not loaded)");
            CmbPalette.SelectedIndex = 0;
        }

        // Tool buttons defaults
        UpdateToolButtons(PixelTool.Pencil);
    }

    private enum PixelTool
    {
        Pencil,
        Bucket,
        Select,
        Mirror
    }

    private PixelTool _activeTool = PixelTool.Pencil;

    // Editor state (stubbed for now)
    private int _tileWidth = 8;
    private int _tileHeight = 8;
    private int _zoom = 8;

    // Simple pixel model: store ARGB colors per pixel.
    // Later this will be wired to the tileset/pipeline.
    private int[,] _pixels = new int[8, 8];

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        RenderTilesetPlaceholders();
        RenderEditorCanvas();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        catch
        {
            // ignore
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // TODO: integrate with asset/pipeline
        DialogResult = true;
        Close();
    }

    private void CmbTilesetAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TODO: integrate with tileset
        RenderTilesetPlaceholders();
        RenderEditorCanvas();
    }

    private void TilesetCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // TODO: select tile under mouse
    }

    private void EditorCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // TODO: apply pencil/bucket/selection/mirror
        CaptureMouse();
    }

    private void EditorCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        // TODO: drag painting
        if (!IsMouseCaptured) return;
    }

    private void EditorCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsMouseCaptured) ReleaseMouseCapture();
    }

    private void TilesetCanvas_MouseMove(object sender, MouseEventArgs e)
    {
    }

    private void EditorCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            var delta = e.Delta > 0 ? 1 : -1;
            _zoom = Math.Clamp(_zoom + delta, 1, 32);
            RenderEditorCanvas();
            return;
        }

        // Shift wheel pans would go here later
    }

    private void BtnCanvasZoom_Click(object sender, RoutedEventArgs e)
    {
        // cycle zoom
        var next = _zoom switch
        {
            <= 4 => 8,
            <= 8 => 12,
            <= 12 => 16,
            <= 16 => 24,
            _ => 8
        };

        _zoom = next;
        RenderEditorCanvas();
    }

    private void BtnToolPencil_Click(object sender, RoutedEventArgs e)
    {
        _activeTool = PixelTool.Pencil;
        TxtModeInfo.Text = "PENCIL";
        UpdateToolButtons(_activeTool);
    }

    private void BtnToolBucket_Click(object sender, RoutedEventArgs e)
    {
        _activeTool = PixelTool.Bucket;
        TxtModeInfo.Text = "BUCKET";
        UpdateToolButtons(_activeTool);
    }

    private void BtnToolSelect_Click(object sender, RoutedEventArgs e)
    {
        _activeTool = PixelTool.Select;
        TxtModeInfo.Text = "SELECT";
        UpdateToolButtons(_activeTool);
    }

    private void BtnToolMirror_Click(object sender, RoutedEventArgs e)
    {
        _activeTool = PixelTool.Mirror;
        TxtModeInfo.Text = "MIRROR";
        UpdateToolButtons(_activeTool);
    }

    private void BtnTileZoom_Click(object sender, RoutedEventArgs e)
    {
        // TODO: zoom options for selected tile preview
        _zoom = Math.Clamp(_zoom + 2, 1, 32);
        RenderEditorCanvas();
    }

    private void UpdateToolButtons(PixelTool tool)
    {
        // basic visual feedback via opacity
        void Set(Button b, bool active) => b.Opacity = active ? 1.0 : 0.65;

        Set(BtnToolPencil, tool == PixelTool.Pencil);
        Set(BtnToolBucket, tool == PixelTool.Bucket);
        Set(BtnToolSelect, tool == PixelTool.Select);
        Set(BtnToolMirror, tool == PixelTool.Mirror);
    }

    private void RenderTilesetPlaceholders()
    {
        // clear and render a simple grid of selectable tiles placeholders
        TilesetCanvas.Children.Clear();
        const int cols = 8;
        const int size = 16;
        for (var i = 0; i < cols * 4; i++)
        {
            var r = i / cols;
            var c = i % cols;

            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = size,
                Height = size,
                Stroke = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromRgb(35, 35, 35))
            };

            Canvas.SetLeft(rect, c * size);
            Canvas.SetTop(rect, r * size);
            TilesetCanvas.Children.Add(rect);
        }

        TxtVramRegionInfo.Text = "(tileset not loaded yet)";
    }

    private void RenderEditorCanvas()
    {
        EditorCanvas.Children.Clear();

        // Simple checkerboard background + grid + pixels
        for (var y = 0; y < _tileHeight; y++)
        for (var x = 0; x < _tileWidth; x++)
        {
            var idx = (x + y) % 2;
            var bg = idx == 0 ? (Color)Color.FromRgb(45, 45, 45) : Color.FromRgb(55, 55, 55);

            var cell = new System.Windows.Shapes.Rectangle
            {
                Width = _zoom,
                Height = _zoom,
                Fill = new SolidColorBrush(bg),
                Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                StrokeThickness = 1
            };

            Canvas.SetLeft(cell, x * _zoom);
            Canvas.SetTop(cell, y * _zoom);
            EditorCanvas.Children.Add(cell);

            var col = _pixels[x, y];
            var pixelRect = new System.Windows.Shapes.Rectangle
            {
                Width = _zoom,
                Height = _zoom,
                Fill = new SolidColorBrush(Color.FromArgb(
                    (byte)((col >> 24) & 0xFF),
                    (byte)((col >> 16) & 0xFF),
                    (byte)((col >> 8) & 0xFF),
                    (byte)(col & 0xFF)))
            };

            Canvas.SetLeft(pixelRect, x * _zoom);
            Canvas.SetTop(pixelRect, y * _zoom);
            EditorCanvas.Children.Add(pixelRect);
        }

        // set preview image to match pixel data
        ImgSelectedTile.Source = CreateTileBitmapSource();
        ImgSelectedTile.RenderOptions.BitmapScalingMode = BitmapScalingMode.NearestNeighbor;

        TxtTileName.Text = $"tile {0}";
        TxtViewportInfo.Text = $"zoom {_zoom}x";
    }

    private BitmapSource CreateTileBitmapSource()
    {
        var wb = new WriteableBitmap(_tileWidth, _tileHeight, 96, 96, PixelFormats.Bgra32, null);
        var buffer = new int[_tileWidth * _tileHeight];

        for (var y = 0; y < _tileHeight; y++)
        for (var x = 0; x < _tileWidth; x++)
        {
            var col = _pixels[x, y];
            // input is ARGB, WriteableBitmap expects BGRA32 ints
            var a = (byte)((col >> 24) & 0xFF);
            var r = (byte)((col >> 16) & 0xFF);
            var g = (byte)((col >> 8) & 0xFF);
            var b = (byte)(col & 0xFF);
            buffer[y * _tileWidth + x] = (b) | (g << 8) | (r << 16) | (a << 24);
        }

        wb.WritePixels(new Int32Rect(0, 0, _tileWidth, _tileHeight), buffer, _tileWidth * 4, 0);
        return wb;
    }

    // Placeholder event handlers referenced by XAML
    private void TilesetCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }
    private void TilesetCanvas_MouseWheel(object sender, MouseWheelEventArgs e) { }
}

