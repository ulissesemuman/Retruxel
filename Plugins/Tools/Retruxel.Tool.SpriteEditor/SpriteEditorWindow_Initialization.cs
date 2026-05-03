using System.Windows;
using Retruxel.Tool.SpriteEditor.Helpers;
using Retruxel.Tool.SpriteEditor.Models;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private void InitializeUI()
    {
        InitializeAnimation();
        RefreshFramesList();
        RenderCanvas();
        RenderPreview();
    }

    public void LoadModuleData(Dictionary<string, object> moduleData)
    {
        if (moduleData.TryGetValue("imagePath", out var imagePathObj) && imagePathObj is string imagePath)
        {
            LoadTileset(imagePath);
        }

        if (moduleData.TryGetValue("frames", out var framesObj) && framesObj is List<object> framesList)
        {
            _state.Frames.Clear();

            foreach (var frameObj in framesList)
            {
                if (frameObj is Dictionary<string, object> frameDict)
                {
                    var frame = new SpriteFrame
                    {
                        Name = frameDict.TryGetValue("name", out var nameObj) && nameObj is string name ? name : "Frame",
                        Duration = frameDict.TryGetValue("duration", out var durationObj) && durationObj is int duration ? duration : 100
                    };

                    if (frameDict.TryGetValue("tiles", out var tilesObj) && tilesObj is List<object> tilesList)
                    {
                        foreach (var tileObj in tilesList)
                        {
                            if (tileObj is Dictionary<string, object> tileDict)
                            {
                                var tile = new SpriteTile
                                {
                                    TileIndex = tileDict.TryGetValue("tileIndex", out var indexObj) && indexObj is int index ? index : 0,
                                    OffsetX = tileDict.TryGetValue("offsetX", out var xObj) && xObj is int x ? x : 0,
                                    OffsetY = tileDict.TryGetValue("offsetY", out var yObj) && yObj is int y ? y : 0
                                };
                                frame.Tiles.Add(tile);
                            }
                        }
                    }

                    _state.Frames.Add(frame);
                }
            }

            if (_state.Frames.Count == 0)
            {
                _state.Frames.Add(new SpriteFrame { Name = "Frame 1" });
            }
        }

        if (moduleData.TryGetValue("currentFrameIndex", out var currentIndexObj) && currentIndexObj is int currentIndex)
        {
            _state.CurrentFrameIndex = Math.Clamp(currentIndex, 0, _state.Frames.Count - 1);
        }

        if (moduleData.TryGetValue("loopAnimation", out var loopObj) && loopObj is bool loop)
        {
            _state.LoopAnimation = loop;
            ChkLoop.IsChecked = loop;
        }

        if (moduleData.TryGetValue("animationSpeed", out var speedObj) && speedObj is int speed)
        {
            _state.AnimationSpeed = speed;
        }

        RefreshFramesList();
        RenderCanvas();
        RenderPreview();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        ModuleData = SaveModuleData();
        _ = _saveProjectCallback?.Invoke();
        Close();
    }

    public Dictionary<string, object> SaveModuleData()
    {
        var framesList = new List<object>();

        foreach (var frame in _state.Frames)
        {
            var tilesList = new List<object>();

            foreach (var tile in frame.Tiles)
            {
                tilesList.Add(new Dictionary<string, object>
                {
                    ["tileIndex"] = tile.TileIndex,
                    ["offsetX"] = tile.OffsetX,
                    ["offsetY"] = tile.OffsetY
                });
            }

            framesList.Add(new Dictionary<string, object>
            {
                ["name"] = frame.Name,
                ["duration"] = frame.Duration,
                ["tiles"] = tilesList
            });
        }

        return new Dictionary<string, object>
        {
            ["frames"] = framesList,
            ["currentFrameIndex"] = _state.CurrentFrameIndex,
            ["loopAnimation"] = ChkLoop.IsChecked == true,
            ["animationSpeed"] = _state.AnimationSpeed
        };
    }
}
