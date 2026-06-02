using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.PrefabEditor;

public partial class PrefabEditorWindow
{
    private static readonly Regex _identifierRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    private void PopulateIdentity()
    {
        TxtName.Text        = _prefab.PrefabId;
        TxtDisplayName.Text = _prefab.DisplayName;
    }

    private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
    {
        _prefab.PrefabId = TxtName.Text.Trim();
        UpdateTitleBar();
        ValidateAll();
    }

    private bool ValidateIdentity(out string? error)
    {
        var id = TxtName.Text.Trim();

        if (string.IsNullOrEmpty(id))
        {
            error = "Name is required.";
            return false;
        }

        if (!_identifierRegex.IsMatch(id))
        {
            error = "Must be a valid C identifier (letters, digits, underscores, no spaces).";
            return false;
        }

        // Uniqueness check — allow the prefab to keep its own original id
        var duplicate = _project.Prefabs
            .Any(p => p.PrefabId == id && p.PrefabId != _originalPrefabId);

        if (duplicate)
        {
            error = $"A prefab named '{id}' already exists.";
            return false;
        }

        error = null;
        return true;
    }
}
