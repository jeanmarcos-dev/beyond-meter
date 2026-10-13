using System.Diagnostics;
using AionMeter.Overlay;
using AionMeter.Overlay.Platform;

namespace AionMeter.Tests;

public sealed class LinuxDesktopTests
{
    [Fact]
    public void Paths_follow_the_xdg_variables()
    {
        var env = new Dictionary<string, string?>
        {
            ["XDG_CONFIG_HOME"] = "/cfg",
            ["XDG_DATA_HOME"] = "/data",
            ["XDG_RUNTIME_DIR"] = "/run/user/1000",
        };
        using var desktop = new LinuxDesktop(name => env.GetValueOrDefault(name));

        Assert.Equal("/cfg/aionmeter", desktop.ConfigDirectory);
        Assert.Equal("/data/aionmeter", desktop.DataDirectory);
        Assert.Equal("/run/user/1000/aionmeter-overlay.sock", desktop.CommandChannelPath);
    }

    [Fact]
    public void Without_xdg_variables_paths_fall_back_to_the_home_folder_and_temp()
    {
        using var desktop = new LinuxDesktop(_ => "");
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Assert.Equal(Path.Combine(home, ".config", "aionmeter"), desktop.ConfigDirectory);
        Assert.Equal(Path.Combine(home, ".local", "share", "aionmeter"), desktop.DataDirectory);
        Assert.Equal(Path.Combine(Path.GetTempPath(), "aionmeter-overlay.sock"), desktop.CommandChannelPath);
    }

    [Fact]
    public void Alerts_go_to_the_desktop_notifier()
    {
        using var desktop = new LinuxDesktop(_ => null, _ => null);

        Assert.IsType<DesktopBossAlert>(desktop.Alert);
    }

    [Fact]
    public void Links_and_folders_open_with_xdg_open()
    {
        var started = new List<ProcessStartInfo>();
        using var desktop = new LinuxDesktop(_ => null, info =>
        {
            started.Add(info);
            return null;
        });

        desktop.Open("https://example.test");

        var info = Assert.Single(started);
        Assert.Equal("xdg-open", info.FileName);
        Assert.Equal(["https://example.test"], info.ArgumentList);
    }

    [Fact]
    public void Linux_leaves_global_hotkeys_to_the_desktop()
    {
        using var desktop = new LinuxDesktop(_ => null);

        Assert.Empty(desktop.RegisterHotkeys(IntPtr.Zero, new Dictionary<OverlayCommand, Core.Input.HotkeyGesture> { [OverlayCommand.Toggle] = default }, _ => { }));
    }
}
