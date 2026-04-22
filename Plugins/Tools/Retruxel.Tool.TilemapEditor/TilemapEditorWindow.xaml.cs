using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
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

    private int _selectedTileId = 0;
    private int _currentLayerIndex = 0;
    private double _zoomLevel = 1.0;
    private bool _isPainting = false;

    // Tilemap data: [layer][y * width + x] = tileId
    private List<int[]> _tilemapLayers = new();

    // Tileset image
    private BitmapSource? _tilesetImage;
    private int _tilesetColumns = 0;
    private int _tilesetRows = 0;

    // Module data to return
    public Dictionary<string, object>? ModuleData { get; private set; }

    public TilemapEditorWindow(ITarget target, RetruxelProject project, string projectPath)
    {
        InitializeComponent();

        _target = target;
        _project = project;
        _projectPath = projectPath;

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

        // Set default dimensions
        TxtWidth.Text = specs.DefaultWidth.ToString();
        TxtHeight.Text = specs.DefaultHeight.ToString();

        // Initialize layers
        int layerCount = specs.MaxLayers;
        for (int i = 0; i < layerCount; i++)
        {
            int width = specs.DefaultWidth;
            int height = specs.DefaultHeight;
            _tilemapLayers.Add(new int[width * height]);
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
    }

    /// <summary>
    /// Loads existing module data into the editor.
    /// Called when editing an existing tilemap module.
    /// </summary>
    public void LoadModuleData(Dictionary<string, object> moduleData)
    {
        if (moduleData.ContainsKey("name"))
            TxtName.Text = moduleData["name"].ToString()!;

        if (moduleData.ContainsKey("width"))
            TxtWidth.Text = moduleData["width"].ToString()!;

        if (moduleData.ContainsKey("height"))
            TxtHeight.Text = moduleData["height"].ToString()!;

        if (moduleData.ContainsKey("tilesAssetId"))
        {
            string assetId = moduleData["tilesAssetId"].ToString()!;
            for (int i = 0; i < CmbTilesetAsset.Items.Count; i++)
            {
                if (CmbTilesetAsset.Items[i].ToString() == assetId)
                {
                    CmbTilesetAsset.SelectedIndex = i;
                    break;
                }
            }
        }

        if (moduleData.ContainsKey("data"))
        {
            string base64Data = moduleData["data"].ToString()!;
            byte[] bytes = Convert.FromBase64String(base64Data);
            
            int width = int.Parse(TxtWidth.Text);
            int height = int.Parse(TxtHeight.Text);
            
            for (int i = 0; i < bytes.Length && i < _tilemapLayers[_currentLayerIndex].Length; i++)
            {
                _tilemapLayers[_currentLayerIndex][i] = bytes[i];
            }
            
            RenderCanvas();
        }
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
    /// Renders the tilemap canvas with grid lines.
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
        if (_tilesetImage != null)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int tileId = _tilemapLayers[_currentLayerIndex][index];

                    if (tileId > 0)
                    {
                        RenderTileAt(x, y, tileId, scaledTileSize);
                    }
                }
            }
        }

        // Draw grid lines on top
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
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("Please enter a name for the tilemap.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbTilesetAsset.SelectedItem == null)
        {
            MessageBox.Show("Please select a tileset asset.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ModuleData = new Dictionary<string, object>
        {
            ["moduleId"] = "tilemap",
            ["name"] = TxtName.Text.Trim(),
            ["width"] = int.Parse(TxtWidth.Text),
            ["height"] = int.Parse(TxtHeight.Text),
            ["tilesAssetId"] = CmbTilesetAsset.SelectedItem.ToString()!,
            ["paletteModuleId"] = CmbPalette.SelectedItem?.ToString() ?? "palette_0",
            ["data"] = ConvertTilemapDataToBase64(),
            ["collision"] = null
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
            bytes[i] = (byte)Math.Min(255, currentLayer[i]);
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
        // Erase tile (set to 0)
        _selectedTileId = 0;
        PaintTile(e.GetPosition(TilemapCanvas));
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
        _tilemapLayers[_currentLayerIndex][index] = _selectedTileId;

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
            Array.Clear(_tilemapLayers[_currentLayerIndex], 0, _tilemapLayers[_currentLayerIndex].Length);
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
        // TODO: Render tilemap to PNG and save
        MessageBox.Show("Export PNG not implemented yet.", "Export PNG", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private void BtnOptimize_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open TilePackerWindow
        MessageBox.Show("Tile optimization not implemented yet.", "Optimize", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Opens PaletteEditor to create a new palette from asset colors.
    /// </summary>
    private void OpenPaletteEditorForAsset(AssetEntry asset)
    {
        OpenPaletteEditor(asset);
    }

    /// <summary>
    /// Handles palette ComboBox selection change.
    /// Opens PaletteEditor if <New Palette> is selected.
    /// </summary>
    private void CmbPalette_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPalette.SelectedItem?.ToString() == "<New Palette>")
        {
            // Check if there are free slots
            var paletteModules = _project.Scenes
                .SelectMany(s => s.Elements)
                .Where(el => el.ModuleId == "palette")
                .ToList();

            int maxPalettes = _target.Specs.Tilemap.PaletteMode == PaletteMode.PerTile
                ? (1 << _target.Specs.Tilemap.PaletteBitsPerTile)
                : int.MaxValue;

            if (paletteModules.Count >= maxPalettes)
            {
                var result = MessageBox.Show(
                    $"All {maxPalettes} palette slots are filled.\n\nDo you want to replace the current palette?",
                    "Palette Slots Full",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    // Revert selection
                    CmbPalette.SelectedIndex = 0;
                    return;
                }
            }

            // Open PaletteEditor via ToolRegistry
            OpenPaletteEditor();
        }
    }

    /// <summary>
    /// Opens PaletteEditor to create a new palette.
    /// </summary>
    private void OpenPaletteEditor(AssetEntry? sourceAsset = null)
    {
        // Get ToolRegistry from VisualToolInvoker (static)
        // For now, create palette directly without PaletteEditor
        var paletteId = $"palette_{_project.Scenes.SelectMany(s => s.Elements).Count(e => e.ModuleId == "palette")}";
        
        var paletteElement = new SceneElementData
        {
            ElementId = paletteId,
            ModuleId = "palette",
            ModuleState = System.Text.Json.JsonDocument.Parse(
                sourceAsset != null
                    ? $"{{\"name\":\"{sourceAsset.Id}_palette\",\"sourceAssetId\":\"{sourceAsset.Id}\"}}"
                    : $"{{\"name\":\"palette_{paletteId}\"}}"
            ).RootElement.Clone(),
            TileX = 0,
            TileY = 0,
            Trigger = "OnStart"
        };

        // Add to first scene
        if (_project.Scenes.Count > 0)
        {
            _project.Scenes[0].Elements.Add(paletteElement);
        }

        // Refresh palette ComboBox
        ConfigurePaletteUI(_target.Specs.Tilemap.PaletteMode, _target.Specs.Tilemap.PaletteBitsPerTile);

        // Select the new palette
        for (int i = 0; i < CmbPalette.Items.Count; i++)
        {
            if (CmbPalette.Items[i].ToString() == paletteId)
            {
                CmbPalette.SelectedIndex = i;
                break;
            }
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

        // Add tile buttons
        for (int tileId = 0; tileId < totalTiles; tileId++)
        {
            var border = new Border
            {
                Width = tileSize + 2,
                Height = tileSize + 2,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(1),
                Cursor = Cursors.Hand,
                Tag = tileId
            };

            var image = new System.Windows.Controls.Image
            {
                Width = tileSize,
                Height = tileSize,
                Source = ExtractTile(tileId),
                Stretch = System.Windows.Media.Stretch.None
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
        if (_tilesetImage == null || tileId <= 0) return;

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
}
