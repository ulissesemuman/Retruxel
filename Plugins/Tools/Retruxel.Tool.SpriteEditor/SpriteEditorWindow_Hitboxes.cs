using Retruxel.Tool.SpriteEditor.Models;
using Retruxel.Tool.SpriteEditor.Views;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private HitboxDefinition? _selectedHitbox;

    private void RefreshHitboxList()
    {
        HitboxesListBox.Items.Clear();

        if (_state.Frames.Count == 0) return;

        var frame = _state.Frames[_state.CurrentFrameIndex];

        foreach (var box in frame.Hitboxes)
        {
            var item = new ListBoxItem
            {
                Content = $"[{box.Type}] {box.Name} ({box.X},{box.Y} {box.Width}×{box.Height})",
                Tag = box,
                Foreground = (Brush)FindResource("BrushOnSurface"),
                Background = (Brush)FindResource("BrushSurfaceContainerLow"),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(4, 2, 4, 2)
            };
            HitboxesListBox.Items.Add(item);
        }
    }

    private void BtnAddHitbox_Click(object sender, RoutedEventArgs e)
    {
        if (_state.Frames.Count == 0) return;

        var dialog = new HitboxTypeDialog { Owner = this };
        if (dialog.ShowDialog() != true) return;

        var frame = _state.Frames[_state.CurrentFrameIndex];
        frame.Hitboxes.Add(new HitboxDefinition
        {
            Name = dialog.BoxName,
            Type = dialog.BoxType,
            X = 0,
            Y = 0,
            Width = 8,
            Height = 8
        });

        RefreshHitboxList();
        DrawHitboxes();
        OnSpriteChanged();
    }

    private void BtnDeleteHitbox_Click(object sender, RoutedEventArgs e)
    {
        if (HitboxesListBox.SelectedItem is not ListBoxItem item) return;
        if (item.Tag is not HitboxDefinition box) return;

        var frame = _state.Frames[_state.CurrentFrameIndex];
        frame.Hitboxes.Remove(box);
        RefreshHitboxList();
        DrawHitboxes();
        OnSpriteChanged();
    }

    private void DrawHitboxes()
    {
        // Remove existing hitbox rectangles
        var toRemove = CompositionCanvas.Children
            .OfType<Rectangle>()
            .Where(r => r.Tag is string s && s == "hitbox")
            .ToList();
        foreach (var r in toRemove)
            CompositionCanvas.Children.Remove(r);

        if (_state.Frames.Count == 0) return;

        var frame = _state.Frames[_state.CurrentFrameIndex];

        foreach (var box in frame.Hitboxes)
        {
            var color = box.Type switch
            {
                HitboxType.Hitbox => Colors.Red,
                HitboxType.Hurtbox => Colors.LimeGreen,
                HitboxType.Solidbox => Colors.DodgerBlue,
                _ => Colors.Yellow
            };

            var rect = new Rectangle
            {
                Width = box.Width * _canvasZoom,
                Height = box.Height * _canvasZoom,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B)),
                Tag = "hitbox",
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rect, box.X * _canvasZoom);
            Canvas.SetTop(rect, box.Y * _canvasZoom);
            CompositionCanvas.Children.Add(rect);
        }
    }
}
