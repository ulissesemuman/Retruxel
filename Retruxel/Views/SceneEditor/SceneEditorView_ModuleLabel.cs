using Retruxel.Core.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    /// <summary>
    /// Builds a visual label for a module showing:
    /// - Category icon (colored square)
    /// - Module type (TEXT, TILEMAP, PALETTE)
    /// - Content preview (user ID or property value)
    /// </summary>
    private UIElement BuildModuleLabel(SceneElement element)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };

        // Top row: icon + type
        var topRow = new StackPanel { Orientation = Orientation.Horizontal };
        
        var icon = new TextBlock
        {
            Text = "⬛",
            FontSize = 8,
            Margin = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(GetCategoryColor(element))
        };

        var moduleType = new TextBlock
        {
            Text = GetModuleTypeName(element),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 8,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };

        topRow.Children.Add(icon);
        topRow.Children.Add(moduleType);

        // Bottom row: preview
        var preview = new TextBlock
        {
            Text = GetModulePreview(element),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 7,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 120
        };

        panel.Children.Add(topRow);
        panel.Children.Add(preview);

        return panel;
    }

    /// <summary>
    /// Returns display text for event panel: "TYPE: preview"
    /// </summary>
    private string GetModuleDisplayText(SceneElement element)
    {
        var type = GetModuleTypeName(element);
        var preview = GetModulePreview(element);
        return $"{type}: {preview}";
    }

    /// <summary>
    /// Gets category color: Logic=blue, Graphic=green, Audio=purple
    /// </summary>
    private Color GetCategoryColor(SceneElement element)
    {
        if (_moduleRegistry is null) return Color.FromRgb(0x8E, 0xFF, 0x71);

        if (_moduleRegistry.LogicModules.ContainsKey(element.ModuleId))
            return Color.FromRgb(0x81, 0xEC, 0xFF); // Blue

        if (_moduleRegistry.GraphicModules.ContainsKey(element.ModuleId))
            return Color.FromRgb(0x8E, 0xFF, 0x71); // Green

        if (_moduleRegistry.AudioModules.ContainsKey(element.ModuleId))
            return Color.FromRgb(0xD4, 0x8E, 0xFF); // Purple

        return Color.FromRgb(0x8E, 0xFF, 0x71);
    }

    /// <summary>
    /// Gets short module type name: "text.display" → "TEXT"
    /// </summary>
    private string GetModuleTypeName(SceneElement element)
    {
        var parts = element.ModuleId.Split('.');
        return parts.Length > 0 ? parts[0].ToUpper() : element.ModuleId.ToUpper();
    }

    /// <summary>
    /// Gets preview text: User ID or first relevant property
    /// </summary>
    private string GetModulePreview(SceneElement element)
    {
        if (!string.IsNullOrEmpty(element.UserId))
            return element.UserId;

        if (element.Module is not IModule module)
            return element.ElementId[..8];

        // Try to extract meaningful preview from module properties
        var json = module.Serialize();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Text module: show text content
            if (root.TryGetProperty("text", out var text))
            {
                var str = text.GetString() ?? "";
                return str.Length > 15 ? str[..15] + "..." : str;
            }

            // Tilemap/Sprite: show asset ID
            if (root.TryGetProperty("tilesAssetId", out var assetId))
            {
                var str = assetId.GetString() ?? "";
                return string.IsNullOrEmpty(str) ? "(no asset)" : str;
            }

            // Palette: show color count or index
            if (root.TryGetProperty("paletteIndex", out var palIdx))
                return $"PAL{palIdx.GetInt32()}";
        }
        catch { }

        return element.ElementId[..8];
    }
}
