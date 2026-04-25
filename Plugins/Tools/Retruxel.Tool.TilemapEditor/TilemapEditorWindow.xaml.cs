using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Retruxel.Tool.TilemapEditor;

/// <summary>
/// Tilemap Editor Window - 100% target-agnostic.
/// Generates UI dynamically based on target.Specs.Tilemap.
/// </summary>
public partial class TilemapEditorWindow : Window
{
    private readonly ITarget _target;
    private readonly RetruxelProject _project;
    private readonly string _projectPath;
    private readonly ToolRegistry? _toolRegistry;
    private readonly Func<System.Threading.Tasks.Task>? _saveProjectCallback;
    private readonly object? _sceneEditor;

    private int _selectedTileId = 0;
    private int _currentLayerIndex = 0;
    private double _zoomLevel = 1.0;
    private double _tileZoomLevel = 1.0;
    private bool _isPainting = false;
    private bool _isInitializing = true;

    // Tilemap data: [layer][y * width + x] = tileId
    private List<int[]> _tilemapLayers = new();

    // Tileset image
    private BitmapSource? _tilesetImage;
    private int _tilesetColumns = 0;
    private int _tilesetRows = 0;

    // Module data to return
    public Dictionary<string, object>? ModuleData { get; private set; }

    public TilemapEditorWindow(ITarget target, RetruxelProject project, string projectPath, ToolRegistry? toolRegistry = null, Func<System.Threading.Tasks.Task>? saveProjectCallback = null, object? sceneEditor = null)
    {
        InitializeComponent();

        _target = target;
        _project = project;
        _projectPath = projectPath;
        _toolRegistry = toolRegistry;
        _saveProjectCallback = saveProjectCallback;
        _sceneEditor = sceneEditor;

        TxtTargetLabel.Text = target.DisplayName.ToUpper();

        InitializeUI();
        LoadAssets();
    }

    /// <summary>
    /// Initializes UI dynamically based on target.Specs.Tilemap.
    /// </summary>
    private void InitializeUI()
    {
        var specs = _target.Specs.Tilemap;

        // Set default dimensions (allow larger maps)
        int mapWidth = specs.DefaultWidth * 2;
        int mapHeight = specs.DefaultHeight * 2;
        TxtWidth.Text = mapWidth.ToString();
        TxtHeight.Text = mapHeight.ToString();

        // Initialize layers with correct size
        int layerCount = specs.MaxLayers;
        for (int i = 0; i < layerCount; i++)
        {
            var layer = new int[mapWidth * mapHeight];
            // Initialize with -1 (empty)
            Array.Fill(layer, -1);
            _tilemapLayers.Add(layer);
        }

        // Show/hide layer selector based on MaxLayers
        if (specs.MaxLayers > 1)
        {
            PanelLayers.Visibility = Visibility.Visible;
            CmbLayers.Items.Clear();
            for (int i = 0; i < specs.MaxLayers; i++)
                CmbLayers.Items.Add($"Layer {i + 1}");
            CmbLayers.SelectedIndex = 0;

            TxtLayerInfo.Text = $"Layer 1 of {specs.MaxLayers}";
        }
        else
        {
            PanelLayers.Visibility = Visibility.Collapsed;
            TxtLayerInfo.Text = "";
        }

        // Configure palette UI based on PaletteMode
        ConfigurePaletteUI(specs.PaletteMode, specs.PaletteBitsPerTile);

        // Render canvas
        RenderCanvas();

        _isInitializing = false;
    }

