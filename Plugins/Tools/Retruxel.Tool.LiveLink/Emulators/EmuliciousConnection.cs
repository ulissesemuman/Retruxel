using System.Net.Sockets;
using System.Text;
using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.LiveLink.Emulators;

/// <summary>
/// Emulicious emulator connection via debug API.
/// Supports SMS, Game Gear, SG-1000, ColecoVision.
/// </summary>
public class EmuliciousConnection : IEmulatorConnection
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public string EmulatorId => "emulicious";
    public string DisplayName => "Emulicious";
    public string[] SupportedTargets => new[] { "sms", "gg", "sg1000", "coleco" };
    public bool IsConnected => _client?.Connected == true;

    public async Task<bool> ConnectAsync(string host = "localhost", int port = 58870)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            return true;
        }
        catch
        {
            _client?.Dispose();
            _client = null;
            _stream = null;
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_stream != null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }
        
        _client?.Dispose();
        _client = null;
    }

    public async Task<byte[]> ReadMemoryAsync(uint address, int length)
    {
        var command = $"r {address:X4} {length}\n";
        var response = await SendCommandAsync(command);
        return ParseHexResponse(response);
    }

    public async Task<byte[]> ReadVramAsync(uint address, int length)
    {
        var command = $"rv {address:X4} {length}\n";
        var response = await SendCommandAsync(command);
        return ParseHexResponse(response);
    }

    public async Task<EmulatorState> GetStateAsync()
    {
        var command = "status\n";
        var response = await SendCommandAsync(command);
        
        return new EmulatorState
        {
            IsPaused = response.Contains("paused", StringComparison.OrdinalIgnoreCase),
            IsRunning = response.Contains("running", StringComparison.OrdinalIgnoreCase)
        };
    }

    private async Task<string> SendCommandAsync(string command)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected");

        var data = Encoding.ASCII.GetBytes(command);
        await _stream.WriteAsync(data);
        
        var buffer = new byte[65536];
        int bytesRead = await _stream.ReadAsync(buffer);
        return Encoding.ASCII.GetString(buffer, 0, bytesRead);
    }

    private byte[] ParseHexResponse(string response)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var bytes = new List<byte>();
        
        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts.Skip(1))
            {
                if (byte.TryParse(part, System.Globalization.NumberStyles.HexNumber, null, out byte b))
                    bytes.Add(b);
            }
        }
        
        return bytes.ToArray();
    }
}
