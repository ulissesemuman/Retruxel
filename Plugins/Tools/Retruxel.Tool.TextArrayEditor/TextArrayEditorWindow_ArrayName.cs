using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Tool.TextArrayEditor;

public partial class TextArrayEditorWindow
{
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
}
