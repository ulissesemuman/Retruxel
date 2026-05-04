using Retruxel.Core.Interfaces;

namespace Retruxel.Target.SMS.Palette;

/// <summary>
/// Converts hex colors to SMS RGB222 format.
/// SMS palette format: 6 bits total (2 bits per channel), packed as 0b00BBGGRR.
/// </summary>
public class SmsPaletteConverter : IPaletteConverter
{
    public string TargetId => "sms";

    public byte[] ConvertColors(IEnumerable<string> hexColors)
    {
        return hexColors.Select(hex =>
        {
            if (string.IsNullOrEmpty(hex) || hex.Length < 7 || hex[0] != '#')
                return (byte)0x00;

            // Parse hex to RGB888
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            var rgb888 = (uint)((r << 16) | (g << 8) | b);

            // Use SmsColorUtils for conversion
            return Tools.SmsColorUtils.ConvertToSmsRgb222(rgb888);
        }).ToArray();
    }
}
