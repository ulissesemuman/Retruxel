using Retruxel.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Retruxel.Core.Services;

/// <summary>
/// Validation logic: tile conflicts, module compatibility checks.
/// </summary>
public partial class CodeGenerator
{
    /// <summary>
    /// Validates tile conflicts between plane and text.display modules.
    /// SMS_autoSetUpTextRenderer() loads ASCII font into tiles 0-255.
    /// If plane startTile < 256, it will overwrite the font.
    /// </summary>
    private static void ValidateTileConflicts(
        Dictionary<string, List<IModule>> instancesByModule,
        IProgress<string>? progress)
    {
        // Check if both plane and text.display are present
        var hasPlane = instancesByModule.ContainsKey("plane");
        var hasTextDisplay = instancesByModule.ContainsKey("text.display");

        if (!hasPlane || !hasTextDisplay)
            return;

        // Check each plane instance for startTile < 256
        foreach (var planeModule in instancesByModule["plane"])
        {
            var json = planeModule.Serialize();
            try
            {
                var node = JsonNode.Parse(json) as JsonObject;
                if (node is null) continue;

                var startTile = node["startTile"]?.GetValue<int>() ?? 0;

                if (startTile < 256)
                {
                    progress?.Report($"WARN: Plane startTile={startTile} conflicts with text.display font (tiles 0-255).");
                    progress?.Report($"WARN: Set plane startTile >= 256 to avoid overwriting the ASCII font.");
                    progress?.Report($"WARN: Text will appear corrupted if plane overwrites font tiles.");
                }
            }
            catch
            {
                // Skip validation if JSON parsing fails
            }
        }
    }
}
