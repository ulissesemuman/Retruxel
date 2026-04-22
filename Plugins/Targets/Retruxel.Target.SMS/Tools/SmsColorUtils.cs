namespace Retruxel.Target.SMS.Tools;

/// <summary>
/// Shared utilities for SMS color format conversion.
/// SMS uses RGB222 format: 0bBBGGRR (2 bits per channel, 6 bits total).
/// Each 2-bit channel maps to: 0→0, 1→85, 2→170, 3→255
/// </summary>
public static class SmsColorUtils
{
    /// <summary>
    /// SMS color levels for each 2-bit channel value.
    /// </summary>
    public static readonly byte[] ColorLevels = { 0, 85, 170, 255 };

    /// <summary>
    /// Generates all 64 possible SMS hardware colors.
    /// Returns array of anonymous objects with R, G, B properties.
    /// </summary>
    public static object[] GenerateAllColors()
    {
        var colors = new object[64];
        int index = 0;
        
        for (int b = 0; b < 4; b++)
        {
            for (int g = 0; g < 4; g++)
            {
                for (int r = 0; r < 4; r++)
                {
                    colors[index++] = new 
                    { 
                        R = ColorLevels[r], 
                        G = ColorLevels[g], 
                        B = ColorLevels[b] 
                    };
                }
            }
        }
        
        return colors;
    }

    /// <summary>
    /// Converts RGB888 (24-bit) color to SMS RGB222 (6-bit) format.
    /// SMS format: 0bBBGGRR
    /// </summary>
    public static byte ConvertToSmsRgb222(uint rgb888)
    {
        byte r = (byte)((rgb888 >> 16) & 0xFF);
        byte g = (byte)((rgb888 >> 8) & 0xFF);
        byte b = (byte)(rgb888 & 0xFF);

        // Convert 8-bit to 2-bit per channel
        byte r2 = (byte)((r >> 6) & 0x03);
        byte g2 = (byte)((g >> 6) & 0x03);
        byte b2 = (byte)((b >> 6) & 0x03);

        // SMS format: 0bBBGGRR
        return (byte)(r2 | (g2 << 2) | (b2 << 4));
    }

    /// <summary>
    /// Converts an array of RGB888 colors to SMS RGB222 format.
    /// </summary>
    public static byte[] ConvertPaletteToSmsRgb222(uint[] rgbPalette)
    {
        var result = new byte[rgbPalette.Length];
        for (int i = 0; i < rgbPalette.Length; i++)
        {
            result[i] = ConvertToSmsRgb222(rgbPalette[i]);
        }
        return result;
    }

    /// <summary>
    /// Converts SMS RGB222 byte to RGB888 (24-bit) color.
    /// </summary>
    public static uint ConvertFromSmsRgb222(byte smsColor)
    {
        byte r2 = (byte)(smsColor & 0x03);
        byte g2 = (byte)((smsColor >> 2) & 0x03);
        byte b2 = (byte)((smsColor >> 4) & 0x03);

        byte r = ColorLevels[r2];
        byte g = ColorLevels[g2];
        byte b = ColorLevels[b2];

        return (uint)((r << 16) | (g << 8) | b);
    }
}
