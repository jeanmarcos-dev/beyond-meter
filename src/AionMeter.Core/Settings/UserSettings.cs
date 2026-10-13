using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AionMeter.Core.Combat;
using AionMeter.Core.Text;

namespace AionMeter.Core.Settings;

public enum HistoryMode
{
    BossesOnly,
    BossesAndLong,
    All,
}

public sealed partial class UserSettings
{
    public const int CurrentLayout = 4;
    public const int MinRowSize = 50, MaxRowSize = 100;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public double OverlayLeft { get; set; } = 80;
    public double OverlayTop { get; set; } = 160;
    public double OverlayWidth { get; set; } = 460;
    public double OverlayHeight { get; set; } = 330;
    public double BackgroundOpacity { get; set; } = 0.9;
    public double LockedOpacity { get; set; } = 0.55;
    public bool Locked { get; set; }
    public bool ClickThrough { get; set; }
    public bool OverlayVisible { get; set; } = true;
    public bool AutoShow { get; set; } = true;
    public int AutoHideSeconds { get; set; } = 60;
    public bool ShowOnStart { get; set; } = true;
    public int BossAlertMinutes { get; set; } = 2;
    public TargetMode TargetMode { get; set; } = TargetMode.BossOnly;
    public bool BarsRelativeToTop { get; set; } = true;
    public int MaxRows { get; set; } = 10;
    public int RowSize { get; set; } = 100;
    public bool ShowGear { get; set; } = true;
    public bool ShowBossPanel { get; set; } = true;
    public int IdleTimeoutSec { get; set; } = 10;
    public int BossIdleTimeoutSec { get; set; } = 30;
    public string Language { get; set; } = "en";
    public string? NamesLanguage { get; set; }
    public bool DownloadIcons { get; set; } = true;
    public bool CheckUpdates { get; set; } = true;
    public bool AutoInstallUpdates { get; set; } = true;
    public string? SkippedUpdate { get; set; }
    public bool SaveHistory { get; set; } = true;
    public HistoryMode HistoryMode { get; set; } = HistoryMode.BossesAndLong;
    public int HistoryMinSeconds { get; set; } = 20;
    public bool RecordPackets { get; set; }
    public string? CaptureDevice { get; set; }
    public string HotkeyToggleOverlay { get; set; } = "Ctrl+Shift+D";
    public string HotkeyReset { get; set; } = "Ctrl+Shift+R";
    public string HotkeyClickThrough { get; set; } = "Ctrl+Shift+L";
    public string HotkeyTimers { get; set; } = "Ctrl+Shift+T";
    public double BreakdownWidth { get; set; } = 960;
    public double BreakdownHeight { get; set; } = 640;
    public int LayoutVersion { get; set; } = CurrentLayout;

    [JsonIgnore]
    public string EffectiveNamesLanguage => NamesLanguage ?? Language;

    public static UserSettings Load(string path)
    {
        try
        {
            return File.Exists(path) ? Parse(File.ReadAllText(path)) : new UserSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new UserSettings();
        }
    }

    public static UserSettings Parse(string json)
    {
        var s = new UserSettings();
        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(json);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return s;
        }
        if (root.ValueKind != JsonValueKind.Object) return s;

