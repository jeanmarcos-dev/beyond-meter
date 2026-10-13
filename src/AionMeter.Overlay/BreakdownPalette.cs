using AionMeter.Core.Events;
using Avalonia.Media;

namespace AionMeter.Overlay;

public static class BreakdownPalette
{
    public static readonly Color Background = Color.FromRgb(0x0A, 0x0E, 0x16);
    public static readonly Color Gold = Color.FromRgb(0xF5, 0xC5, 0x42);
    public static readonly Color Muted = Color.FromRgb(0xA8, 0xB3, 0xC7);

    public static Color Class(GameClass gameClass)
    {
        var rgb = ClassColors.Rgb(gameClass);
        return Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}
