namespace Retruxel.Tool.SpriteEditor.Models;

/// <summary>
/// Defines a collision box (hitbox, hurtbox, or solidbox) for a sprite frame.
/// Used for collision detection in the game engine.
/// </summary>
public class HitboxDefinition
{
    public string Name { get; set; } = "Box";
    public HitboxType Type { get; set; } = HitboxType.Hurtbox;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 8;
    public int Height { get; set; } = 8;
}

/// <summary>
/// Type of collision box.
/// Hitbox: deals damage to others
/// Hurtbox: receives damage from others
/// Solidbox: blocks movement
/// </summary>
public enum HitboxType
{
    Hitbox,
    Hurtbox,
    Solidbox
}
