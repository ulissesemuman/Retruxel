using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Core.Connectors;

/// <summary>
/// Connector that passes palette data back to TilemapEditor (or other calling tool).
/// Does NOT create a module — just returns temporary data for the caller to use.
/// </summary>
public class PaletteToTilemapConnector : IToolConnector
{
    public string ConnectorId => "palette_to_tilemap";

    public void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context)
    {
        // Validate output
        if (!toolOutput.ContainsKey("name") || !toolOutput.ContainsKey("colors"))
        {
            context.AddError("Palette output missing required fields: name, colors");
            return;
        }

        // Pass data back to caller via ChainedResults
        context.ChainResult(new Dictionary<string, object>
        {
            ["paletteName"] = toolOutput["name"],
            ["paletteColors"] = toolOutput["colors"]
        });
    }
}