    /// <summary>
    /// Loads existing module data into the editor.
    /// Called when editing an existing tilemap module.
    /// </summary>
    public void LoadModuleData(Dictionary<string, object> moduleData)
    {
        System.Diagnostics.Debug.WriteLine($"[TilemapEditor] Loading module data:");
        System.Diagnostics.Debug.WriteLine($"  Keys: {string.Join(", ", moduleData.Keys)}");

        // Use mapWidth/mapHeight instead of width/height
        if (moduleData.ContainsKey("mapWidth"))
            TxtWidth.Text = moduleData["mapWidth"].ToString()!;
        else if (moduleData.ContainsKey("width"))
            TxtWidth.Text = moduleData["width"].ToString()!;

        if (moduleData.ContainsKey("mapHeight"))
            TxtHeight.Text = moduleData["mapHeight"].ToString()!;
        else if (moduleData.ContainsKey("height"))
            TxtHeight.Text = moduleData["height"].ToString()!;

        // Resize layers to match loaded dimensions
        int width = int.Parse(TxtWidth.Text);
        int height = int.Parse(TxtHeight.Text);
        int newSize = width * height;

        System.Diagnostics.Debug.WriteLine($"  Width: {width}, Height: {height}, Size: {newSize}");

        for (int i = 0; i < _tilemapLayers.Count; i++)
        {
            if (_tilemapLayers[i].Length != newSize)
            {
                var newLayer = new int[newSize];
                Array.Fill(newLayer, -1);
                _tilemapLayers[i] = newLayer;
            }
        }

        if (moduleData.ContainsKey("tilesAssetId"))
        {
            string assetId = moduleData["tilesAssetId"].ToString()!;
            System.Diagnostics.Debug.WriteLine($"  Asset ID: {assetId}");
            for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
            {
                if (CmbTilesetAsset.Items[i].ToString() == assetId)
                {
                    CmbTilesetAsset.SelectedIndex = i;
                    break;
                }
            }
        }

        // Load associated palette
        if (moduleData.ContainsKey("paletteRef"))
        {
            string paletteRef = moduleData["paletteRef"].ToString()!;
            System.Diagnostics.Debug.WriteLine($"  Palette Ref: {paletteRef}");
            for (int i = 0; i < CmbPalette.Items.Count; i++)
            {
                if (CmbPalette.Items[i].ToString() == paletteRef)
                {
                    CmbPalette.SelectedIndex = i;
                    break;
                }
            }
        }

        // Try both 'mapData' (TilemapModule) and 'data' (legacy)
        if (moduleData.ContainsKey("mapData"))
        {
            var mapDataObj = moduleData["mapData"];
            
            // Handle int[] (TilemapModule format)
            if (mapDataObj is System.Text.Json.JsonElement jsonEl)
            {
                if (jsonEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var intArray = jsonEl.EnumerateArray().Select(e => e.GetInt32()).ToArray();
                    System.Diagnostics.Debug.WriteLine($"  MapData as int[]: {intArray.Length} elements");
                    
                    int nonEmptyCount = 0;
                    for (int i = 0; i < intArray.Length && i < _tilemapLayers[_currentLayerIndex].Length; i++)
                    {
                        _tilemapLayers[_currentLayerIndex][i] = intArray[i];
                        if (intArray[i] >= 0) nonEmptyCount++;
                    }
                    System.Diagnostics.Debug.WriteLine($"  Non-empty tiles loaded: {nonEmptyCount}");
                    RenderCanvas();
                }
                else if (jsonEl.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    // Handle Base64 string (legacy format)
                    var base64Data = jsonEl.GetString();
                    if (!string.IsNullOrEmpty(base64Data))
                    {
                        LoadFromBase64(base64Data);
                    }
                }
            }
            else if (mapDataObj is int[] intArray)
            {
                System.Diagnostics.Debug.WriteLine($"  MapData as int[]: {intArray.Length} elements");
                
                int nonEmptyCount = 0;
                for (int i = 0; i < intArray.Length && i < _tilemapLayers[_currentLayerIndex].Length; i++)
                {
                    _tilemapLayers[_currentLayerIndex][i] = intArray[i];
                    if (intArray[i] >= 0) nonEmptyCount++;
                }
                System.Diagnostics.Debug.WriteLine($"  Non-empty tiles loaded: {nonEmptyCount}");
                RenderCanvas();
            }
        }
        else if (moduleData.ContainsKey("data"))
        {
            var dataObj = moduleData["data"];
            if (dataObj is string base64Data)
            {
                LoadFromBase64(base64Data);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"  No 'mapData' or 'data' key found in moduleData!");
        }
    }

    private void LoadFromBase64(string base64Data)
    {
        System.Diagnostics.Debug.WriteLine($"  Data length: {base64Data.Length} chars");
        System.Diagnostics.Debug.WriteLine($"  First 50 chars: {(base64Data.Length > 50 ? base64Data.Substring(0, 50) : base64Data)}");
        
        byte[] bytes = Convert.FromBase64String(base64Data);
        System.Diagnostics.Debug.WriteLine($"  Decoded bytes: {bytes.Length}");

        int nonEmptyCount = 0;
        for (int i = 0; i < bytes.Length && i < _tilemapLayers[_currentLayerIndex].Length; i++)
        {
            // Convert 255 (empty marker) back to -1
            if (bytes[i] == 255)
                _tilemapLayers[_currentLayerIndex][i] = -1;
            else
            {
                _tilemapLayers[_currentLayerIndex][i] = bytes[i];
                nonEmptyCount++;
            }
        }

        System.Diagnostics.Debug.WriteLine($"  Non-empty tiles loaded: {nonEmptyCount}");
        RenderCanvas();
    }

    /// <summary>
    /// Configures palette UI dynamically based on PaletteMode.
    /// </summary>
    private void ConfigurePaletteUI(PaletteMode mode, int paletteBits)
    {
        CmbPalette.Items.Clear();

        // Load existing palette modules from project
        var paletteModules = _project.Scenes
            .SelectMany(s => s.Elements)
            .Where(e => e.ModuleId == "palette")
            .ToList();

        switch (mode)
        {
            case PaletteMode.PerTilemap:
                LblPalette.Text = "PALETTE (TILEMAP)";
                TxtPaletteModeInfo.Text = "One palette for the entire tilemap. All tiles share the same palette.";

                // Add existing palettes
                foreach (var pal in paletteModules)
                    CmbPalette.Items.Add(pal.ElementId);

                // Add <New Palette> option
                CmbPalette.Items.Add("<New Palette>");
                CmbPalette.SelectedIndex = 0;
                break;

            case PaletteMode.PerTile:
                int paletteCount = 1 << paletteBits;
                LblPalette.Text = "PALETTE (DEFAULT)";
                TxtPaletteModeInfo.Text = $"Each tile can use one of {paletteCount} palettes. Select default palette here. Use palette brush to paint per-tile palettes.";

                // Add existing palettes (up to paletteCount)
                int addedCount = 0;
                foreach (var pal in paletteModules.Take(paletteCount))
                {
                    CmbPalette.Items.Add(pal.ElementId);
                    addedCount++;
                }

                // Fill remaining slots with <New Palette>
                for (int i = addedCount; i < paletteCount; i++)
                    CmbPalette.Items.Add("<New Palette>");

                CmbPalette.SelectedIndex = 0;
                break;

            case PaletteMode.PerBlock:
                LblPalette.Text = "PALETTE (BLOCK)";
                TxtPaletteModeInfo.Text = "Tiles are grouped into blocks. Each block shares a palette. Use block overlay to assign palettes.";

                // Add existing palettes
                foreach (var pal in paletteModules)
                    CmbPalette.Items.Add(pal.ElementId);

                // Add <New Palette> option
                CmbPalette.Items.Add("<New Palette>");
                CmbPalette.SelectedIndex = 0;
                break;
        }
    }

