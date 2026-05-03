using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private DispatcherTimer? _animationTimer;
    private int _animationFrameIndex = 0;

    private void InitializeAnimation()
    {
        _animationTimer = new DispatcherTimer();
        _animationTimer.Tick += AnimationTimer_Tick;
        UpdateAnimationSpeed();
    }

    private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_state.IsAnimating)
        {
            StopAnimation();
        }
        else
        {
            StartAnimation();
        }
    }

    private void StartAnimation()
    {
        if (_state.Frames.Count <= 1 || _animationTimer == null)
            return;

        _state.IsAnimating = true;
        _animationFrameIndex = 0;
        _animationTimer.Start();
        BtnPlayPause.Content = "⏸";
        RenderPreview();
    }

    private void StopAnimation()
    {
        if (_animationTimer == null)
            return;

        _state.IsAnimating = false;
        _animationTimer.Stop();
        BtnPlayPause.Content = "▶";
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        _animationFrameIndex++;

        if (_animationFrameIndex >= _state.Frames.Count)
        {
            if (ChkLoop.IsChecked == true)
            {
                _animationFrameIndex = 0;
            }
            else
            {
                StopAnimation();
                return;
            }
        }

        RenderPreview();
    }

    private void UpdateAnimationSpeed()
    {
        if (_animationTimer == null || _state.Frames.Count == 0)
            return;

        var currentFrame = _state.Frames[_state.CurrentFrameIndex];
        double interval = currentFrame.Duration * (100.0 / _state.AnimationSpeed);
        _animationTimer.Interval = TimeSpan.FromMilliseconds(interval);
    }

    private void RenderPreview()
    {
        PreviewCanvas.Children.Clear();

        if (_state.Frames.Count == 0 || _tilesetImage == null)
            return;

        int frameIndex = _state.IsAnimating ? _animationFrameIndex : _state.CurrentFrameIndex;
        
        if (frameIndex >= _state.Frames.Count)
            return;

        var frame = _state.Frames[frameIndex];
        int scale = 2;

        foreach (var tile in frame.Tiles)
        {
            var tileImage = ExtractTile(tile.TileIndex);
            
            var image = new Image
            {
                Source = tileImage,
                Width = 8 * scale,
                Height = 8 * scale,
                Stretch = Stretch.None
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

            Canvas.SetLeft(image, tile.OffsetX * scale);
            Canvas.SetTop(image, tile.OffsetY * scale);

            PreviewCanvas.Children.Add(image);
        }
    }
}
