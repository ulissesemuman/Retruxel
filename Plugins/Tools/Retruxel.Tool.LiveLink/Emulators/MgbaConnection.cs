using Retruxel.Core.Interfaces;
using System.Net.Sockets;
using System.Text;

namespace Retruxel.Tool.LiveLink.Emulators;

/// <summary>
/// mGBA emulator connection via GDB remote protocol.
/// Supports Game Boy Advance.
/// Protocol: https://sourceware.org/gdb/onlinedocs/gdb/Remote-Protocol.html
/// </summary>
public class MgbaConnection : IEmulatorConnection
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public string EmulatorId => "mgba";
    public string DisplayName => "mGBA";
    public string[] SupportedTargets => new[] { "gba", "gb", "gbc" };
    public bool IsConnected => _client?.Connected == true;

    public async Task<bool> ConnectAsync(string host = "localhost", int port = 2345)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();

            // GDB handshake
            await SendGdbCommandAsync("+");

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
        // GDB command: m<addr>,<length>
        var command = $"m{address:x},{length:x}";
        var response = await SendGdbCommandAsync(command);

        return ParseHexData(response);
    }

    public async Task<byte[]> ReadVramAsync(uint address, int length)
    {
        // GB/GBC VRAM: 0x8000-0x9FFF (8KB for GB, 16KB for GBC)
        // GBA VRAM: 0x06000000-0x06017FFF (96KB)
        // For now, assume GBA. GB/GBC detection would require reading memory to identify console.
        return await ReadMemoryAsync(0x06000000 + address, length);
    }

    public async Task<EmulatorState> GetStateAsync()
    {
        // GDB command: ?
        var response = await SendGdbCommandAsync("?");

        return new EmulatorState
        {
            IsRunning = !response.StartsWith("S"),
            IsPaused = response.StartsWith("S")
        };
    }

    private async Task<string> SendGdbCommandAsync(string command)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected");

        // Calculate checksum
        int checksum = 0;
        foreach (char c in command)
            checksum += c;
        checksum &= 0xFF;

        // Format: $<command>#<checksum>
        var packet = $"${command}#{checksum:x2}";
        var data = Encoding.ASCII.GetBytes(packet);

        await _stream.WriteAsync(data);

        var buffer = new byte[65536];
        int bytesRead = await _stream.ReadAsync(buffer);
        var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

        // Strip $...# wrapper
        if (response.StartsWith("$") && response.Contains("#"))
        {
            int start = 1;
            int end = response.IndexOf('#');
            return response.Substring(start, end - start);
        }

        return response;
    }

    private byte[] ParseHexData(string hexString)
    {
        var bytes = new List<byte>();

        for (int i = 0; i < hexString.Length; i += 2)
        {
            if (i + 1 < hexString.Length)
            {
                var hex = hexString.Substring(i, 2);
                if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out byte b))
                    bytes.Add(b);
            }
        }

        return bytes.ToArray();
    }
}
