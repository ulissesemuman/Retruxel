using System.Windows;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void BtnOffsetUp_Click(object sender, RoutedEventArgs e)
    {
        _mapOffsetY--;
        UpdateMapOffset();
    }

    private void BtnOffsetDown_Click(object sender, RoutedEventArgs e)
    {
        _mapOffsetY++;
        UpdateMapOffset();
    }

    private void BtnOffsetLeft_Click(object sender, RoutedEventArgs e)
    {
        _mapOffsetX--;
        UpdateMapOffset();
    }

    private void BtnOffsetRight_Click(object sender, RoutedEventArgs e)
    {
        _mapOffsetX++;
        UpdateMapOffset();
    }

    private void UpdateMapOffset()
    {
        TxtMapOffset.Text = $"{_mapOffsetX},{_mapOffsetY}";
        RenderCanvas();
    }
}
