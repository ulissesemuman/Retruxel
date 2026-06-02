using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Core.Services;

/// <summary>
/// Allocates Sprite Attribute Table (SAT) slots to entities before code generation.
///
/// Each entity occupies (WidthTiles × HeightTiles) hardware sprite slots.
/// Slots are assigned sequentially in scene.Entities order.
/// Throws BuildException if total exceeds target.Specs.MaxSpritesOnScreen.
/// </summary>
public static class SatAllocator
{
    public static IReadOnlyDictionary<string, int> Allocate(SceneData scene, ITarget target, RetruxelProject? project = null)
    {
        var result   = new Dictionary<string, int>();
        int nextSlot = 0;

        foreach (var entity in scene.Entities)
        {
            // Resolve dimensions from Prefab if available, else fall back to legacy nullable fields
            var prefab = project is not null
                ? project.Prefabs.FirstOrDefault(p => p.PrefabId == entity.PrefabId)
                : null;

            int w = prefab?.WidthTiles  ?? entity.WidthTiles  ?? 2;
            int h = prefab?.HeightTiles ?? entity.HeightTiles ?? 2;

            result[entity.EntityId] = nextSlot;
            nextSlot += w * h;
        }

        int max = target.Specs.MaxSpritesOnScreen;
        if (nextSlot > max)
            throw new BuildException(
                $"Scene '{scene.SceneName}' uses {nextSlot} sprite slot(s) but target supports only {max}.");

        return result;
    }
}
