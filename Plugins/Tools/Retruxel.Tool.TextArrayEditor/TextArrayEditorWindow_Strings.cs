using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Tool.TextArrayEditor;

public partial class TextArrayEditorWindow
{
    #region Strings Management

    private void RefreshStringsList()
    {
        StringsList.Children.Clear();

        if (_state.Languages.Count == 0 || _activeLanguageIndex >= _state.Languages.Count)
            return;

        var currentLang = _state.Languages[_activeLanguageIndex];

        for (int i = 0; i < currentLang.Strings.Count; i++)
        {
            var index = i; // Capture for closure
            var stringValue = currentLang.Strings[i];

            var grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 1),
                Background = (Brush)FindResource("BrushSurfaceContainerLow")
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            // Index
            var txtIndex = new TextBlock
            {
                Text = i.ToString("D3"),
                Style = (Style)FindResource("TextCode"),
                Foreground = (Brush)FindResource("BrushPrimary"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 8, 12, 8)
            };
            Grid.SetColumn(txtIndex, 0);
            grid.Children.Add(txtIndex);

            // String input
            var txtInput = new TextBox
            {
                Text = stringValue
            };
            txtInput.TextChanged += (s, e) =>
            {
                currentLang.Strings[index] = txtInput.Text;
                if (index == _selectedStringIndex)
                    RenderPreview(txtInput.Text);
            };
            txtInput.GotFocus += (s, e) =>
            {
                _selectedStringIndex = index;
                RenderPreview(txtInput.Text);
            };
            Grid.SetColumn(txtInput, 1);
            grid.Children.Add(txtInput);

            // Delete button
            var btnDelete = new Button
            {
                Content = "✕",
                Style = (Style)FindResource("ButtonGhost"),
                Foreground = (Brush)FindResource("BrushError"),
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 4, 0)
            };
            btnDelete.Click += (s, e) => RemoveStringAtIndex(index);
            Grid.SetColumn(btnDelete, 2);
            grid.Children.Add(btnDelete);

            StringsList.Children.Add(grid);
        }
    }

    private void BtnAddString_Click(object sender, RoutedEventArgs e)
    {
        // Add empty string to all languages
        foreach (var lang in _state.Languages)
        {
            lang.Strings.Add("");
        }

        RefreshStringsList();
    }

    private void RemoveStringAtIndex(int index)
    {
        // Confirm deletion
        var result = MessageBox.Show(
            $"Remove string at index {index} from ALL languages?",
            "Confirm Deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        // Remove from all languages
        foreach (var lang in _state.Languages)
        {
            if (index < lang.Strings.Count)
                lang.Strings.RemoveAt(index);
        }

        RefreshStringsList();
    }

    #endregion
}
