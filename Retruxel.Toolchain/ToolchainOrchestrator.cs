using Retruxel.Toolchain.Builders;

namespace Retruxel.Toolchain;

/// <summary>
/// Central orchestrator that routes build requests to the appropriate toolchain builder.
/// </summary>
public static class ToolchainOrchestrator
{
    public static IToolchainBuilder GetBuilder(string targetId)
    {
        return targetId.ToLowerInvariant() switch
        {
            "sms" => new SmsToolchainBuilder(),
            "nes" => new NesToolchainBuilder(),
            "gamegear" => new GameGearToolchainBuilder(),
            "sg1000" => new Sg1000ToolchainBuilder(),
            "colecovision" => new ColecoVisionToolchainBuilder(),
            _ => throw new NotSupportedException($"Target '{targetId}' is not supported by the toolchain orchestrator.")
        };
    }
}
