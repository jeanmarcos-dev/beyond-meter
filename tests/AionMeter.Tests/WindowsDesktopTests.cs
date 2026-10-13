using System.Diagnostics;
using AionMeter.Overlay;
using AionMeter.Overlay.Platform;

namespace AionMeter.Tests;

public sealed class WindowsDesktopTests
{
    private static readonly IntPtr Window = new(42);

    private readonly FakeWin32 _native = new();

    private WindowsDesktop Create(Func<ProcessStartInfo, Process?>? start = null) =>
        new(_native, _ => new NullBossAlert(), name => name switch
        {
            "APPDATA" => Path.Combine("C:", "Users", "jean", "AppData", "Roaming"),
            "LOCALAPPDATA" => Path.Combine("C:", "Users", "jean", "AppData", "Local"),
            _ => null,
        }, start);

    [Fact]
    public void Settings_and_data_live_where_the_windows_app_keeps_them()
    {
        using var desktop = Create();

        Assert.Equal(Path.Combine("C:", "Users", "jean", "AppData", "Roaming", "AionMeter"), desktop.ConfigDirectory);
        Assert.Equal(desktop.ConfigDirectory, desktop.DataDirectory);
    }

    [Fact]
    public void The_command_channel_lives_in_local_app_data()
    {
        using var desktop = Create();

        Assert.Equal(Path.Combine("C:", "Users", "jean", "AppData", "Local", "BeyondMeter", "overlay.sock"), desktop.CommandChannelPath);
    }

    [Fact]
    public void The_alert_is_built_for_the_data_folder()
    {
        string? folder = null;
        var alert = new NullBossAlert();

        using var desktop = new WindowsDesktop(_native, data =>
        {
            folder = data;
            return alert;
        }, _ => null);

        Assert.Same(alert, desktop.Alert);
        Assert.Equal(desktop.DataDirectory, folder);
    }

    [Fact]
    public void Hotkeys_are_registered_without_repeat_and_shown_as_hints()
    {
        using var desktop = Create();

        var failed = desktop.RegisterHotkeys(Window, new Dictionary<OverlayCommand, string> { [OverlayCommand.Toggle] = "Ctrl+Shift+D" }, _ => { });

        Assert.Empty(failed);
        var registered = Assert.Single(_native.Hotkeys);
        Assert.Equal((Window, 0x2u | 0x4u | WindowsDesktop.NoRepeat, (uint)'D'), (registered.Value.Window, registered.Value.Modifiers, registered.Value.Key));
        Assert.Equal("Ctrl+Shift+D", desktop.ShortcutHints()[OverlayCommand.Toggle]);
    }

    [Fact]
    public void Taken_and_unreadable_hotkeys_are_reported_and_not_hinted()
    {
        _native.Taken.Add((uint)'R');
        using var desktop = Create();

        var failed = desktop.RegisterHotkeys(Window, new Dictionary<OverlayCommand, string>
        {
            [OverlayCommand.Toggle] = "Ctrl+Shift+D",
            [OverlayCommand.Reset] = "Ctrl+Shift+R",
            [OverlayCommand.Lock] = "Ctrl+Nope",
        }, _ => { });

        Assert.Equal([OverlayCommand.Reset, OverlayCommand.Lock], failed);
        Assert.Equal([OverlayCommand.Toggle], desktop.ShortcutHints().Keys);
    }

    [Fact]
    public void A_hotkey_message_runs_its_command()
    {
        using var desktop = Create();
        var pressed = new List<OverlayCommand>();
        desktop.RegisterHotkeys(Window, new Dictionary<OverlayCommand, string>
        {
            [OverlayCommand.Toggle] = "Ctrl+Shift+D",
            [OverlayCommand.Lock] = "Ctrl+Shift+L",
        }, pressed.Add);
        var lockId = _native.Hotkeys.Single(h => h.Value.Key == 'L').Key;

        Assert.True(desktop.HandleMessage(WindowsDesktop.HotkeyMessage, lockId));
        Assert.False(desktop.HandleMessage(WindowsDesktop.HotkeyMessage, 7));
        Assert.False(desktop.HandleMessage(0x0010, lockId));
        Assert.Equal([OverlayCommand.Lock], pressed);
    }

