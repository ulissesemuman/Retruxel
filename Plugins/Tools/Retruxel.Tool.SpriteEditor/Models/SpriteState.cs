using System.Collections.Generic;

namespace Retruxel.Tool.SpriteEditor.Models;

public class SpriteState
{
    public List<SpriteFrame> Frames { get; set; } = new();
    public int CurrentFrameIndex { get; set; } = 0;
    public bool IsAnimating { get; set; } = false;
    public bool LoopAnimation { get; set; } = true;
    public int AnimationSpeed { get; set; } = 100;
    public int StartTile { get; set; } = 0;
    public bool OnionSkinPrevious { get; set; } = true;
    public bool OnionSkinNext { get; set; } = true;
    public double OnionSkinOpacity { get; set; } = 0.35;
}
