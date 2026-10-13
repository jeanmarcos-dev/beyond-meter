using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.Core.Assets;
using AionMeter.Core.Combat;

namespace AionMeter.App.Services;

/// <summary>
/// Boss portraits (game art © NCSOFT) from MetaBot.GG's public AION 2 database, cached in
/// %AppData%\AionMeter\portraits. Lookups never block — <see cref="PortraitReady"/> fires when a download lands.
/// </summary>
public sealed class BossPortraits
{
    private readonly BossPortraitCache _cache;
    private readonly ConcurrentDictionary<string, ImageSource?> _loaded = new();

    public BossPortraits(string dataDirectory, bool enabled)
    {
        _cache = new BossPortraitCache(dataDirectory, Path.Combine(AppSettings.AppDataDir, "portraits"), enabled, log: Log.Info);
        _cache.PortraitReady += _ => PortraitReady?.Invoke();
    }

    public bool Enabled
    {
        get => _cache.Enabled;
        set => _cache.Enabled = value;
    }

    /// <summary>True while downloads are still running (the offscreen render tool waits for them).</summary>
    public bool Busy => _cache.Busy;

    /// <summary>Raised on a worker thread when a downloaded portrait becomes available.</summary>
    public event Action? PortraitReady;

    public ImageSource? Of(int bossCode, int targetCode) => Image(_cache.FileOf(bossCode, targetCode));

    public ImageSource? Of(EncounterSnapshot s) => Image(_cache.FileOf(s));

    public ImageSource? Get(int npcCode) => Image(_cache.FileOf(npcCode));

    private ImageSource? Image(string? file) => file is null ? null : _loaded.GetOrAdd(file, Load);

    /// <summary>WebP goes through the Windows imaging codec (built into Windows 11; "WebP Image Extensions" on 10).</summary>
    private static ImageSource? Load(string file)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(file);
            bmp.DecodePixelWidth = 160;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException or ArgumentException)
        {
            Log.Info($"Portrait {Path.GetFileName(file)} could not be decoded: {ex.Message}");
            return null;
        }
    }
}
