using Retruxel.Tool.SpriteEditor.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Tool.SpriteEditor.Models;

public class SpriteFrame
{
    public string Name { get; set; } = "Frame";
    public int Duration { get; set; } = 100; // milliseconds
    public List<SpriteTile> Tiles { get; set; } = new();
    public List<HitboxDefinition> Hitboxes { get; set; } = new();

    public SpriteFrame Clone()
    {
        return new SpriteFrame
        {
            Name = Name,
            Duration = Duration,
            Tiles = Tiles.Select(t => t.Clone()).ToList(),
            Hitboxes = Hitboxes.Select(h => new HitboxDefinition
            {
                Name = h.Name,
                Type = h.Type,
                X = h.X,
                Y = h.Y,
                Width = h.Width,
                Height = h.Height
            }).ToList()
        };
    }
}