    [Fact]
    public void Registering_again_replaces_the_previous_hotkeys()
    {
        using var desktop = Create();
        desktop.RegisterHotkeys(Window, new Dictionary<OverlayCommand, string> { [OverlayCommand.Toggle] = "Ctrl+Shift+D" }, _ => { });

        desktop.RegisterHotkeys(Window, new Dictionary<OverlayCommand, string> { [OverlayCommand.Reset] = "Alt+F9" }, _ => { });

        Assert.Equal([0x78u], _native.Hotkeys.Values.Select(h => h.Key));
        Assert.Equal([OverlayCommand.Reset], desktop.ShortcutHints().Keys);
    }

    [Fact]
    public void Disposing_releases_the_hotkeys()
    {
        var desktop = Create();
        desktop.RegisterHotkeys(Window, new Dictionary<OverlayCommand, string> { [OverlayCommand.Toggle] = "Ctrl+Shift+D" }, _ => { });

        desktop.Dispose();

        Assert.Empty(_native.Hotkeys);
    }

    [Fact]
    public void Click_through_makes_the_window_a_transparent_opaque_layer_and_restores_it()
    {
        _native.Styles[Window] = 0x08000080;
        using var desktop = Create();

        desktop.SetClickThrough(Window, true);
        desktop.SetClickThrough(Window, true);

        Assert.Equal(0x08000080 | WindowsDesktop.TransparentStyle | WindowsDesktop.LayeredStyle, _native.Styles[Window]);
        Assert.Equal([Window], _native.OpaqueLayers);

        desktop.SetClickThrough(Window, false);

        Assert.Equal(0x08000080, _native.Styles[Window]);
    }

    [Fact]
    public void An_already_layered_window_keeps_its_layer_settings()
    {
        _native.Styles[Window] = WindowsDesktop.LayeredStyle;
        using var desktop = Create();

        desktop.SetClickThrough(Window, true);
        desktop.SetClickThrough(Window, false);

        Assert.Empty(_native.OpaqueLayers);
        Assert.Equal(WindowsDesktop.LayeredStyle, _native.Styles[Window]);
    }

    [Fact]
    public void Turning_off_click_through_that_was_never_on_changes_nothing()
    {
        _native.Styles[Window] = 0x80;
        using var desktop = Create();

        desktop.SetClickThrough(Window, false);

        Assert.Equal(0x80, _native.Styles[Window]);
    }

    [Fact]
    public void The_window_position_is_its_screen_origin()
    {
        _native.Origins[Window] = (-1200, 300);
        using var desktop = Create();

        Assert.Equal((-1200, 300), desktop.WindowPosition(Window));
        Assert.Null(desktop.WindowPosition(new IntPtr(7)));
    }

    [Fact]
    public void Links_and_folders_open_through_the_shell()
    {
        var started = new List<ProcessStartInfo>();
        using var desktop = Create(info =>
        {
            started.Add(info);
            return null;
        });

        desktop.Open("https://example.test");

        var info = Assert.Single(started);
        Assert.Equal("https://example.test", info.FileName);
        Assert.True(info.UseShellExecute);
    }

    private sealed class FakeWin32 : IWin32
    {
        public Dictionary<IntPtr, long> Styles { get; } = [];

        public List<IntPtr> OpaqueLayers { get; } = [];

        public Dictionary<int, (IntPtr Window, uint Modifiers, uint Key)> Hotkeys { get; } = [];

        public HashSet<uint> Taken { get; } = [];

        public Dictionary<IntPtr, (int X, int Y)> Origins { get; } = [];

        public long GetExStyle(IntPtr window) => Styles.GetValueOrDefault(window);

        public void SetExStyle(IntPtr window, long style) => Styles[window] = style;

        public void MakeLayerOpaque(IntPtr window) => OpaqueLayers.Add(window);

        public bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key)
        {
            if (Taken.Contains(key)) return false;
            Hotkeys[id] = (window, modifiers, key);
            return true;
        }

        public void UnregisterHotKey(IntPtr window, int id) => Hotkeys.Remove(id);

        public (int X, int Y)? WindowOrigin(IntPtr window) => Origins.TryGetValue(window, out var origin) ? origin : null;
    }
}
