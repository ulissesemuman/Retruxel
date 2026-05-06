using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Tool.TextArrayEditor;

public partial class TextArrayEditorWindow
{
    #region Language Management

    private void RefreshLanguageTabs()
    {
        LanguageTabs.Children.Clear();

        for (int i = 0; i < _state.Languages.Count; i++)
        {
            var lang = _state.Languages[i];
            var index = i; // Capture for closure

            var btn = new Button
            {
                Content = $"[{lang.Code}]",
                Style = (Style)FindResource("ButtonGhost"),
                Padding = new Thickness(12, 0, 12, 0),
                Height = 32,
                Margin = new Thickness(0, 0, 4, 0)
            };

            if (index == _activeLanguageIndex)
                btn.Foreground = (Brush)FindResource("BrushPrimary");
            else
                btn.Foreground = (Brush)FindResource("BrushOnSurfaceVariant");

            btn.Click += (s, e) => SelectLanguage(index);

            LanguageTabs.Children.Add(btn);
        }

        // Add [+] button
        var addBtn = new Button
        {
            Content = "+",
            Style = (Style)FindResource("ButtonGhost"),
            Padding = new Thickness(12, 0, 12, 0),
            Height = 32,
            Foreground = (Brush)FindResource("BrushPrimary")
        };
        addBtn.Click += BtnAddLanguage_Click;
        LanguageTabs.Children.Add(addBtn);
    }

    private void SelectLanguage(int index)
    {
        _activeLanguageIndex = index;
        RefreshLanguageTabs();
        RefreshStringsList();
    }

    private void BtnAddLanguage_Click(object sender, RoutedEventArgs e)
    {
        // Prompt for language code
        var dialog = new TextInputDialog("New Language", "Enter language code (e.g., 'en', 'pt', 'jp'):");
        if (dialog.ShowDialog() == true)
        {
            var code = dialog.InputText.Trim();
            if (string.IsNullOrEmpty(code))
                return;

            // Check if language already exists
            if (_state.Languages.Any(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"Language '{code}' already exists.", "Duplicate Language",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create new language with same string count as existing languages
            var stringCount = _state.Languages.Count > 0 ? _state.Languages[0].Strings.Count : 1;
            var newLang = new TextLanguage
            {
                Code = code,
                Strings = Enumerable.Repeat("", stringCount).ToList()
            };

            _state.Languages.Add(newLang);
            _activeLanguageIndex = _state.Languages.Count - 1;

            RefreshLanguageTabs();
            RefreshStringsList();
        }
    }

    #endregion
}
