using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Tool.LiveLink.Services;

/// <summary>
/// Optimizes palettes using k-means clustering to group similar colors.
/// </summary>
public class PaletteOptimizer
{
    public class OptimizedPalette
    {
        public uint[][] Palettes { get; set; } = Array.Empty<uint[]>();
        public byte[] TilePaletteAssignments { get; set; } = Array.Empty<byte>();
        public int TotalColors { get; set; }
    }

    /// <summary>
    /// Optimizes palette for SMS/GG (2 palettes × 16 colors, first 4 filled).
    /// Uses hierarchical clustering: first separates very different colors,
    /// then subdivides each group if palette space remains.
    /// </summary>
    public static OptimizedPalette OptimizeForSms(byte[][] tiles, uint[] sourceColors, int maxIterations = 20)
    {
        const int paletteCount = 2;
        const int colorsPerPalette = 16;
        const int filledColors = 4;
        const int totalSlots = paletteCount * filledColors; // 8 slots

        var uniqueColors = ExtractUniqueColors(tiles, sourceColors);
        
        // Hierarchical clustering: start with fewer clusters, then refine
        var clusters = HierarchicalClustering(uniqueColors, totalSlots, maxIterations);

        var palettes = new uint[paletteCount][];
        for (int i = 0; i < paletteCount; i++)
        {
            palettes[i] = new uint[colorsPerPalette];
            for (int j = 0; j < filledColors && i * filledColors + j < clusters.Length; j++)
            {
                palettes[i][j] = clusters[i * filledColors + j];
            }
        }

        var assignments = AssignTilesToPalettes(tiles, sourceColors, palettes, filledColors);

        return new OptimizedPalette
        {
            Palettes = palettes,
            TilePaletteAssignments = assignments,
            TotalColors = Math.Min(clusters.Length, totalSlots)
        };
    }

    /// <summary>
    /// Optimizes palette for NES (4 palettes × 4 colors).
    /// Uses hierarchical clustering for better color distribution.
    /// </summary>
    public static OptimizedPalette OptimizeForNes(byte[][] tiles, uint[] sourceColors, int maxIterations = 20)
    {
        const int paletteCount = 4;
        const int colorsPerPalette = 4;
        const int totalSlots = paletteCount * colorsPerPalette; // 16 slots

        var uniqueColors = ExtractUniqueColors(tiles, sourceColors);
        
        // Hierarchical clustering
        var clusters = HierarchicalClustering(uniqueColors, totalSlots, maxIterations);

        var palettes = new uint[paletteCount][];
        for (int i = 0; i < paletteCount; i++)
        {
            palettes[i] = new uint[colorsPerPalette];
            for (int j = 0; j < colorsPerPalette && i * colorsPerPalette + j < clusters.Length; j++)
            {
                palettes[i][j] = clusters[i * colorsPerPalette + j];
            }
        }

        var assignments = AssignTilesToPalettes(tiles, sourceColors, palettes, colorsPerPalette);

        return new OptimizedPalette
        {
            Palettes = palettes,
            TilePaletteAssignments = assignments,
            TotalColors = Math.Min(clusters.Length, totalSlots)
        };
    }

    private static HashSet<uint> ExtractUniqueColors(byte[][] tiles, uint[] sourceColors)
    {
        var colors = new HashSet<uint>();
        foreach (var tile in tiles)
        {
            foreach (var idx in tile)
            {
                if (idx < sourceColors.Length)
                    colors.Add(sourceColors[idx]);
            }
        }
        return colors;
    }

