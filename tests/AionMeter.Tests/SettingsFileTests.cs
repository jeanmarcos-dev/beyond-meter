using AionMeter.Core.Combat;
using AionMeter.Core.Settings;
using AionMeter.Overlay;
using AionMeter.Overlay.Platform;

namespace AionMeter.Tests;

public sealed class SettingsFileTests : IDisposable
{
    private const string LinuxOverlayJson = """
        {"X": 1900, "Y": 320, "Width": 512, "TargetMode": "All", "Language": "ko", "MaxRows": 8, "Opacity": 0.7, "HistoryMode": "All"}
        """;

    private readonly string _dir = Directory.CreateTempSubdirectory("settings-file").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Legacy => Path.Combine(_dir, "overlay.json");

    private string Current => Path.Combine(_dir, "settings.json");

    [Fact]
    public void A_linux_overlay_json_is_imported_once_with_the_interface_in_spanish()
    {
        File.WriteAllText(Legacy, LinuxOverlayJson);

        var settings = SettingsFile.Load(_dir, Legacy);

        Assert.Equal((1900.0, 320.0, 512.0), (settings.OverlayLeft, settings.OverlayTop, settings.OverlayWidth));
        Assert.Equal(TargetMode.All, settings.TargetMode);
        Assert.Equal("es", settings.Language);
        Assert.Equal("ko", settings.EffectiveNamesLanguage);
        Assert.Equal(8, settings.MaxRows);
        Assert.Equal(0.7, settings.BackgroundOpacity);
        Assert.Equal(HistoryMode.All, settings.HistoryMode);
        Assert.True(File.Exists(Current));
    }

    [Fact]
    public void The_old_file_is_left_untouched()
    {
        File.WriteAllText(Legacy, LinuxOverlayJson);

        SettingsFile.Load(_dir, Legacy).Save(Current);

        Assert.Equal(LinuxOverlayJson, File.ReadAllText(Legacy));
    }

    [Fact]
    public void Once_imported_the_new_file_wins()
    {
        File.WriteAllText(Legacy, LinuxOverlayJson);
        var imported = SettingsFile.Load(_dir, Legacy);
        imported.Language = "ru";
        imported.Save(Current);

        Assert.Equal("ru", SettingsFile.Load(_dir, Legacy).Language);
    }

    [Fact]
    public void A_new_linux_install_starts_in_english_with_defaults()
    {
        var settings = SettingsFile.Load(_dir, Legacy);

        Assert.Equal("en", settings.Language);
        Assert.Equal(new UserSettings().ToJson(), settings.ToJson());
        Assert.False(File.Exists(Current));
    }

    [Fact]
    public void On_windows_the_settings_of_the_wpf_app_are_read_as_they_are()
    {
        File.WriteAllText(Current, """
            {"Language": "ru", "HotkeyToggleOverlay": "Alt+F9", "HotkeyReset": "Ctrl+Alt+R", "TargetMode": "All", "LayoutVersion": 4}
            """);
        using var desktop = new WindowsDesktop(new NoWin32(), _ => new NullBossAlert(), name => name == "APPDATA" ? _dir : null);

        var settings = SettingsFile.Load(_dir, desktop.LegacySettingsPath);

        Assert.Equal("ru", settings.Language);
        Assert.Equal("Alt+F9", HotkeyBindings.From(settings)[OverlayCommand.Toggle]);
        Assert.Equal("Ctrl+Alt+R", HotkeyBindings.From(settings)[OverlayCommand.Reset]);
        Assert.Equal(TargetMode.All, settings.TargetMode);
    }

    [Fact]
    public void Linux_offers_its_old_overlay_json_and_windows_nothing()
    {
        using var linux = new LinuxDesktop(name => name == "XDG_CONFIG_HOME" ? _dir : null);
        using var windows = new WindowsDesktop(new NoWin32(), _ => new NullBossAlert(), _ => null);

        Assert.Equal(Path.Combine(_dir, "aionmeter", "overlay.json"), linux.LegacySettingsPath);
        Assert.Null(windows.LegacySettingsPath);
    }

    private sealed class NoWin32 : IWin32
    {
        public long GetExStyle(IntPtr window) => 0;

        public void SetExStyle(IntPtr window, long style)
        {
        }

        public void MakeLayerOpaque(IntPtr window)
        {
        }

        public bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key) => false;

        public void UnregisterHotKey(IntPtr window, int id)
        {
        }

        public (int X, int Y)? WindowOrigin(IntPtr window) => null;
    }
}
