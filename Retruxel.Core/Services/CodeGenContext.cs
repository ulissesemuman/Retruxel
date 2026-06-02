using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Collections.Generic;

namespace Retruxel.Core.Services;

/// <summary>
/// Pre-computed allocation context passed to all CodeGens before generation begins.
///
/// Assembled by CodeGenerator.GenerateAsync() after running:
///   1. VramAllocator.Allocate()  → Vram
///   2. SatAllocator.Allocate()   → SatIndices
///
/// CodeGens read from this context instead of computing offsets themselves,
/// ensuring all modules agree on the same VRAM layout and SAT assignments.
/// </summary>
public class CodeGenContext
{
    /// <summary>
    /// VRAM tile offsets per asset ID.
    /// Key = AssetEntry.Id, Value = first tile slot in VRAM for that asset.
    /// </summary>
    public VramAllocation Vram { get; init; } = new();

    /// <summary>
    /// SAT slot index per entity ID.
    /// Key = EntityData.EntityId, Value = first hardware sprite slot for that entity.
    /// </summary>
    public IReadOnlyDictionary<string, int> SatIndices { get; init; }
        = new Dictionary<string, int>();

    /// <summary>
    /// Collision maps per layer ID (optional — built only when collision is needed).
    /// Key = PlaneLayerData.LayerId, Value = packed collision byte array.
    /// </summary>
    public IReadOnlyDictionary<string, byte[]> CollisionMaps { get; init; }
        = new Dictionary<string, byte[]>();

    public ITarget         Target  { get; init; } = null!;
    public SceneData       Scene   { get; init; } = null!;
    public RetruxelProject Project { get; init; } = null!;
}
