using System;

namespace Retruxel.Lib.ImageProcessing.Compression;

/// <summary>
/// Konami RLE compression used in some NES games
/// TODO: Implement when needed for ROM asset extraction
/// </summary>
public class KonamiRleCompression
{
    public byte[] Compress(byte[] data)
    {
        throw new NotImplementedException("Konami RLE compression not yet implemented");
    }

    public byte[] Decompress(byte[] data)
    {
        throw new NotImplementedException("Konami RLE decompression not yet implemented");
    }
}

/// <summary>
/// GBA BIOS compression (LZ77 variant)
/// TODO: Implement when needed for ROM asset extraction
/// </summary>
public class GbaBiosCompression
{
    public byte[] Compress(byte[] data)
    {
        throw new NotImplementedException("GBA BIOS compression not yet implemented");
    }

    public byte[] Decompress(byte[] data)
    {
        throw new NotImplementedException("GBA BIOS decompression not yet implemented");
    }
}

/// <summary>
/// SNES Mode 7 specific compression
/// TODO: Implement when needed for ROM asset extraction
/// </summary>
public class SnesMode7Compression
{
    public byte[] Compress(byte[] data)
    {
        throw new NotImplementedException("SNES Mode 7 compression not yet implemented");
    }

    public byte[] Decompress(byte[] data)
    {
        throw new NotImplementedException("SNES Mode 7 decompression not yet implemented");
    }
}

/// <summary>
/// PCEngine/TurboGrafx-16 RLE compression
/// TODO: Implement when needed for ROM asset extraction
/// </summary>
public class PceRleCompression
{
    public byte[] Compress(byte[] data)
    {
        throw new NotImplementedException("PCE RLE compression not yet implemented");
    }

    public byte[] Decompress(byte[] data)
    {
        throw new NotImplementedException("PCE RLE decompression not yet implemented");
    }
}
