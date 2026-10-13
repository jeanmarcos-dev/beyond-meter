using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AionMeter.Core.Combat;

namespace AionMeter.Overlay;

public sealed partial record OverlaySettings(
    int X = 40,
    int Y = 240,
    double Width = 420,
    bool Locked = false,
    int MaxRows = 12,
    TargetMode TargetMode = TargetMode.BossOnly,
    string Language = "en",
    int IdleTimeoutSec = 10,
    int BossIdleTimeoutSec = 30,
    HistoryMode HistoryMode = HistoryMode.BossesAndLong,
    int HistoryMinSeconds = 20,
    double LockedOpacity = 0.55,
    int BossAlertMinutes = 2,
    int AutoHideSeconds = 60,
    double BreakdownWidth = 960,
    double BreakdownHeight = 640,
    double Opacity = 0.85)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public static string DefaultPath()
    {
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(config))
            config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(config, "aionmeter", "overlay.json");
    }

    public static OverlaySettings Load(string path)
    {
        try
        {
            return Parse(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new OverlaySettings();
        }
    }

    public static OverlaySettings Parse(string json)
    {
        var d = new OverlaySettings();
        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(json);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return d;
        }
        if (root.ValueKind != JsonValueKind.Object) return d;

        return new OverlaySettings(
            Int(root, nameof(X), d.X, int.MinValue, int.MaxValue),
            Int(root, nameof(Y), d.Y, int.MinValue, int.MaxValue),
            Number(root, nameof(Width), d.Width, 280, 2000),
            Bool(root, nameof(Locked), d.Locked),
            Int(root, nameof(MaxRows), d.MaxRows, 1, 20),
            Choice(root, nameof(TargetMode), d.TargetMode),
            Text(root, nameof(Language)) is { } language && LanguageCode().IsMatch(language) ? language : d.Language,
            Int(root, nameof(IdleTimeoutSec), d.IdleTimeoutSec, 3, 120),
            Int(root, nameof(BossIdleTimeoutSec), d.BossIdleTimeoutSec, 5, 300),
            Choice(root, nameof(HistoryMode), d.HistoryMode),
            Int(root, nameof(HistoryMinSeconds), d.HistoryMinSeconds, 5, 600),
            Number(root, nameof(LockedOpacity), d.LockedOpacity, 0.15, 1),
            Int(root, nameof(BossAlertMinutes), d.BossAlertMinutes, 0, 30),
            Int(root, nameof(AutoHideSeconds), d.AutoHideSeconds, 0, 600),
            Number(root, nameof(BreakdownWidth), d.BreakdownWidth, 480, 3000),
            Number(root, nameof(BreakdownHeight), d.BreakdownHeight, 320, 2000),
            Number(root, nameof(Opacity), d.Opacity, 0.15, 1));
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, Json));
        File.Move(tmp, path, overwrite: true);
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
    private static partial Regex LanguageCode();
}
