namespace Retruxel.Tool.SpriteEditor.Models;

public class SpriteState
{
    public List<SpriteFrame> Frames { get; set; } = new();
    public int CurrentFrameIndex { get; set; } = 0;
    public bool IsAnimating { get; set; } = false;
    public bool LoopAnimation { get; set; } = true;
    public int AnimationSpeed { get; set; } = 100; // percentage (100 = normal speed)
}