    /// <summary>
    /// Loads available tileset assets from project.
    /// </summary>
    private void LoadAssets()
    {
        CmbTilesetAsset.Items.Clear();

        // Filter assets by VramRegion = "bg" or "background"
        var bgAssets = _project.Assets
            .Where(a => a.VramRegionId == "bg" || a.VramRegionId == "background")
            .ToList();

        if (bgAssets.Count == 0)
        {
            TxtVramRegionInfo.Text = "No background assets found. Click IMPORT ASSET to add one.";
            return;
        }

        foreach (var asset in bgAssets)
        {
            CmbTilesetAsset.Items.Add(asset.Id);
        }

        CmbTilesetAsset.SelectedIndex = 0;
    }

    /// <summary>
    /// Renders the tilemap canvas with grid lines and viewport overlay.
    /// </summary>
    private void RenderCanvas()
    {
        TilemapCanvas.Children.Clear();

        int width = int.Parse(TxtWidth.Text);
        int height = int.Parse(TxtHeight.Text);
        int tileSize = _target.Specs.TileWidth;

        double scaledTileSize = tileSize * _zoomLevel;

        TilemapCanvas.Width = width * scaledTileSize;
        TilemapCanvas.Height = height * scaledTileSize;

        // Render tiles from current layer
        if (_tilesetImage != null && _tilemapLayers.Count > _currentLayerIndex)
        {
            var currentLayer = _tilemapLayers[_currentLayerIndex];
            int expectedSize = width * height;

            // Safety check: ensure layer has correct size
            if (currentLayer.Length != expectedSize)
            {
                var newLayer = new int[expectedSize];
                Array.Fill(newLayer, -1);
                Array.Copy(currentLayer, newLayer, Math.Min(currentLayer.Length, expectedSize));
                _tilemapLayers[_currentLayerIndex] = newLayer;
                currentLayer = newLayer;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (index < currentLayer.Length)
                    {
                        int tileId = currentLayer[index];
                        if (tileId >= 0)
                        {
                            RenderTileAt(x, y, tileId, scaledTileSize);
                        }
                    }
                }
            }
        }

