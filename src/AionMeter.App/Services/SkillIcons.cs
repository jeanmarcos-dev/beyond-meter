using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.Core.Assets;

namespace AionMeter.App.Services;

/// <summary>
/// Skill icons from the official AION 2 game-data CDN, cached in %AppData%\AionMeter\icons. Lookups never block:
/// a missing icon starts a background download and <see cref="IconReady"/> fires when it lands.
/// </summary>
public sealed class SkillIcons
{
    private readonly SkillIconCache _cache;
    private readonly ConcurrentDictionary<string, ImageSource?> _loaded = new();

    public SkillIcons(string dataDirectory, bool enabled)
    {
        _cache = new SkillIconCache(dataDirectory, Path.Combine(AppSettings.AppDataDir, "icons"), enabled, log: Log.Info);
        _cache.IconReady += _ => IconReady?.Invoke();
    }

    public bool Enabled
    {
        get => _cache.Enabled;
        set => _cache.Enabled = value;
    }

    /// <summary>Raised on a worker thread when a downloaded icon becomes available.</summary>
    public event Action? IconReady;

    public ImageSource? Get(int skillCode) =>
        _cache.FileOf(skillCode) is { } file ? _loaded.GetOrAdd(file, Load) : null;

    private static ImageSource? Load(string file)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(file);
            bmp.DecodePixelWidth = 48;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException)
        {
            return null;
        }
    }
}
