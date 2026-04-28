using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
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
}