        // Draw grid lines
        for (int x = 0; x <= width; x++)
        {
            var line = new Line
            {
                X1 = x * scaledTileSize,
                Y1 = 0,
                X2 = x * scaledTileSize,
                Y2 = height * scaledTileSize,
                Stroke = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)),
                StrokeThickness = 1
            };
            TilemapCanvas.Children.Add(line);
        }

        for (int y = 0; y <= height; y++)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y * scaledTileSize,
                X2 = width * scaledTileSize,
                Y2 = y * scaledTileSize,
                Stroke = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)),
                StrokeThickness = 1
            };
            TilemapCanvas.Children.Add(line);
        }

        // Draw viewport overlay (visible screen area)
        DrawViewportOverlay(scaledTileSize);
    }

    /// <summary>
    /// Draws a red rectangle showing the visible viewport area.
    /// </summary>
    private void DrawViewportOverlay(double scaledTileSize)
    {
        var specs = _target.Specs.Tilemap;
        int viewportWidth = specs.DefaultWidth;
        int viewportHeight = specs.DefaultHeight;

        double rectWidth = viewportWidth * scaledTileSize;
        double rectHeight = viewportHeight * scaledTileSize;

        // Red rectangle for viewport
        var viewportRect = new System.Windows.Shapes.Rectangle
        {
            Width = rectWidth,
            Height = rectHeight,
            Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x4D, 0x4D)), // Bright red
            StrokeThickness = 3,
            Fill = null,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(viewportRect, 0);
        Canvas.SetTop(viewportRect, 0);
        TilemapCanvas.Children.Add(viewportRect);

        // Label showing viewport dimensions
        var label = new TextBlock
        {
            Text = $"VIEWPORT: {viewportWidth}×{viewportHeight}",
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x4D, 0x4D)),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            Padding = new Thickness(4, 2, 4, 2),
            IsHitTestVisible = false
        };

        Canvas.SetLeft(label, 8);
        Canvas.SetTop(label, 8);
        TilemapCanvas.Children.Add(label);
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Close();

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
        => Close();

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (CmbTilesetAsset.SelectedItem == null)
        {
            MessageBox.Show("Please select a tileset asset.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check if <New Palette> is selected
        if (CmbPalette.SelectedItem?.ToString() == "<New Palette>")
        {
            MessageBox.Show("Please create a palette before saving.\n\nClick on the palette dropdown and select <New Palette> to create one.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            
            // Open palette editor
            OpenPaletteEditor();
            return;
        }

        var base64Data = ConvertTilemapDataToBase64();
        System.Diagnostics.Debug.WriteLine($"[TilemapEditor] Saving tilemap data:");
        System.Diagnostics.Debug.WriteLine($"  Width: {TxtWidth.Text}");
        System.Diagnostics.Debug.WriteLine($"  Height: {TxtHeight.Text}");
        System.Diagnostics.Debug.WriteLine($"  Asset: {CmbTilesetAsset.SelectedItem}");
        System.Diagnostics.Debug.WriteLine($"  Data length: {base64Data.Length} chars");
        System.Diagnostics.Debug.WriteLine($"  First 50 chars: {(base64Data.Length > 50 ? base64Data.Substring(0, 50) : base64Data)}");

        // Convert Base64 back to int array for TilemapModule
        var bytes = Convert.FromBase64String(base64Data);
        var mapDataArray = new int[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            // Convert 255 (empty marker) back to -1 for storage
            mapDataArray[i] = bytes[i] == 255 ? -1 : bytes[i];
        }

        // Use TilemapModule property names
        ModuleData = new Dictionary<string, object>
        {
            ["moduleId"] = "tilemap",
            ["mapWidth"] = int.Parse(TxtWidth.Text),
            ["mapHeight"] = int.Parse(TxtHeight.Text),
            ["tilesAssetId"] = CmbTilesetAsset.SelectedItem.ToString()!,
            ["paletteRef"] = CmbPalette.SelectedItem?.ToString() ?? "",
            ["mapData"] = mapDataArray,
            ["mapAssetId"] = "",
            ["startTile"] = 0,
            ["mapX"] = 0,
            ["mapY"] = 0,
            ["solidTiles"] = Array.Empty<int>()
        };

        DialogResult = true;
        Close();
    }

    private string ConvertTilemapDataToBase64()
    {
        var currentLayer = _tilemapLayers[_currentLayerIndex];
        byte[] bytes = new byte[currentLayer.Length];

        for (int i = 0; i < currentLayer.Length; i++)
        {
            // Convert -1 (empty) to 255, keep valid tile IDs as-is
            int tileId = currentLayer[i];
            if (tileId < 0)
                bytes[i] = 255; // Use 255 as empty marker
            else
                bytes[i] = (byte)Math.Min(254, tileId); // 0-254 are valid tiles
        }

        return Convert.ToBase64String(bytes);
    }

    private void CmbTilesetAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTilesetAsset.SelectedItem == null) return;

        string assetId = CmbTilesetAsset.SelectedItem.ToString()!;
        var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);

        if (asset == null) return;

        // Update info
        TxtTilesetInfo.Text = $"{asset.FileName} ({asset.TileCount} tiles)";

        // Find VRAM region
        var region = _target.Specs.VramRegions.FirstOrDefault(r => r.Id == asset.VramRegionId);
        if (region != null)
        {
            TxtVramRegionInfo.Text = $"{region.Label}: {region.StartTile}-{region.EndTile} ({region.TileCount} tiles)";
        }

        // Load tileset image
        LoadTilesetImage(asset);
    }

    private void CmbLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentLayerIndex = CmbLayers.SelectedIndex;
        TxtLayerInfo.Text = $"Layer {_currentLayerIndex + 1} of {_target.Specs.Tilemap.MaxLayers}";
        RenderCanvas();
    }

    private void ChkShowCollision_CheckedChanged(object sender, RoutedEventArgs e)
    {
        // TODO: Toggle collision overlay
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPainting = true;
        PaintTile(e.GetPosition(TilemapCanvas));
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
        {
            PaintTile(e.GetPosition(TilemapCanvas));
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPainting = false;
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Erase tile (set to -1 for empty)
        int previousTileId = _selectedTileId;
        _selectedTileId = -1;
        PaintTile(e.GetPosition(TilemapCanvas));
        _selectedTileId = previousTileId;
    }

    private void PaintTile(Point position)
    {
        int tileSize = _target.Specs.TileWidth;
        double scaledTileSize = tileSize * _zoomLevel;

        int tileX = (int)(position.X / scaledTileSize);
        int tileY = (int)(position.Y / scaledTileSize);

        int width = int.Parse(TxtWidth.Text);
        int height = int.Parse(TxtHeight.Text);

        if (tileX < 0 || tileX >= width || tileY < 0 || tileY >= height)
            return;

        int index = tileY * width + tileX;
        var currentLayer = _tilemapLayers[_currentLayerIndex];

        // Safety check
        if (index >= currentLayer.Length)
            return;

        currentLayer[index] = _selectedTileId;

        // Render tile immediately
        RenderCanvas();
    }

    private void BtnZoomFit_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Calculate zoom to fit canvas in viewport
        _zoomLevel = 1.0;
        TxtZoomLevel.Text = "100%";
        RenderCanvas();
    }

    private void BtnZoom100_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = 1.0;
        TxtZoomLevel.Text = "100%";
        RenderCanvas();
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Clear all tiles in the current layer?",
            "Clear Map",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            Array.Fill(_tilemapLayers[_currentLayerIndex], -1);
            RenderCanvas();
        }
    }

    private void BtnFill_Click(object sender, RoutedEventArgs e)
    {
        // Fill entire layer with selected tile
        for (int i = 0; i < _tilemapLayers[_currentLayerIndex].Length; i++)
        {
            _tilemapLayers[_currentLayerIndex][i] = _selectedTileId;
        }
        RenderCanvas();
    }

    private void BtnExportPng_Click(object sender, RoutedEventArgs e)
    {
        if (_tilesetImage == null)
        {
            MessageBox.Show("No tileset loaded.", "Export PNG", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            FileName = "tilemap.png"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                int width = int.Parse(TxtWidth.Text);
                int height = int.Parse(TxtHeight.Text);
                int tileSize = _target.Specs.TileWidth;

                var bitmap = new RenderTargetBitmap(
                    width * tileSize,
                    height * tileSize,
                    96, 96,
                    PixelFormats.Pbgra32);

                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var currentLayer = _tilemapLayers[_currentLayerIndex];
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = y * width + x;
                            if (index < currentLayer.Length)
                            {
                                int tileId = currentLayer[index];
                                if (tileId >= 0)
                                {
                                    var tileImage = ExtractTile(tileId);
                                    if (tileImage != null)
                                    {
                                        context.DrawImage(tileImage, new Rect(x * tileSize, y * tileSize, tileSize, tileSize));
                                    }
                                }
                            }
                        }
                    }
                }

                bitmap.Render(visual);

                using var fileStream = new FileStream(dialog.FileName, FileMode.Create);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);

                MessageBox.Show($"Tilemap exported to {System.IO.Path.GetFileName(dialog.FileName)}", "Export PNG", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export PNG", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnImportAsset_Click(object sender, RoutedEventArgs e)
    {
        // Open AssetImporter with PreSelectRegion("background")
        var assetImporter = new Tool.AssetImporter.AssetImporterWindow(_target, _projectPath)
        {
            Owner = this
        };

        // Pre-select background region
        assetImporter.PreSelectRegion("background");

        if (assetImporter.ShowDialog() == true && assetImporter.ImportedAsset is not null)
        {
            var asset = assetImporter.ImportedAsset;

            // Check for duplicate ID
            if (_project.Assets.Any(a => a.Id == asset.Id))
            {
                MessageBox.Show(
                    $"An asset named '{asset.Id}' already exists.",
                    "Retruxel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Add asset to project
            _project.Assets.Add(asset);

            // Ask if user wants to use asset colors in palette
            var result = MessageBox.Show(
                $"Asset '{asset.Id}' imported successfully.\n\nDo you want to create a palette from this asset's colors?",
                "Create Palette",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                OpenPaletteEditorForAsset(asset);
            }

            // Refresh asset list
            LoadAssets();

            // Auto-select the new asset
            for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
            {
                if (CmbTilesetAsset.Items[i].ToString() == asset.Id)
                {
                    CmbTilesetAsset.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private async void BtnImportFromLiveLink_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_toolRegistry == null)
            {
                MessageBox.Show(
                    "Tool registry not available.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Get LiveLink tool
            var liveLinkTool = _toolRegistry.GetVisualTool("livelink");
            if (liveLinkTool == null)
            {
                MessageBox.Show(
                    "LiveLink tool not found. Make sure the plugin is installed.",
                    "Tool Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Open LiveLink in capture mode
            var liveLinkInput = new Dictionary<string, object>
            {
                ["mode"] = "capture",
                ["targetId"] = _target.TargetId,
                ["callerId"] = "tilemap_editor"
            };

            var liveLinkWindow = (Window)liveLinkTool.CreateWindow(liveLinkInput);
            liveLinkWindow.Owner = this;

            if (liveLinkWindow.ShowDialog() == true)
            {
                // Get imported asset data from LiveLink
                var moduleDataProp = liveLinkWindow.GetType().GetProperty("ModuleData");
                var moduleData = moduleDataProp?.GetValue(liveLinkWindow) as Dictionary<string, object>;

                if (moduleData == null || !moduleData.ContainsKey("importedAssetData"))
                {
                    MessageBox.Show(
                        "No data received from LiveLink.",
                        "Import Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var importedData = (ImportedAssetData)moduleData["importedAssetData"];

                // Use pipeline to convert ImportedAssetData → Tilemap Editor format
                var pipeline = new Pipelines.ImportedAssetToTilemapPipeline();
                var pipelineOptions = new Dictionary<string, object>
                {
                    ["project"] = _project,
                    ["projectPath"] = _projectPath,
                    ["targetId"] = _target.TargetId
                };

                var tilemapData = pipeline.ProcessTyped(importedData, pipelineOptions);

                // Save project to persist new asset
                if (_saveProjectCallback != null)
                {
                    await _saveProjectCallback.Invoke();
                }

                // Refresh asset list
                LoadAssets();

                // Auto-select the new asset
                var assetId = tilemapData["tilesAssetId"].ToString()!;
                for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
                {
                    if (CmbTilesetAsset.Items[i].ToString() == assetId)
                    {
                        CmbTilesetAsset.SelectedIndex = i;
                        break;
                    }
                }

                // Load tilemap data
                TxtWidth.Text = tilemapData["mapWidth"].ToString()!;
                TxtHeight.Text = tilemapData["mapHeight"].ToString()!;

                // Resize layers
                int width = (int)tilemapData["mapWidth"];
                int height = (int)tilemapData["mapHeight"];
                int newSize = width * height;

                for (int i = 0; i < _tilemapLayers.Count; i++)
                {
                    var newLayer = new int[newSize];
                    Array.Fill(newLayer, -1);
                    _tilemapLayers[i] = newLayer;
                }

                // Load tilemap data into current layer
                var mapData = (int[])tilemapData["mapData"];
                Array.Copy(mapData, _tilemapLayers[_currentLayerIndex], Math.Min(mapData.Length, newSize));

                // Render canvas
                RenderCanvas();

                // Ask if user wants to create palette from captured colors
                var palette = (uint[])tilemapData["palette"];
                if (palette.Length > 0)
                {
                    var result = MessageBox.Show(
                        $"LiveLink captured {palette.Length} colors.\n\nDo you want to create a palette from these colors?",
                        "Create Palette",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var asset = (AssetEntry)tilemapData["asset"];
                        OpenPaletteEditorForAsset(asset);
                    }
                }

                MessageBox.Show(
                    $"Successfully imported tilemap from LiveLink!\n\n" +
                    $"Tiles: {importedData.Tiles.Length}\n" +
                    $"Map: {width}x{height}\n" +
                    $"Palette: {palette.Length} colors",
                    "Import Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to import from LiveLink: {ex.Message}\n\n{ex.StackTrace}",
                "Import Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void BtnOptimize_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open TilePackerWindow
        MessageBox.Show("Tile optimization not implemented yet.", "Optimize", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void BtnTileZoom50_Click(object sender, RoutedEventArgs e)
    {
        _tileZoomLevel = 0.5;
        PopulateTilesetGrid();
    }
    
    private void BtnTileZoom100_Click(object sender, RoutedEventArgs e)
    {
        _tileZoomLevel = 1.0;
        PopulateTilesetGrid();
    }
    
    private void BtnTileZoom200_Click(object sender, RoutedEventArgs e)
    {
        _tileZoomLevel = 2.0;
        PopulateTilesetGrid();
    }

    /// <summary>
    /// Opens PaletteEditor to create a new palette from asset colors.
    /// </summary>
    private void OpenPaletteEditorForAsset(AssetEntry asset)
    {
        OpenPaletteEditor(asset);
    }

    /// <summary>
    /// Opens PaletteEditor to edit the currently selected palette.
    /// </summary>
    private void BtnEditPalette_Click(object sender, RoutedEventArgs e)
    {
        if (CmbPalette.SelectedItem == null || CmbPalette.SelectedItem.ToString() == "<New Palette>")
        {
            MessageBox.Show("Please select a palette to edit.", "No Palette Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string paletteId = CmbPalette.SelectedItem.ToString()!;
        
        // Find palette element in project
        var paletteElement = _project.Scenes
            .SelectMany(s => s.Elements)
            .FirstOrDefault(e => e.ModuleId == "palette" && e.ElementId == paletteId);

        if (paletteElement == null)
        {
            MessageBox.Show($"Palette '{paletteId}' not found in project.", "Palette Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_toolRegistry == null)
            {
                MessageBox.Show("Tool registry not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get palette editor tool
            var paletteEditorTool = _toolRegistry.GetVisualTool("palette_editor");
            if (paletteEditorTool == null)
            {
                MessageBox.Show("Palette Editor tool not found.", "Tool Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get palette provider extension from target
            var extension = _toolRegistry.GetTool($"palette_editor_ext_{_target.TargetId}");
            if (extension == null)
            {
                MessageBox.Show($"Target '{_target.DisplayName}' does not support Palette Editor yet.", "Not Supported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var extensionResult = extension.Execute(new Dictionary<string, object>());
            if (!extensionResult.ContainsKey("paletteProvider"))
            {
                MessageBox.Show($"Target '{_target.DisplayName}' palette extension is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var paletteProvider = (IPaletteProvider)extensionResult["paletteProvider"];

            // Extract existing palette data
            var moduleState = paletteElement.ModuleState;
            byte[]? existingColors = null;
            
            if (moduleState.TryGetProperty("colors", out var colorsProperty))
            {
                var colorsList = new List<byte>();
                foreach (var colorElement in colorsProperty.EnumerateArray())
                {
                    colorsList.Add((byte)colorElement.GetInt32());
                }
                existingColors = colorsList.ToArray();
            }

            // Open palette editor with existing data and elementId
            var paletteEditor = new Tool.PaletteEditor.PaletteEditorWindow(
                paletteProvider, 
                "tilemap_editor", 
                existingColors, 
                paletteId,  // Pass elementId so it can show usage
                _project)
            {
                Owner = this
            };

            if (paletteEditor.ShowDialog() == true && paletteEditor.ModuleData != null)
            {
                // Update palette element in project
                paletteElement.ModuleState = System.Text.Json.JsonDocument.Parse(
                    System.Text.Json.JsonSerializer.Serialize(paletteEditor.ModuleData)
                ).RootElement.Clone();

                // Save project
                _ = _saveProjectCallback?.Invoke();

                // Reload tileset with updated palette
                if (CmbTilesetAsset.SelectedItem != null)
                {
                    string assetId = CmbTilesetAsset.SelectedItem.ToString()!;
                    var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
                    if (asset != null)
                    {
                        LoadTilesetImage(asset);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open Palette Editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles palette ComboBox selection change.
    /// Opens PaletteEditor if <New Palette> is selected.
    /// Reloads tileset with new palette colors.
    /// </summary>
    private void CmbPalette_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Don't trigger during initialization
        if (_isInitializing) return;

        if (CmbPalette.SelectedItem?.ToString() == "<New Palette>")
        {
            OpenPaletteEditor();
        }
        else
        {
            // Reload tileset with new palette colors
            if (CmbTilesetAsset.SelectedItem != null)
            {
                string assetId = CmbTilesetAsset.SelectedItem.ToString()!;
                var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
                if (asset != null)
                {
                    LoadTilesetImage(asset);
                }
            }
        }
    }

    /// <summary>
    /// Handles palette ComboBox dropdown closed.
    /// Catches the case where user clicks on already-selected <New Palette>.
    /// </summary>
    private void CmbPalette_DropDownClosed(object sender, EventArgs e)
    {
        if (_isInitializing) return;

        if (CmbPalette.SelectedItem?.ToString() == "<New Palette>")
        {
            OpenPaletteEditor();
        }
    }

    /// <summary>
    /// Opens PaletteEditor to create a new palette.
    /// </summary>
    private void OpenPaletteEditor(AssetEntry? sourceAsset = null)
    {
        try
        {
            if (_toolRegistry == null)
            {
                MessageBox.Show(
                    "Tool registry not available.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Get palette editor tool
            var paletteEditorTool = _toolRegistry.GetVisualTool("palette_editor");
            if (paletteEditorTool == null)
            {
                MessageBox.Show(
                    "Palette Editor tool not found. Make sure the plugin is installed.",
                    "Tool Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Get palette provider extension from target
            var extension = _toolRegistry.GetTool($"palette_editor_ext_{_target.TargetId}");
            if (extension == null)
            {
                MessageBox.Show(
                    $"Target '{_target.DisplayName}' does not support Palette Editor yet.",
                    "Not Supported",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Execute extension to get palette provider
            var extensionResult = extension.Execute(new Dictionary<string, object>());
            if (!extensionResult.ContainsKey("paletteProvider"))
            {
                MessageBox.Show(
                    $"Target '{_target.DisplayName}' palette extension is invalid.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var paletteProvider = (IPaletteProvider)extensionResult["paletteProvider"];
            
            // Extract colors from asset if provided
            byte[]? initialColors = null;
            if (sourceAsset != null)
            {
                initialColors = ExtractColorsFromAsset(sourceAsset, paletteProvider);
            }
            
            var paletteEditor = new Tool.PaletteEditor.PaletteEditorWindow(paletteProvider, "tilemap_editor", initialColors, null, _project)
            {
                Owner = this
            };

            if (paletteEditor.ShowDialog() == true && paletteEditor.ModuleData != null)
            {
                // Use the user-provided name as ElementId
                var paletteName = paletteEditor.ModuleData.ContainsKey("name") 
                    ? paletteEditor.ModuleData["name"].ToString()!
                    : "palette";
                
                // Ensure unique ID
                var existingPalettes = _project.Scenes.SelectMany(s => s.Elements)
                    .Where(e => e.ModuleId == "palette")
                    .Select(e => e.ElementId)
                    .ToHashSet();
                
                var paletteId = paletteName;
                int suffix = 1;
                while (existingPalettes.Contains(paletteId))
                {
                    paletteId = $"{paletteName}_{suffix}";
                    suffix++;
                }

                var paletteElement = new SceneElementData
                {
                    ElementId = paletteId,
                    ModuleId = "palette",
                    ModuleState = System.Text.Json.JsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(paletteEditor.ModuleData)
                    ).RootElement.Clone(),
                    TileX = 0,
                    TileY = 0,
                    Trigger = "OnStart"
                };

                if (_project.Scenes.Count > 0)
                {
                    _project.Scenes[0].Elements.Add(paletteElement);
                    
                    // Add visual element to SceneEditorView if available
                    if (_sceneEditor != null)
                    {
                        var addMethod = _sceneEditor.GetType().GetMethod("AddElementFromData");
                        addMethod?.Invoke(_sceneEditor, new object[] { paletteElement });
                    }
                    
                    // Save project and keep it marked as dirty
                    _ = _saveProjectCallback?.Invoke();
                }

                ConfigurePaletteUI(_target.Specs.Tilemap.PaletteMode, _target.Specs.Tilemap.PaletteBitsPerTile);

                for (int i = 0; i < CmbPalette.Items.Count; i++)
                {
                    if (CmbPalette.Items[i].ToString() == paletteId)
                    {
                        CmbPalette.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                var previousSelection = CmbPalette.Items.Cast<string>().FirstOrDefault(s => s != "<New Palette>");
                if (previousSelection != null)
                {
                    CmbPalette.SelectedItem = previousSelection;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open Palette Editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Helper Methods ────────────────────────────────────────────────────────

    private void LoadTilesetImage(AssetEntry asset)
    {
        try
        {
            var absPath = System.IO.Path.Combine(_projectPath, asset.RelativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!File.Exists(absPath))
            {
                MessageBox.Show($"Tileset image not found: {absPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(absPath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            _tilesetImage = bitmap;
            _tilesetColumns = asset.SourceWidth / _target.Specs.TileWidth;
            _tilesetRows = asset.SourceHeight / _target.Specs.TileHeight;

            // Populate tileset grid
            PopulateTilesetGrid();

            // Refresh canvas
            RenderCanvas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load tileset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PopulateTilesetGrid()
    {
        if (_tilesetImage == null) return;

        TilesetGrid.Items.Clear();

        int tileSize = _target.Specs.TileWidth;
        int totalTiles = _tilesetColumns * _tilesetRows;
        int scaledSize = (int)(tileSize * _tileZoomLevel);

        // Add tile buttons
        for (int tileId = 0; tileId < totalTiles; tileId++)
        {
            var border = new Border
            {
                Width = scaledSize + 2,
                Height = scaledSize + 2,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(1),
                Cursor = Cursors.Hand,
                Tag = tileId
            };

            var image = new System.Windows.Controls.Image
            {
                Width = scaledSize,
                Height = scaledSize,
                Source = ExtractTile(tileId),
                Stretch = System.Windows.Media.Stretch.Fill
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

            border.Child = image;
            border.MouseLeftButtonDown += (s, e) =>
            {
                _selectedTileId = (int)((Border)s).Tag;
                UpdateTileSelection();
            };

            TilesetGrid.Items.Add(border);
        }

        UpdateTileSelection();
    }

    private void UpdateTileSelection()
    {
        foreach (var item in TilesetGrid.Items)
        {
            if (item is Border border)
            {
                int tileId = (int)border.Tag;
                if (tileId == _selectedTileId)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71));
                    border.BorderThickness = new Thickness(2);
                }
                else
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75));
                    border.BorderThickness = new Thickness(1);
                }
            }
        }
        
        if (_tilesetImage != null && _selectedTileId >= 0)
        {
            var tileImage = ExtractTile(_selectedTileId);
            if (tileImage != null)
            {
                ImgSelectedTile.Source = tileImage;
                ImgSelectedTile.Stretch = System.Windows.Media.Stretch.Fill;
                TxtSelectedTileInfo.Text = $"Tile ID: {_selectedTileId}";
            }
        }
    }

    private BitmapSource ExtractTile(int tileId)
    {
        if (_tilesetImage == null) return null!;

        int tileSize = _target.Specs.TileWidth;
        int srcX = (tileId % _tilesetColumns) * tileSize;
        int srcY = (tileId / _tilesetColumns) * tileSize;

        var croppedBitmap = new CroppedBitmap(_tilesetImage, new Int32Rect(srcX, srcY, tileSize, tileSize));
        croppedBitmap.Freeze();
        return croppedBitmap;
    }

    private void RenderTileAt(int x, int y, int tileId, double scaledTileSize)
    {
        if (_tilesetImage == null || tileId < 0) return;

        var tileImage = ExtractTile(tileId);
        if (tileImage == null) return;

        var image = new System.Windows.Controls.Image
        {
            Width = scaledTileSize,
            Height = scaledTileSize,
            Source = tileImage,
            Stretch = System.Windows.Media.Stretch.Fill
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

        Canvas.SetLeft(image, x * scaledTileSize);
        Canvas.SetTop(image, y * scaledTileSize);
        TilemapCanvas.Children.Add(image);
    }
    
    /// <summary>
    /// Extracts unique colors from an asset and maps them to hardware palette indices.
    /// </summary>
    private byte[]? ExtractColorsFromAsset(AssetEntry asset, IPaletteProvider paletteProvider)
    {
        try
        {
            var absPath = System.IO.Path.Combine(_projectPath, asset.RelativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!File.Exists(absPath)) return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(absPath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            var stride = width * 4;
            var pixels = new byte[height * stride];
            bitmap.CopyPixels(pixels, stride, 0);

            // Extract unique colors (limit to palette slot count)
            var uniqueColors = new HashSet<(byte R, byte G, byte B)>();
            for (int i = 0; i < pixels.Length; i += 4)
            {
                var b = pixels[i];
                var g = pixels[i + 1];
                var r = pixels[i + 2];
                var a = pixels[i + 3];
                
                if (a > 0) // Skip transparent pixels
                    uniqueColors.Add((r, g, b));
                
                if (uniqueColors.Count >= paletteProvider.SlotCount)
                    break;
            }

            // Map colors to hardware palette
            var paletteIndices = new byte[paletteProvider.SlotCount];
            var hardwareColors = paletteProvider.HardwareColors;
            int slotIndex = 0;
            
            foreach (var color in uniqueColors.Take(paletteProvider.SlotCount))
            {
                // Find closest hardware color
                int closestIndex = 0;
                double minDistance = double.MaxValue;
                
                for (int i = 0; i < hardwareColors.Length; i++)
                {
                    if (hardwareColors[i] is Retruxel.Core.Models.HardwareColor hwColor)
                    {
                        var distance = Math.Sqrt(
                            Math.Pow(color.R - hwColor.R, 2) +
                            Math.Pow(color.G - hwColor.G, 2) +
                            Math.Pow(color.B - hwColor.B, 2));
                        
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestIndex = i;
                        }
                    }
                }
                
                paletteIndices[slotIndex++] = (byte)closestIndex;
            }

            return paletteIndices;
        }
        catch
        {
            return null;
        }
    }
}
