using Retruxel.Tool.SpriteEditor.Helpers;
using Retruxel.Tool.SpriteEditor.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
 => DragMove();

    private void BtnClose_Click(object sender, RoutedEventArgs e)
 => Close();

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
 => Close();
    private void InitializeUI()
    {
        InitializeAnimation();
        InitializeFrameTags();
        RefreshFramesList();
        UpdateFrameDurationField();
        RenderCanvas();
        RenderPreview();
    }

    public void LoadModuleData(Dictionary<string, object> moduleData)
    {
        // Load tileset asset if specified
        if (moduleData.TryGetValue("tilesetAssetId", out var assetIdObj) && assetIdObj is string assetId)
        {
            _tilesetAssetId = assetId;
            // Asset will be loaded by InitializeAssetSelector
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
                        Tag = frameDict.TryGetValue("tag", out var tagObj) && tagObj is string tag ? tag : string.Empty,
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

                    if (frameDict.TryGetValue("hitboxes", out var hitboxesObj) && hitboxesObj is List<object> hitboxesList)
                    {
                        foreach (var hitboxObj in hitboxesList)
                        {
                            if (hitboxObj is Dictionary<string, object> hitboxDict)
                            {
                                var hitbox = new Models.HitboxDefinition
                                {
                                    Name = hitboxDict.TryGetValue("name", out var hNameObj) && hNameObj is string hName ? hName : "Hitbox",
                                    Type = hitboxDict.TryGetValue("type", out var hTypeObj) && hTypeObj is string hType ? Enum.Parse<Models.HitboxType>(hType) : Models.HitboxType.Hitbox,
                                    X = hitboxDict.TryGetValue("x", out var hXObj) && hXObj is int hX ? hX : 0,
                                    Y = hitboxDict.TryGetValue("y", out var hYObj) && hYObj is int hY ? hY : 0,
                                    Width = hitboxDict.TryGetValue("width", out var hWObj) && hWObj is int hW ? hW : 8,
                                    Height = hitboxDict.TryGetValue("height", out var hHObj) && hHObj is int hH ? hH : 8
                                };
                                frame.Hitboxes.Add(hitbox);
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

        if (moduleData.TryGetValue("onionSkinPrevious", out var onionPrevObj) && onionPrevObj is bool onionPrev)
        {
            _state.OnionSkinPrevious = onionPrev;
            ChkOnionPrev.IsChecked = onionPrev;
        }

        if (moduleData.TryGetValue("onionSkinNext", out var onionNextObj) && onionNextObj is bool onionNext)
        {
            _state.OnionSkinNext = onionNext;
            ChkOnionNext.IsChecked = onionNext;
        }

        if (moduleData.TryGetValue("onionSkinOpacity", out var onionOpacityObj))
        {
            if (onionOpacityObj is double onionOpacity)
                _state.OnionSkinOpacity = Math.Clamp(onionOpacity, 0.1, 0.7);
            else if (onionOpacityObj is int onionOpacityPercent)
                _state.OnionSkinOpacity = Math.Clamp(onionOpacityPercent / 100.0, 0.1, 0.7);

            SldOnionOpacity.Value = _state.OnionSkinOpacity * 100.0;
        }

        if (moduleData.TryGetValue("paletteSlot", out var paletteSlotObj) && paletteSlotObj is int paletteSlot)
        {
            if (CmbPaletteSlot.Items.Count > paletteSlot)
                CmbPaletteSlot.SelectedIndex = paletteSlot;
        }

        RefreshFramesList();
        UpdateFrameDurationField();
        UpdateFrameTagField();
        RefreshHitboxList();
        RenderCanvas();
        RenderPreview();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        ModuleData = SaveModuleData();
        DialogResult = true;
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

            var hitboxesList = new List<object>();

            foreach (var hitbox in frame.Hitboxes)
            {
                hitboxesList.Add(new Dictionary<string, object>
                {
                    ["name"] = hitbox.Name,
                    ["type"] = hitbox.Type.ToString(),
                    ["x"] = hitbox.X,
                    ["y"] = hitbox.Y,
                    ["width"] = hitbox.Width,
                    ["height"] = hitbox.Height
                });
            }

            framesList.Add(new Dictionary<string, object>
            {
                ["name"] = frame.Name,
                ["tag"] = frame.Tag,
                ["duration"] = frame.Duration,
                ["tiles"] = tilesList,
                ["hitboxes"] = hitboxesList
            });
        }

        var result = new Dictionary<string, object>
        {
            ["frames"] = framesList,
            ["currentFrameIndex"] = _state.CurrentFrameIndex,
            ["loopAnimation"] = ChkLoop.IsChecked == true,
            ["animationSpeed"] = _state.AnimationSpeed,
            ["onionSkinPrevious"] = ChkOnionPrev.IsChecked == true,
            ["onionSkinNext"] = ChkOnionNext.IsChecked == true,
            ["onionSkinOpacity"] = _state.OnionSkinOpacity
        };

        if (!string.IsNullOrEmpty(_tilesetAssetId))
            result["tilesetAssetId"] = _tilesetAssetId;

        if (CmbPaletteSlot.SelectedIndex >= 0)
            result["paletteSlot"] = CmbPaletteSlot.SelectedIndex;

        return result;
    }
}
