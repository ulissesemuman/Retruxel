namespace Retruxel.SDK.LiveLink;

/// <summary>
/// Interface for emulator debug connections.
/// </summary>
public interface IEmulatorConnection
{
    string EmulatorId { get; }
    string DisplayName { get; }
    string[] SupportedTargets { get; }
    bool IsConnected { get; }
    
    Task<bool> ConnectAsync(string host = "localhost", int port = 0);
    Task DisconnectAsync();
    Task<byte[]> ReadMemoryAsync(uint address, int length);
    Task<byte[]> ReadVramAsync(uint address, int length);
    Task<EmulatorState> GetStateAsync();
}

public class EmulatorState
{
    public bool IsPaused { get; set; }
    public bool IsRunning { get; set; }
    public string? LoadedRom { get; set; }
    public Dictionary<string, object> CustomData { get; set; } = new();
}
