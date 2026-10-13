using AionMeter.Core.Events;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AionMeter.Overlay;

public static class ClassIcons
{
    private static readonly Dictionary<GameClass, Bitmap?> Cache = [];

    public static Bitmap? For(GameClass cls)
    {
        if (cls == GameClass.Unknown) return null;
        lock (Cache)
        {
            if (!Cache.TryGetValue(cls, out var icon))
            {
                icon = Load(cls);
                Cache[cls] = icon;
            }
            return icon;
        }
    }

    private static Bitmap? Load(GameClass cls)
    {
        var uri = new Uri($"avares://aionmeter-overlay/Assets/Classes/{cls.ToString().ToLowerInvariant()}.png");
        if (!AssetLoader.Exists(uri)) return null;
        using var stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }
}
