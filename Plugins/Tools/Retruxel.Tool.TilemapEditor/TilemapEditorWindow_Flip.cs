using System.Windows.Input;

namespace Retruxel.Tool.TilemapEditor;

/// <summary>
/// Handles flip H/V hotkeys for tile painting.
/// </summary>
public partial class TilemapEditorWindow
{
    /// <summary>
    /// Initializes flip hotkeys (H = flip horizontal, V = flip vertical).
    /// </summary>
    private void InitializeFlipHotkeys()
    {
        KeyDown += Window_KeyDown;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // H key = toggle horizontal flip
        if (e.Key == Key.H && _selectedTileIds.Count == 1)
        {
            _selectedFlipH = !_selectedFlipH;
            UpdateSelectedTilePreview();
            e.Handled = true;
        }

        // V key = toggle vertical flip
        if (e.Key == Key.V && _selectedTileIds.Count == 1)
        {
            _selectedFlipV = !_selectedFlipV;
            UpdateSelectedTilePreview();
            e.Handled = true;
        }

        // R key = reset flip
        if (e.Key == Key.R && _selectedTileIds.Count == 1)
        {
            _selectedFlipH = false;
            _selectedFlipV = false;
            UpdateSelectedTilePreview();
            e.Handled = true;
        }
    }
}
