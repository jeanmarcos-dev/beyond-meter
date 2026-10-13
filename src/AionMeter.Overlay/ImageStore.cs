using AionMeter.Core.Assets;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AionMeter.Overlay;

public sealed class ImageStore : IDisposable
{
    private readonly SkillIconCache _icons;
    private readonly BossPortraitCache _portraits;
    private readonly Dictionary<string, Bitmap?> _loaded = [];

    public ImageStore(SkillIconCache icons, BossPortraitCache portraits)
    {
        _icons = icons;
        _portraits = portraits;
        _icons.IconReady += _ => Announce();
        _portraits.PortraitReady += _ => Announce();
    }

    public event Action? Changed;

    public bool Enabled
    {
        get => _icons.Enabled;
        set => _icons.Enabled = _portraits.Enabled = value;
    }

    public static ImageStore For(string dataDirectory, string cacheHome, bool enabled) =>
        new(new SkillIconCache(dataDirectory, Path.Combine(cacheHome, "icons"), enabled),
            new BossPortraitCache(dataDirectory, Path.Combine(cacheHome, "portraits"), enabled));

    public Bitmap? SkillIcon(int skillCode) => Load(_icons.FileOf(skillCode));

    public Bitmap? Portrait(int bossCode, int targetCode) => Load(_portraits.FileOf(bossCode, targetCode));

    public void Dispose()
    {
        _icons.Dispose();
        _portraits.Dispose();
        foreach (var bitmap in _loaded.Values) bitmap?.Dispose();
    }

    private Bitmap? Load(string? path)
    {
        if (path is null) return null;
        if (_loaded.TryGetValue(path, out var known)) return known;
        Bitmap? bitmap;
        try
        {
            bitmap = new Bitmap(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            bitmap = null;
        }
        _loaded[path] = bitmap;
        return bitmap;
    }

    private void Announce() => Dispatcher.UIThread.Post(() => Changed?.Invoke());
}
