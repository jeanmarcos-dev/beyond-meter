using AionMeter.Core.Combat;
using AionMeter.Core.Settings;

namespace AionMeter.Tests;

public sealed class UserSettingsTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("user-settings").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private const string WpfSettings = """
        {
          "OverlayLeft": 1520,
          "OverlayTop": 210,
          "OverlayWidth": 470,
          "OverlayHeight": 540,
          "BackgroundOpacity": 0.8,
          "Locked": true,
          "ClickThrough": false,
          "OverlayVisible": true,
          "AutoShow": true,
          "AutoHideSeconds": 90,
          "ShowOnStart": true,
          "BossAlertMinutes": 5,
          "TargetMode": "All",
          "BarsRelativeToTop": false,
          "MaxRows": 8,
          "RowSize": 75,
          "ShowGear": false,
          "ShowBossPanel": true,
          "IdleTimeoutSec": 12,
          "BossIdleTimeoutSec": 40,
          "Language": "ru",
          "DownloadIcons": true,
          "CheckUpdates": true,
          "AutoInstallUpdates": false,
          "SkippedUpdate": "1.1.0",
          "SaveHistory": true,
          "HistoryMode": "All",
          "HistoryMinSeconds": 30,
          "RecordPackets": false,
          "CaptureDevice": "\\Device\\NPF_{ABC}",
          "HotkeyToggleOverlay": "Ctrl+Alt+M",
          "HotkeyReset": "Ctrl+Shift+R",
          "HotkeyClickThrough": "Ctrl+Shift+L",
          "HotkeyTimers": "Ctrl+Shift+T",
          "LayoutVersion": 4
        }
        """;

    [Fact]
    public void Defaults_match_the_windows_app()
    {
        var s = new UserSettings();

        Assert.Equal("en", s.Language);
        Assert.Null(s.NamesLanguage);
        Assert.Equal("en", s.EffectiveNamesLanguage);
        Assert.Equal(TargetMode.BossOnly, s.TargetMode);
        Assert.Equal(10, s.MaxRows);
        Assert.Equal(100, s.RowSize);
        Assert.Equal(HistoryMode.BossesAndLong, s.HistoryMode);
        Assert.Equal("Ctrl+Shift+D", s.HotkeyToggleOverlay);
        Assert.Equal(0.55, s.LockedOpacity);
    }

    [Fact]
    public void A_settings_file_of_the_windows_app_reads_as_is()
    {
        var s = UserSettings.Parse(WpfSettings);

        Assert.Equal((1520.0, 210.0, 470.0, 540.0), (s.OverlayLeft, s.OverlayTop, s.OverlayWidth, s.OverlayHeight));
        Assert.Equal(0.8, s.BackgroundOpacity);
        Assert.True(s.Locked);
        Assert.Equal(90, s.AutoHideSeconds);
        Assert.Equal(5, s.BossAlertMinutes);
        Assert.Equal(TargetMode.All, s.TargetMode);
        Assert.False(s.BarsRelativeToTop);
        Assert.Equal((8, 75), (s.MaxRows, s.RowSize));
        Assert.False(s.ShowGear);
        Assert.Equal((12, 40), (s.IdleTimeoutSec, s.BossIdleTimeoutSec));
        Assert.Equal("ru", s.Language);
        Assert.False(s.AutoInstallUpdates);
        Assert.Equal("1.1.0", s.SkippedUpdate);
        Assert.Equal((HistoryMode.All, 30), (s.HistoryMode, s.HistoryMinSeconds));
        Assert.Equal("\\Device\\NPF_{ABC}", s.CaptureDevice);
        Assert.Equal("Ctrl+Alt+M", s.HotkeyToggleOverlay);
        Assert.Equal(0.55, s.LockedOpacity);
    }

    [Fact]
    public void Out_of_range_values_are_clamped()
    {
        var s = UserSettings.Parse("""{"MaxRows": 99, "RowSize": 10, "BackgroundOpacity": 5, "IdleTimeoutSec": 1, "BossAlertMinutes": -3, "OverlayWidth": 50}""");

        Assert.Equal(20, s.MaxRows);
        Assert.Equal(50, s.RowSize);
        Assert.Equal(1, s.BackgroundOpacity);
        Assert.Equal(3, s.IdleTimeoutSec);
        Assert.Equal(0, s.BossAlertMinutes);
        Assert.Equal(280, s.OverlayWidth);
    }

    [Fact]
    public void An_unreadable_value_falls_back_without_touching_the_others()
    {
        var s = UserSettings.Parse("""{"MaxRows": "many", "TargetMode": "Sometimes", "Language": "xx", "NamesLanguage": "../de", "Locked": true}""");

        Assert.Equal(10, s.MaxRows);
        Assert.Equal(TargetMode.BossOnly, s.TargetMode);
        Assert.Equal("en", s.Language);
        Assert.Null(s.NamesLanguage);
        Assert.True(s.Locked);
    }

    [Fact]
    public void Broken_json_gives_the_defaults()
    {
        Assert.Equal(10, UserSettings.Parse("{ not json").MaxRows);
        Assert.Equal(10, UserSettings.Parse("[1, 2]").MaxRows);
    }

    [Fact]
    public void Names_language_follows_the_interface_unless_set()
    {
        Assert.Equal("es", UserSettings.Parse("""{"Language": "es"}""").EffectiveNamesLanguage);
        Assert.Equal("de", UserSettings.Parse("""{"Language": "es", "NamesLanguage": "de"}""").EffectiveNamesLanguage);
    }

    [Fact]
    public void Files_older_than_the_language_switch_start_in_english()
    {
        Assert.Equal("en", UserSettings.Parse("""{"Language": "ru", "LayoutVersion": 3}""").Language);
    }

    [Fact]
    public void Save_and_load_round_trip_without_leaving_a_temporary_file()
    {
        var path = Path.Combine(_dir, "nested", "settings.json");
        var s = UserSettings.Parse(WpfSettings);
        s.NamesLanguage = "de";
        s.BreakdownWidth = 1200;

        s.Save(path);
        var loaded = UserSettings.Load(path);

        Assert.Equal(s.ToJson(), loaded.ToJson());
        Assert.False(File.Exists(path + ".tmp"));
        Assert.Contains("\"TargetMode\": \"All\"", File.ReadAllText(path));
    }

    [Fact]
    public void A_missing_file_loads_the_defaults()
    {
        Assert.Equal(new UserSettings().ToJson(), UserSettings.Load(Path.Combine(_dir, "none.json")).ToJson());
    }

    [Fact]
    public void Meter_options_get_the_measuring_settings()
    {
        var options = new MeterOptions();

        UserSettings.Parse("""{"TargetMode": "All", "IdleTimeoutSec": 12, "BossIdleTimeoutSec": 40}""").ApplyTo(options);

        Assert.Equal((TargetMode.All, 12_000, 40_000), (options.TargetMode, options.IdleTimeoutMs, options.BossIdleTimeoutMs));
    }
}
