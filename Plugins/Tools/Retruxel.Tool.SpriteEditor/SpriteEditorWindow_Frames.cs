using Retruxel.Tool.SpriteEditor.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private void RefreshFramesList()
    {
        FramesListBox.Items.Clear();

        for (int i = 0; i < _state.Frames.Count; i++)
        {
            var frame = _state.Frames[i];
            var tagPrefix = string.IsNullOrWhiteSpace(frame.Tag) ? "" : $"[{frame.Tag}] ";
            var item = new ListBoxItem
            {
                Content = $"{tagPrefix}{frame.Name} ({frame.Tiles.Count} tiles)",
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
            UpdateFrameDurationField();
            UpdateFrameTagField();
            RefreshHitboxList();
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
        UpdateFrameTagField();
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
        UpdateFrameTagField();
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
        UpdateFrameDurationField();
        UpdateFrameTagField();
        OnSpriteChanged();
    }

    private void UpdateFrameDurationField()
    {
        if (_state.Frames.Count == 0)
            return;

        var currentFrame = _state.Frames[_state.CurrentFrameIndex];
        TxtFrameDuration.Text = currentFrame.Duration.ToString();
        UpdateFrameTagField();
    }

    private void TxtFrameDuration_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing || _state.Frames.Count == 0)
            return;

        if (int.TryParse(TxtFrameDuration.Text, out int duration) && duration > 0)
        {
            var currentFrame = _state.Frames[_state.CurrentFrameIndex];
            currentFrame.Duration = duration;
            UpdateAnimationSpeed();
        }
    }

    private void InitializeFrameTags()
    {
        CmbFrameTag.Items.Clear();
        foreach (var tag in new[] { "", "idle", "walk", "jump", "attack" })
            CmbFrameTag.Items.Add(tag);

        UpdateFrameTagField();
    }

    private void UpdateFrameTagField()
    {
        if (CmbFrameTag == null || _state.Frames.Count == 0)
            return;

        var tag = _state.Frames[_state.CurrentFrameIndex].Tag;
        if (!CmbFrameTag.Items.Cast<object>().Any(i => string.Equals(i?.ToString(), tag, System.StringComparison.OrdinalIgnoreCase)))
            CmbFrameTag.Items.Add(tag);

        CmbFrameTag.Text = tag;
    }

    private void CmbFrameTag_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || _state.Frames.Count == 0 || CmbFrameTag.SelectedItem is null)
            return;

        SetCurrentFrameTag(CmbFrameTag.SelectedItem.ToString() ?? string.Empty);
    }

    private void CmbFrameTag_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _state.Frames.Count == 0)
            return;

        SetCurrentFrameTag(CmbFrameTag.Text);
    }

    private void SetCurrentFrameTag(string tag)
    {
        var normalized = tag.Trim();
        var currentFrame = _state.Frames[_state.CurrentFrameIndex];
        if (currentFrame.Tag == normalized)
            return;

        currentFrame.Tag = normalized;
        if (!string.IsNullOrWhiteSpace(normalized) &&
            !CmbFrameTag.Items.Cast<object>().Any(i => string.Equals(i?.ToString(), normalized, System.StringComparison.OrdinalIgnoreCase)))
        {
            CmbFrameTag.Items.Add(normalized);
        }

        RefreshFramesList();
        OnSpriteChanged();
    }

    private void OnionSkinSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
            return;

        _state.OnionSkinPrevious = ChkOnionPrev.IsChecked == true;
        _state.OnionSkinNext = ChkOnionNext.IsChecked == true;
        RenderCanvas();
    }

    private void SldOnionOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing)
            return;

        _state.OnionSkinOpacity = SldOnionOpacity.Value / 100.0;
        RenderCanvas();
    }
}
