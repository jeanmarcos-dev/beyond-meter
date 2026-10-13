using System.Text.Json;
using AionMeter.Core.Combat;

namespace AionMeter.Core.Assets;

public sealed class BossPortraitCache : IDisposable
{
    public const string BaseUrl = "https://metabot.gg/web/aion2/npcs/";

    private readonly Dictionary<int, string> _files = new();
    private readonly RemoteImageCache _cache;

    public BossPortraitCache(string dataDirectory, string cacheDirectory, bool enabled, HttpMessageHandler? handler = null, Action<string>? log = null)
    {
        var options = new RemoteImageOptions(BaseUrl, cacheDirectory, ".webp", "image/webp", TimeSpan.FromDays(7),
            "BeyondMeter/0.1 (portraits credited to MetaBot.GG)");
        _cache = new RemoteImageCache(options, handler, log) { Enabled = enabled };
        _cache.Ready += name => PortraitReady?.Invoke(name);
        LoadMap(Path.Combine(dataDirectory, "boss_portraits.json"), log ?? (_ => { }));
    }

    public bool Enabled
    {
        get => _cache.Enabled;
        set => _cache.Enabled = value;
    }

    public bool Busy => _cache.Busy;

    public event Action<string>? PortraitReady;

    public string? FileOf(int npcCode) =>
        npcCode <= 0 ? null : _cache.Get(_files.TryGetValue(npcCode, out var file) ? file : npcCode.ToString());

    public string? FileOf(int bossCode, int targetCode) => FileOf(bossCode != 0 ? bossCode : targetCode);

    public string? FileOf(EncounterSnapshot s) => FileOf(s.Boss?.NpcCode ?? 0, s.TargetCode);

    private void LoadMap(string path, Action<string> log)
    {
        if (!File.Exists(path)) return;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("portraits", out var map)) return;
            foreach (var p in map.EnumerateObject())
                if (int.TryParse(p.Name, out var code) && p.Value.GetString() is { Length: > 0 } file)
                    _files[code] = file;
        }
        catch (JsonException ex)
        {
            log($"boss_portraits.json ignored: {ex.Message}");
        }
    }

    public void Dispose() => _cache.Dispose();
}
