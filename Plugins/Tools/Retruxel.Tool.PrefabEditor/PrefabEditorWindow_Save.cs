using System.Windows;

namespace Retruxel.Tool.PrefabEditor;

public partial class PrefabEditorWindow
{
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateAll()) return;

        // Commit fields that aren't bound live
        _prefab.DisplayName = TxtDisplayName.Text.Trim();

        if (int.TryParse(TxtWidthTiles.Text, out var w) && w >= 1)
            _prefab.WidthTiles = w;
        if (int.TryParse(TxtHeightTiles.Text, out var h) && h >= 1)
            _prefab.HeightTiles = h;

        _saveCallback?.Invoke(_prefab);
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Runs all validation rules. Updates inline error labels and footer error.
    /// Returns true if all rules pass.
    /// </summary>
    private bool ValidateAll()
    {
        var valid = true;

        // Identity
        if (!ValidateIdentity(out var nameError))
        {
            TxtNameError.Text       = nameError;
            TxtNameError.Visibility = System.Windows.Visibility.Visible;
            valid = false;
        }
        else
        {
            TxtNameError.Visibility = System.Windows.Visibility.Collapsed;
        }

        // Dimensions
        if (!int.TryParse(TxtWidthTiles.Text, out var w) || w < 1 ||
            !int.TryParse(TxtHeightTiles.Text, out var h) || h < 1)
        {
            valid = false;
        }

        // Input mapping references — ensure no dangling InstanceIds
        if (_prefab.InputMapping is not null)
        {
            var validIds = new System.Collections.Generic.HashSet<string>(
                _prefab.Actions.Select(a => a.InstanceId));

            foreach (var (btn, ids) in _prefab.InputMapping.ButtonMappings)
            {
                if (ids.Any(id => !validIds.Contains(id)))
                {
                    valid = false;
                    break;
                }
            }
        }

        BtnSave.IsEnabled            = valid;
        TxtValidationError.Visibility = valid
            ? System.Windows.Visibility.Collapsed
            : System.Windows.Visibility.Visible;

        if (!valid)
            TxtValidationError.Text = "Fix errors above before saving.";

        return valid;
    }
}
