using System.Text.Json;

namespace AionMeter.Core.Assets;

public sealed class SkillIconCache : IDisposable
{
    public const string BaseUrl = "https://assets.playnccdn.com/static-aion2-gamedata/resources/";

    private static readonly Dictionary<int, string> ClassCodes = new()
    {
        [11] = "GL", [12] = "TE", [13] = "AS", [14] = "RA", [15] = "SO",
        [16] = "EL", [17] = "CL", [18] = "CH", [19] = "GT",
    };

    private readonly Dictionary<int, string> _overrides = new();
    private readonly RemoteImageCache _cache;

    public SkillIconCache(string dataDirectory, string cacheDirectory, bool enabled, HttpMessageHandler? handler = null, Action<string>? log = null)
    {
        _cache = new RemoteImageCache(new RemoteImageOptions(BaseUrl, cacheDirectory, ".png"), handler, log) { Enabled = enabled };
        _cache.Ready += name => IconReady?.Invoke(name);
        LoadOverrides(Path.Combine(dataDirectory, "skill_icons.json"));
    }

    public bool Enabled
    {
        get => _cache.Enabled;
        set => _cache.Enabled = value;
    }

    public bool Busy => _cache.Busy;

    public event Action<string>? IconReady;

    public string? IconName(int skillCode)
    {
        var text = skillCode.ToString();
        if (skillCode > 999 && int.TryParse(text[..4], out var key) && _overrides.TryGetValue(key, out var icon))
            return icon;

        if (skillCode is >= 30_000_000 and <= 30_999_999 && text.Length >= 7)
        {
            return int.TryParse(text.Substring(5, 2), out var stone) && stone > 0
                ? $"Icon_Item_Usable_Godstone_WP_r_{stone:x3}"
                : null;
        }

        if (skillCode is < 10_000_000 or > 19_999_999) return null;
        if (skillCode / 100 % 10_000 == 0 && skillCode % 100 != 0) return null;
        if (!ClassCodes.TryGetValue(skillCode / 1_000_000, out var cls)) return null;
        var sub = skillCode / 10_000 % 100;
        var passive = sub >= 70;
        if (passive) sub -= 70;
        return $"ICON_{cls}_SKILL_{(passive ? "Passive_" : "")}{sub:D3}";
    }

    public string? FileOf(int skillCode) => IconName(skillCode) is { } name ? _cache.Get(name) : null;

    private void LoadOverrides(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
            if (map is null) return;
            foreach (var (k, v) in map)
                if (int.TryParse(k, out var key) && v.Length > 0) _overrides[key] = v;
        }
        catch (JsonException)
        {
        }
    }

    public void Dispose() => _cache.Dispose();
}
