using System.Windows;
using System.Windows.Controls;
using Retruxel.Tool.SpriteEditor.Models;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private void RefreshFramesList()
    {
        FramesListBox.Items.Clear();

        for (int i = 0; i < _state.Frames.Count; i++)
        {
            var frame = _state.Frames[i];
            var item = new ListBoxItem
            {
                Content = $"{frame.Name} ({frame.Tiles.Count} tiles)",
                Tag = i,
                Foreground = (System.Windows.Media.Brush)FindResource("BrushOnSurface"),
                Background = (System.Windows.Media.Brush)FindResource("BrushSurfaceContainerLow"),
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(8, 4, 8, 4)
            };

            FramesListBox.Items.Add(item);
        }

        if (_state.CurrentFrameIndex < FramesListBox.Items.Count)
        {
            FramesListBox.SelectedIndex = _state.CurrentFrameIndex;
        }
    }

    private void FramesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FramesListBox.SelectedItem is ListBoxItem item && item.Tag is int index)
        {
            _state.CurrentFrameIndex = index;
            RenderCanvas();
        }
    }

    private void BtnAddFrame_Click(object sender, RoutedEventArgs e)
    {
        var newFrame = new SpriteFrame
        {
            Name = $"Frame {_state.Frames.Count + 1}",
            Duration = 100
        };

        _state.Frames.Add(newFrame);
        _state.CurrentFrameIndex = _state.Frames.Count - 1;

        RefreshFramesList();
        OnSpriteChanged();
    }

    private void BtnDuplicateFrame_Click(object sender, RoutedEventArgs e)
    {
        if (_state.Frames.Count == 0)
            return;

        var currentFrame = _state.Frames[_state.CurrentFrameIndex];
        var duplicatedFrame = currentFrame.Clone();
        duplicatedFrame.Name = $"{currentFrame.Name} (Copy)";

        _state.Frames.Insert(_state.CurrentFrameIndex + 1, duplicatedFrame);
        _state.CurrentFrameIndex++;

        RefreshFramesList();
        OnSpriteChanged();
    }

    private void BtnDeleteFrame_Click(object sender, RoutedEventArgs e)
    {
        if (_state.Frames.Count <= 1)
        {
            MessageBox.Show("Cannot delete the last frame.", "Delete Frame", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _state.Frames.RemoveAt(_state.CurrentFrameIndex);

        if (_state.CurrentFrameIndex >= _state.Frames.Count)
        {
            _state.CurrentFrameIndex = _state.Frames.Count - 1;
        }

        RefreshFramesList();
        OnSpriteChanged();
    }
}
