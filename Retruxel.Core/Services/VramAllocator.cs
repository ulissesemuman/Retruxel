using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Core.Services;

/// <summary>
/// Calculates VRAM usage and assigns tile offsets for all planes in a scene.
///
/// Two modes:
///   Analyze  — dry run used by the editor in real time (no addresses assigned).
///   Allocate — build-time pass that assigns sequential tileOffset per asset.
/// </summary>
public static class VramAllocator
{
    /// <summary>
    /// Analyzes VRAM usage for a scene without assigning addresses.
    /// Safe to call on every tree refresh — reads only, never mutates.
    /// </summary>
    public static VramUsageReport Analyze(
        SceneData scene,
        ITarget target,
        IReadOnlyList<AssetEntry> assets,
        int fontTileCount = 0)
    {
        var specs      = target.Specs;
        var available  = specs.VramBytesForTiles;
        var planeUsages = new List<PlaneUsage>();
        int totalUsed  = 0;

        // Default bytes-per-tile from the first plane spec (fallback: 32 bytes = SMS 4bpp)
        int defaultBytesPerTile = specs.Planes.FirstOrDefault()?.BytesPerTile ?? 32;

        // Track counted assets to avoid double-counting shared assets
        var counted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plane in scene.Planes)
        {
            var planeSpecs = specs.Planes.FirstOrDefault(p => p.Id == plane.PlaneId);
            if (planeSpecs is null) continue;

            int bytesPerTile = planeSpecs.BytesPerTile;
            var layerUsages  = new List<LayerUsage>();
            int planeBytes   = 0;

            foreach (var layer in plane.Layers)
            {
                if (string.IsNullOrEmpty(layer.AssetId)) continue;

                int tileCount = GetTileCount(layer.AssetId, assets);
                int bytes     = counted.Add(layer.AssetId) ? tileCount * bytesPerTile : 0;

                layerUsages.Add(new LayerUsage
                {
                    LayerName = layer.LayerName,
                    AssetId   = layer.AssetId,
                    TileCount = tileCount,
                    BytesUsed = bytes
                });

                planeBytes += bytes;
            }

            planeUsages.Add(new PlaneUsage
            {
                PlaneId      = plane.PlaneId,
                PlaneLabel   = planeSpecs.Label,
                BytesPerTile = bytesPerTile,
                BytesUsed    = planeBytes,
                Layers       = layerUsages
            });

            totalUsed += planeBytes;
        }

        // Entity sprite assets
        foreach (var entity in scene.Entities)
        {
            if (string.IsNullOrEmpty(entity.SpriteAssetId)) continue;
            if (!counted.Add(entity.SpriteAssetId)) continue;

            int tileCount = GetTileCount(entity.SpriteAssetId, assets);
            totalUsed += tileCount * defaultBytesPerTile;
        }

        // Font tiles (text system)
        totalUsed += fontTileCount * defaultBytesPerTile;

