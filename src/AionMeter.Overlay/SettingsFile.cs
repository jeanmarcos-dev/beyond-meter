using AionMeter.Core.Settings;

namespace AionMeter.Overlay;

public static class SettingsFile
{
    public const string FileName = "settings.json";
    public const string LegacyInterfaceLanguage = "es";

    public static string PathIn(string configDirectory) => System.IO.Path.Combine(configDirectory, FileName);

    public static UserSettings Load(string configDirectory, string? legacyPath)
    {
        var path = PathIn(configDirectory);
        if (File.Exists(path) || legacyPath is null || !File.Exists(legacyPath)) return UserSettings.Load(path);
        var imported = FromOverlay(OverlaySettings.Load(legacyPath));
        imported.Save(path);
        return imported;
    }

    public static UserSettings FromOverlay(OverlaySettings legacy) => new()
    {
        OverlayLeft = legacy.X,
        OverlayTop = legacy.Y,
        OverlayWidth = legacy.Width,
        Locked = legacy.Locked,
        MaxRows = legacy.MaxRows,
        TargetMode = legacy.TargetMode,
        Language = LegacyInterfaceLanguage,
        NamesLanguage = legacy.Language,
        IdleTimeoutSec = legacy.IdleTimeoutSec,
        BossIdleTimeoutSec = legacy.BossIdleTimeoutSec,
        HistoryMode = legacy.HistoryMode,
        HistoryMinSeconds = legacy.HistoryMinSeconds,
        LockedOpacity = legacy.LockedOpacity,
        BossAlertMinutes = legacy.BossAlertMinutes,
        AutoHideSeconds = legacy.AutoHideSeconds,
        BreakdownWidth = legacy.BreakdownWidth,
        BreakdownHeight = legacy.BreakdownHeight,
        BackgroundOpacity = legacy.Opacity,
    };
}