    private static uint[] KMeansClustering(HashSet<uint> colors, int k, int maxIterations)
    {
        if (colors.Count <= k)
            return colors.ToArray();

        var colorList = colors.ToList();
        
        // Use K-means++ initialization for better centroid selection
        var centroids = KMeansPlusPlusInit(colorList, k);
        
        System.Diagnostics.Debug.WriteLine($"[PaletteOptimizer] K-means clustering: {colorList.Count} colors → {k} clusters");
        System.Diagnostics.Debug.WriteLine($"[PaletteOptimizer] Initial centroids: {string.Join(", ", centroids.Select(c => $"#{c:X6}"))}");

        for (int iter = 0; iter < maxIterations; iter++)
        {
            var clusters = new List<uint>[k];
            for (int i = 0; i < k; i++)
                clusters[i] = new List<uint>();

            // Assign colors to nearest centroid
            foreach (var color in colorList)
            {
                int nearest = 0;
                double minDist = ColorDistance(color, centroids[0]);
                
                for (int i = 1; i < k; i++)
                {
                    double dist = ColorDistance(color, centroids[i]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = i;
                    }
                }
                
                clusters[nearest].Add(color);
            }

            // Recalculate centroids
            bool changed = false;
            for (int i = 0; i < k; i++)
            {
                if (clusters[i].Count > 0)
                {
                    var newCentroid = CalculateCentroid(clusters[i]);
                    if (newCentroid != centroids[i])
                    {
                        centroids[i] = newCentroid;
                        changed = true;
                    }
                }
            }

            if (!changed)
            {
                System.Diagnostics.Debug.WriteLine($"[PaletteOptimizer] Converged after {iter + 1} iterations");
                break;
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[PaletteOptimizer] Final centroids: {string.Join(", ", centroids.Select(c => $"#{c:X6}"))}");

        return centroids;
    }
    
    /// <summary>
    /// K-means++ initialization: selects initial centroids that are far apart.
    /// This ensures diverse color selection (e.g., both green AND red).
    /// </summary>
    private static uint[] KMeansPlusPlusInit(List<uint> colors, int k)
    {
        var random = new Random();
        var centroids = new List<uint>();
        
        // 1. Choose first centroid randomly
        centroids.Add(colors[random.Next(colors.Count)]);
        
        // 2. Choose remaining centroids based on distance from existing ones
        for (int i = 1; i < k; i++)
        {
            var distances = new double[colors.Count];
            double totalDistance = 0;
            
            // Calculate distance from each color to nearest existing centroid
            for (int j = 0; j < colors.Count; j++)
            {
                double minDist = double.MaxValue;
                foreach (var centroid in centroids)
                {
                    double dist = ColorDistance(colors[j], centroid);
                    if (dist < minDist)
                        minDist = dist;
                }
                distances[j] = minDist * minDist; // Square for weighted probability
                totalDistance += distances[j];
            }
            
            // Choose next centroid with probability proportional to distance
            double threshold = random.NextDouble() * totalDistance;
            double sum = 0;
            
            for (int j = 0; j < colors.Count; j++)
            {
                sum += distances[j];
                if (sum >= threshold)
                {
                    centroids.Add(colors[j]);
                    break;
                }
            }
            
            // Fallback: if loop didn't add a centroid, add the farthest color
            if (centroids.Count == i)
            {
                int farthestIdx = 0;
                double maxDist = 0;
                for (int j = 0; j < colors.Count; j++)
                {
                    if (distances[j] > maxDist)
                    {
                        maxDist = distances[j];
                        farthestIdx = j;
                    }
                }
                centroids.Add(colors[farthestIdx]);
            }
        }
        
        return centroids.ToArray();
    }
    
    // Diversity parameter from Dithertron algorithm
    // Range: 0.75 (low diversity) to 1.25 (high diversity)
    // Default: 1.25 (maximum diversity)
    private const double DEFAULT_DIVERSITY = 1.25;
    
    /// <summary>
    /// Public method for palette optimization with custom diversity.
    /// Used by PaletteOptimizationWindow for real-time preview.
    /// </summary>
    public static List<(byte R, byte G, byte B)> OptimizePalette(List<(byte R, byte G, byte B)> pixels, int targetColorCount, double diversity = DEFAULT_DIVERSITY)
    {
        var colorSet = new HashSet<uint>();
        foreach (var (r, g, b) in pixels)
        {
            uint color = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
            colorSet.Add(color);
        }
        
        System.Diagnostics.Debug.WriteLine($"[OptimizePalette] Input: {pixels.Count} pixels, {colorSet.Count} unique colors");
        System.Diagnostics.Debug.WriteLine($"[OptimizePalette] Target: {targetColorCount} colors, diversity: {diversity}");
        
        var optimized = HierarchicalClustering(colorSet, targetColorCount, 20, diversity);
        
        System.Diagnostics.Debug.WriteLine($"[OptimizePalette] Output: {optimized.Length} colors");
        
        return optimized.Select(c => (
            R: (byte)((c >> 16) & 0xFF),
            G: (byte)((c >> 8) & 0xFF),
            B: (byte)(c & 0xFF)
        )).ToList();
    }
    
    /// <summary>
    /// Dithertron-style diversity-based palette optimization.
    /// Uses bias and decay factors derived from diversity parameter.
    /// </summary>
    private static uint[] HierarchicalClustering(HashSet<uint> colors, int targetSlots, int maxIterations, double diversity = DEFAULT_DIVERSITY)
    {
        if (colors.Count <= targetSlots)
            return colors.ToArray();
        
        var colorList = colors.ToList();
        System.Diagnostics.Debug.WriteLine($"[HierarchicalClustering] {colorList.Count} colors → {targetSlots} slots (diversity={diversity})");
        
        // Use diversity-weighted K-means++ initialization
        var centroids = DiversityWeightedInit(colorList, targetSlots, diversity);
        System.Diagnostics.Debug.WriteLine($"[HierarchicalClustering] Initial centroids: {string.Join(", ", centroids.Select(c => $"#{c:X6}"))}");
        
        // Run K-means to convergence
        for (int iter = 0; iter < maxIterations; iter++)
        {
            var clusters = new List<uint>[targetSlots];
            for (int i = 0; i < targetSlots; i++)
                clusters[i] = new List<uint>();
            
            foreach (var color in colorList)
            {
                int nearest = FindNearestCentroid(color, centroids);
                clusters[nearest].Add(color);
            }
            
            bool changed = false;
            for (int i = 0; i < targetSlots; i++)
            {
                if (clusters[i].Count > 0)
                {
                    var newCentroid = CalculateCentroid(clusters[i]);
                    if (newCentroid != centroids[i])
                    {
                        centroids[i] = newCentroid;
                        changed = true;
                    }
                }
            }
            
            if (!changed)
            {
                System.Diagnostics.Debug.WriteLine($"[HierarchicalClustering] Converged at iteration {iter}");
                break;
            }
        }
        
        var finalCentroids = new List<uint>();
        for (int i = 0; i < targetSlots; i++)
        {
            var clusterColors = colorList.Where(c => FindNearestCentroid(c, centroids) == i).ToList();
            if (clusterColors.Count > 0)
            {
                finalCentroids.Add(centroids[i]);
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[HierarchicalClustering] Final: {finalCentroids.Count}/{targetSlots} colors");
        System.Diagnostics.Debug.WriteLine($"[HierarchicalClustering] Colors: {string.Join(", ", finalCentroids.Select(c => $"#{c:X6}"))}");
        
        return finalCentroids.ToArray();
    }
    
    /// <summary>
    /// Dithertron-style diversity initialization using bias and decay factors.
    /// bias = diversity * 0.5 + 0.5 (weights centroid selection)
    /// decay = diversity * 0.25 + 0.65 (controls error propagation)
    /// </summary>
    private static uint[] DiversityWeightedInit(List<uint> colors, int k, double diversity)
    {
        // Use deterministic seed based on diversity to ensure same results for same diversity value
        int seed = (int)(diversity * 10000);
        var random = new Random(seed);
        var centroids = new List<uint>();
        
        // Dithertron bias and decay calculations
        double bias = diversity * 0.5 + 0.5;      // 1.125 at diversity=1.25
        double decay = diversity * 0.25 + 0.65;   // 0.9625 at diversity=1.25
        
        System.Diagnostics.Debug.WriteLine($"[DiversityWeightedInit] bias={bias:F3}, decay={decay:F3}");
        
        // Count color frequencies
        var colorCounts = new Dictionary<uint, int>();
        foreach (var color in colors)
        {
            if (!colorCounts.ContainsKey(color))
                colorCounts[color] = 0;
            colorCounts[color]++;
        }
        
        var uniqueColors = colorCounts.Keys.ToList();
        System.Diagnostics.Debug.WriteLine($"[DiversityWeightedInit] {uniqueColors.Count} unique colors from {colors.Count} pixels");
        
        // 1. Choose first centroid: most frequent color (weighted by bias)
        var firstColor = uniqueColors.OrderByDescending(c => colorCounts[c] * bias).First();
        centroids.Add(firstColor);
        System.Diagnostics.Debug.WriteLine($"[DiversityWeightedInit] Centroid 0: #{firstColor:X6} (count={colorCounts[firstColor]})");
        
        // 2. Choose remaining centroids with diversity + frequency weighting
        for (int i = 1; i < k; i++)
        {
            var weights = new double[uniqueColors.Count];
            double totalWeight = 0;
            
            for (int j = 0; j < uniqueColors.Count; j++)
            {
                var color = uniqueColors[j];
                
                // Skip if already selected
                if (centroids.Contains(color))
                {
                    weights[j] = 0;
                    continue;
                }
                
                // Calculate minimum distance to existing centroids
                double minDist = double.MaxValue;
                foreach (var centroid in centroids)
                {
                    double dist = ColorDistance(color, centroid);
                    if (dist < minDist)
                        minDist = dist;
                }
                
                // Frequency weight (biased)
                double freqWeight = colorCounts[color] * bias;
                
                // Diversity weight (distance with decay)
                double divWeight = minDist * minDist * decay;
                
                // Combined weight: diversity dominates but frequency matters
                weights[j] = divWeight + freqWeight * 0.1; // 10% frequency influence
                totalWeight += weights[j];
            }
            
            // Weighted random selection
            double threshold = random.NextDouble() * totalWeight;
            double sum = 0;
            
            for (int j = 0; j < uniqueColors.Count; j++)
            {
                sum += weights[j];
                if (sum >= threshold)
                {
                    centroids.Add(uniqueColors[j]);
                    System.Diagnostics.Debug.WriteLine($"[DiversityWeightedInit] Centroid {i}: #{uniqueColors[j]:X6} (count={colorCounts[uniqueColors[j]]}, weight={weights[j]:F2})");
                    break;
                }
            }
            
            // Fallback: if no color was added, pick the one with highest weight
            if (centroids.Count == i)
            {
                int maxIdx = 0;
                double maxWeight = 0;
                for (int j = 0; j < uniqueColors.Count; j++)
                {
                    if (weights[j] > maxWeight)
                    {
                        maxWeight = weights[j];
                        maxIdx = j;
                    }
                }
                centroids.Add(uniqueColors[maxIdx]);
                System.Diagnostics.Debug.WriteLine($"[DiversityWeightedInit] Centroid {i} (fallback): #{uniqueColors[maxIdx]:X6}");
            }
        }
        
        return centroids.ToArray();
    }
    
    private static int FindNearestCentroid(uint color, uint[] centroids)
    {
        int nearest = 0;
        double minDist = ColorDistance(color, centroids[0]);
        
        for (int i = 1; i < centroids.Length; i++)
        {
            double dist = ColorDistance(color, centroids[i]);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }
        
        return nearest;
    }

    private static double ColorDistance(uint c1, uint c2)
    {
        int r1 = (int)((c1 >> 16) & 0xFF);
        int g1 = (int)((c1 >> 8) & 0xFF);
        int b1 = (int)(c1 & 0xFF);
        
        int r2 = (int)((c2 >> 16) & 0xFF);
        int g2 = (int)((c2 >> 8) & 0xFF);
        int b2 = (int)(c2 & 0xFF);
        
        int dr = r1 - r2;
        int dg = g1 - g2;
        int db = b1 - b2;
        
        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    private static uint CalculateCentroid(List<uint> colors)
    {
        long r = 0, g = 0, b = 0;
        
        foreach (var color in colors)
        {
            r += (color >> 16) & 0xFF;
            g += (color >> 8) & 0xFF;
            b += color & 0xFF;
        }
        
        int count = colors.Count;
        byte avgR = (byte)(r / count);
        byte avgG = (byte)(g / count);
        byte avgB = (byte)(b / count);
        
        return 0xFF000000u | ((uint)avgR << 16) | ((uint)avgG << 8) | avgB;
    }

    private static byte[] AssignTilesToPalettes(byte[][] tiles, uint[] sourceColors, uint[][] palettes, int colorsPerPalette)
    {
        var assignments = new byte[tiles.Length];
        
        for (int tileIdx = 0; tileIdx < tiles.Length; tileIdx++)
        {
            var tile = tiles[tileIdx];
            var tileColors = tile.Where(idx => idx < sourceColors.Length)
                                 .Select(idx => sourceColors[idx])
                                 .Distinct()
                                 .ToArray();

            int bestPalette = 0;
            double minError = double.MaxValue;

            for (int palIdx = 0; palIdx < palettes.Length; palIdx++)
            {
                double error = CalculatePaletteError(tileColors, palettes[palIdx], colorsPerPalette);
                if (error < minError)
                {
                    minError = error;
                    bestPalette = palIdx;
                }
            }

            assignments[tileIdx] = (byte)bestPalette;
        }

        return assignments;
    }

    private static double CalculatePaletteError(uint[] tileColors, uint[] palette, int maxColors)
    {
        double totalError = 0;
        
        foreach (var color in tileColors)
        {
            double minDist = double.MaxValue;
            for (int i = 0; i < maxColors && i < palette.Length; i++)
            {
                double dist = ColorDistance(color, palette[i]);
                if (dist < minDist)
                    minDist = dist;
            }
            totalError += minDist;
        }
        
        return totalError;
    }
}