        var layout = Int(root, nameof(LayoutVersion), 0, 0, int.MaxValue);
        s.OverlayLeft = Number(root, nameof(OverlayLeft), s.OverlayLeft, -100_000, 100_000);
        s.OverlayTop = Number(root, nameof(OverlayTop), s.OverlayTop, -100_000, 100_000);
        s.OverlayWidth = Number(root, nameof(OverlayWidth), s.OverlayWidth, 280, 2000);
        s.OverlayHeight = Number(root, nameof(OverlayHeight), s.OverlayHeight, 160, 2000);
        s.BackgroundOpacity = Number(root, nameof(BackgroundOpacity), s.BackgroundOpacity, 0.15, 1);
        s.LockedOpacity = Number(root, nameof(LockedOpacity), s.LockedOpacity, 0.15, 1);
        s.Locked = Bool(root, nameof(Locked), s.Locked);
        s.ClickThrough = Bool(root, nameof(ClickThrough), s.ClickThrough);
        s.OverlayVisible = Bool(root, nameof(OverlayVisible), s.OverlayVisible);
        s.AutoShow = Bool(root, nameof(AutoShow), s.AutoShow);
        s.AutoHideSeconds = Int(root, nameof(AutoHideSeconds), s.AutoHideSeconds, 0, 600);
        s.ShowOnStart = Bool(root, nameof(ShowOnStart), s.ShowOnStart);
        s.BossAlertMinutes = Int(root, nameof(BossAlertMinutes), s.BossAlertMinutes, 0, 30);
        s.TargetMode = Choice(root, nameof(TargetMode), s.TargetMode);
        s.BarsRelativeToTop = Bool(root, nameof(BarsRelativeToTop), s.BarsRelativeToTop);
        s.MaxRows = Int(root, nameof(MaxRows), s.MaxRows, 1, 20);
        s.RowSize = Int(root, nameof(RowSize), s.RowSize, MinRowSize, MaxRowSize);
        s.ShowGear = Bool(root, nameof(ShowGear), s.ShowGear);
        s.ShowBossPanel = Bool(root, nameof(ShowBossPanel), s.ShowBossPanel);
        s.IdleTimeoutSec = Int(root, nameof(IdleTimeoutSec), s.IdleTimeoutSec, 3, 120);
        s.BossIdleTimeoutSec = Int(root, nameof(BossIdleTimeoutSec), s.BossIdleTimeoutSec, 5, 300);
        s.Language = layout is > 0 and < 4 ? "en" : UiText.Normalize(Text(root, nameof(Language)));
        s.NamesLanguage = Text(root, nameof(NamesLanguage)) is { } names && DataLanguage().IsMatch(names) ? names : null;
        s.DownloadIcons = Bool(root, nameof(DownloadIcons), s.DownloadIcons);
        s.CheckUpdates = Bool(root, nameof(CheckUpdates), s.CheckUpdates);
        s.AutoInstallUpdates = Bool(root, nameof(AutoInstallUpdates), s.AutoInstallUpdates);
        s.SkippedUpdate = Text(root, nameof(SkippedUpdate));
        s.SaveHistory = Bool(root, nameof(SaveHistory), s.SaveHistory);
        s.HistoryMode = Choice(root, nameof(HistoryMode), s.HistoryMode);
        s.HistoryMinSeconds = Int(root, nameof(HistoryMinSeconds), s.HistoryMinSeconds, 5, 600);
        s.RecordPackets = Bool(root, nameof(RecordPackets), s.RecordPackets);
        s.CaptureDevice = Text(root, nameof(CaptureDevice));
        s.HotkeyToggleOverlay = Text(root, nameof(HotkeyToggleOverlay)) ?? s.HotkeyToggleOverlay;
        s.HotkeyReset = Text(root, nameof(HotkeyReset)) ?? s.HotkeyReset;
        s.HotkeyClickThrough = Text(root, nameof(HotkeyClickThrough)) ?? s.HotkeyClickThrough;
        s.HotkeyTimers = Text(root, nameof(HotkeyTimers)) ?? s.HotkeyTimers;
        s.BreakdownWidth = Number(root, nameof(BreakdownWidth), s.BreakdownWidth, 480, 3000);
        s.BreakdownHeight = Number(root, nameof(BreakdownHeight), s.BreakdownHeight, 320, 2000);
        if (layout is > 0 and < 3)
        {
            s.OverlayWidth = Math.Max(s.OverlayWidth, 470);
            s.OverlayHeight = Math.Max(s.OverlayHeight, 540);
        }
        return s;
    }

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, ToJson());
        File.Move(tmp, path, overwrite: true);
    }

    public void ApplyTo(MeterOptions options)
    {
        options.TargetMode = TargetMode;
        options.IdleTimeoutMs = IdleTimeoutSec * 1000;
        options.BossIdleTimeoutMs = BossIdleTimeoutSec * 1000;
    }

    private static int Int(JsonElement root, string name, int fallback, int min, int max) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var n)
            ? (int)Math.Clamp(Math.Round(n), min, max)
            : fallback;

    private static double Number(JsonElement root, string name, double fallback, double min, double max) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var n)
            ? Math.Clamp(n, min, max)
            : fallback;

    private static bool Bool(JsonElement root, string name, bool fallback) =>
        root.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : fallback;

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static T Choice<T>(JsonElement root, string name, T fallback) where T : struct, Enum =>
        Text(root, name) is { } text && text.All(char.IsAsciiLetter) && Enum.TryParse<T>(text, ignoreCase: true, out var value)
            ? value
            : fallback;

    [GeneratedRegex("^[A-Za-z]{2}(-[A-Za-z]{2,4})?$")]
    private static partial Regex DataLanguage();
}
