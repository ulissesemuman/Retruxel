using Retruxel.Core.Interfaces;
using System.Net.Sockets;
using System.Text;

namespace Retruxel.Tool.LiveLink.Emulators;

/// <summary>
/// BGB emulator connection via debug API.
/// Supports Game Boy and Game Boy Color.
/// Protocol: https://bgb.bircd.org/manual.html#expressions
/// </summary>
public class BgbConnection : IEmulatorConnection
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public string EmulatorId => "bgb";
    public string DisplayName => "BGB";
    public string[] SupportedTargets => new[] { "gb", "gbc" };
    public bool IsConnected => _client?.Connected == true;

    public async Task<bool> ConnectAsync(string host = "localhost", int port = 8765)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();

            // BGB doesn't require handshake, just start sending commands
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
        var bytes = new byte[length];

        for (int i = 0; i < length; i++)
        {
            var command = $"r {address + i:X4}\n";
            var response = await SendCommandAsync(command);
            bytes[i] = ParseByteResponse(response);
        }

        return bytes;
    }

    public async Task<byte[]> ReadVramAsync(uint address, int length)
    {
        // BGB uses same memory read for VRAM (0x8000-0x9FFF range)
        return await ReadMemoryAsync(0x8000 + address, length);
    }

    public async Task<EmulatorState> GetStateAsync()
    {
        var command = "r pc\n";
        var response = await SendCommandAsync(command);

        return new EmulatorState
        {
            IsRunning = !string.IsNullOrEmpty(response),
            IsPaused = false
        };
    }

    private async Task<string> SendCommandAsync(string command)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected");

        var data = Encoding.ASCII.GetBytes(command);
        await _stream.WriteAsync(data);

        var buffer = new byte[1024];
        int bytesRead = await _stream.ReadAsync(buffer);
        return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
    }

    private byte ParseByteResponse(string response)
    {
        // BGB returns format like "A=42" or just "42"
        var parts = response.Split('=');
        var hexValue = parts.Length > 1 ? parts[1].Trim() : response.Trim();

        if (byte.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out byte result))
            return result;

        return 0;
    }
}
