using Retruxel.Tool.SpriteEditor.Helpers;

namespace Retruxel.Tool.SpriteEditor.Models;

public class SpriteFrame
{
    public string Name { get; set; } = "Frame";
    public int Duration { get; set; } = 100; // milliseconds
    public List<SpriteTile> Tiles { get; set; } = new();

    public SpriteFrame Clone()
    {
        return new SpriteFrame
        {
            Name = Name,
            Duration = Duration,
            Tiles = Tiles.Select(t => t.Clone()).ToList()
        };
    }
}
