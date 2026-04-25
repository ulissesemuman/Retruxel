using Retruxel.Core.Interfaces;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Retruxel.Tool.LiveLink.Emulators;

/// <summary>
/// Mesen-S emulator connection via debug API.
/// Supports Super Nintendo Entertainment System.
/// Uses same TCP/JSON protocol as Mesen.
/// </summary>
public class MesenSConnection : IEmulatorConnection
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public string EmulatorId => "mesen-s";
    public string DisplayName => "Mesen-S";
    public string[] SupportedTargets => new[] { "snes" };
    public bool IsConnected => _client?.Connected == true;

    public async Task<bool> ConnectAsync(string host = "localhost", int port = 8888)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();

            // Send handshake
            var handshake = new { command = "connect", version = "1.0" };
            await SendCommandAsync(handshake);

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
        var command = new
        {
            command = "readMemory",
            address = address,
            length = length,
            memoryType = "CpuMemory"
        };

        var response = await SendCommandAsync(command);
        return Convert.FromBase64String(response.GetProperty("data").GetString() ?? "");
    }

    public async Task<byte[]> ReadVramAsync(uint address, int length)
    {
        var command = new
        {
            command = "readMemory",
            address = address,
            length = length,
            memoryType = "VideoRam"
        };

        var response = await SendCommandAsync(command);
        return Convert.FromBase64String(response.GetProperty("data").GetString() ?? "");
    }

    public async Task<EmulatorState> GetStateAsync()
    {
        var command = new { command = "getState" };
        var response = await SendCommandAsync(command);

        return new EmulatorState
        {
            IsPaused = response.GetProperty("paused").GetBoolean(),
            IsRunning = response.GetProperty("running").GetBoolean(),
            LoadedRom = response.GetProperty("romName").GetString()
        };
    }

    private async Task<JsonElement> SendCommandAsync(object command)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected");

        var json = JsonSerializer.Serialize(command);
        var data = Encoding.UTF8.GetBytes(json + "\n");

        await _stream.WriteAsync(data);

        var buffer = new byte[65536];
        int bytesRead = await _stream.ReadAsync(buffer);
        var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        return JsonSerializer.Deserialize<JsonElement>(responseJson);
    }
}