        return new VramUsageReport
        {
            TotalBytesAvailable = available,
            TotalBytesUsed      = totalUsed,
            Planes              = planeUsages
        };
    }

    /// <summary>
    /// Assigns sequential tile offsets to every asset referenced by the scene's planes.
    /// Called once per build — after this, CodeGen uses the returned offsets.
    /// </summary>
    public static VramAllocation Allocate(
        SceneData scene,
        ITarget target,
        IReadOnlyList<AssetEntry> assets)
    {
        var specs       = target.Specs;
        var offsets     = new Dictionary<string, int>();
        int nextOffset  = 0;

        // Deterministic order: sort planes by PlaneId so builds are reproducible.
        var sortedPlanes = scene.Planes
            .OrderBy(p => p.PlaneId)
            .ToList();

        foreach (var plane in sortedPlanes)
        {
            var planeSpecs = specs.Planes.FirstOrDefault(p => p.Id == plane.PlaneId);
            if (planeSpecs is null) continue;

            foreach (var layer in plane.Layers)
            {
                if (string.IsNullOrEmpty(layer.AssetId)) continue;

                // Each unique assetId gets one offset — shared across layers that
                // reference the same asset (deduplication at asset level).
                if (offsets.ContainsKey(layer.AssetId)) continue;

                var asset      = assets.FirstOrDefault(a => a.Id == layer.AssetId);
                int tileCount  = asset?.GenerationParams?.TileCount ?? 0;

                offsets[layer.AssetId] = nextOffset;
                nextOffset += tileCount;
            }
        }

        // Entity sprite assets — allocated after plane assets
        int defaultBytesPerTile = specs.Planes.FirstOrDefault()?.BytesPerTile ?? 32;
        foreach (var entity in scene.Entities)
        {
            if (string.IsNullOrEmpty(entity.SpriteAssetId)) continue;
            if (offsets.ContainsKey(entity.SpriteAssetId)) continue;

            var asset     = assets.FirstOrDefault(a => a.Id == entity.SpriteAssetId);
            int tileCount = asset?.GenerationParams?.TileCount ?? 0;

            offsets[entity.SpriteAssetId] = nextOffset;
            nextOffset += tileCount;
        }

        int totalBytesUsed = nextOffset * defaultBytesPerTile;

        return new VramAllocation
        {
            TileOffsets    = offsets,
            TotalTilesUsed = nextOffset,
            FitsInVram     = totalBytesUsed <= specs.VramBytesForTiles
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Looks up the tile count for an asset referenced by a layer.
    /// Falls back to 0 if the asset is not yet imported or has no generation params.
    /// </summary>
    private static int GetTileCount(string assetId, IReadOnlyList<AssetEntry> assets)
    {
        if (string.IsNullOrEmpty(assetId)) return 0;
        return assets.FirstOrDefault(a => a.Id == assetId)?.GenerationParams?.TileCount ?? 0;
    }

    private static int CalculateTotalBytes(
        SceneData scene,
        ITarget target,
        IReadOnlyList<AssetEntry> assets)
    {
        var specs = target.Specs;
        int total = 0;

        // Track which assets have already been counted (shared across layers).
        var counted = new HashSet<string>();

        foreach (var plane in scene.Planes)
        {
            var planeSpecs = specs.Planes.FirstOrDefault(p => p.Id == plane.PlaneId);
            if (planeSpecs is null) continue;

            int bytesPerTile = planeSpecs.BytesPerTile;

            foreach (var layer in plane.Layers)
            {
                if (string.IsNullOrEmpty(layer.AssetId)) continue;
                if (!counted.Add(layer.AssetId)) continue;

                var asset     = assets.FirstOrDefault(a => a.Id == layer.AssetId);
                int tileCount = asset?.GenerationParams?.TileCount ?? 0;
                total += tileCount * bytesPerTile;
            }
        }

        return total;
    }
}

// ── Report models ─────────────────────────────────────────────────────────────

/// <summary>
/// Result of a dry-run VRAM analysis. Used by the editor to show usage indicators.
/// </summary>
public class VramUsageReport
{
    public int TotalBytesAvailable { get; init; }
    public int TotalBytesUsed      { get; init; }
    public int TotalBytesRemaining => TotalBytesAvailable - TotalBytesUsed;
    public float UsagePercent      => TotalBytesAvailable > 0
        ? (float)TotalBytesUsed / TotalBytesAvailable
        : 0f;
    public IReadOnlyList<PlaneUsage> Planes { get; init; } = [];
}

/// <summary>Per-plane breakdown inside a VramUsageReport.</summary>
public class PlaneUsage
{
    public string PlaneId      { get; init; } = string.Empty;
    public string PlaneLabel   { get; init; } = string.Empty;
    public int    BytesPerTile { get; init; }
    public int    BytesUsed    { get; init; }
    public IReadOnlyList<LayerUsage> Layers { get; init; } = [];
}

/// <summary>Per-layer breakdown inside a PlaneUsage.</summary>
public class LayerUsage
{
    public string LayerName { get; init; } = string.Empty;
    public string AssetId   { get; init; } = string.Empty;
    public int    TileCount { get; init; }
    public int    BytesUsed { get; init; }
}

/// <summary>
/// Result of a build-time VRAM allocation.
/// Maps each assetId to its starting tile offset in VRAM.
/// </summary>
public class VramAllocation
{
    public IReadOnlyDictionary<string, int> TileOffsets    { get; init; }
        = new Dictionary<string, int>();
    public int  TotalTilesUsed { get; init; }
    public bool FitsInVram     { get; init; }
}
