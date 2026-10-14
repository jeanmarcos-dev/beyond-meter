using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Overlay;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class SettingsWindowTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("settings-window").FullName;
    private readonly List<string> _opened = [];

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    [AvaloniaFact]
    public void Saving_measure_everything_counts_mobs_at_once_and_writes_the_file()
    {
        var (window, meter) = Open();
        var dialog = OpenSettings(window);

        dialog.Mode.SelectedIndex = 1;
        dialog.Save();

        Assert.Equal(TargetMode.All, meter.TargetMode);
        Assert.Equal(TargetMode.All, UserSettings.Load(SettingsPath).TargetMode);
        Assert.Null(window.SettingsDialog);
    }

    [AvaloniaFact]
    public void Cancelling_changes_nothing_and_leaves_no_file()
    {
        var (window, meter) = Open();
        var dialog = OpenSettings(window);

        dialog.Mode.SelectedIndex = 1;
        dialog.BackgroundOpacity.Value = 0.2;
        dialog.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(TargetMode.BossOnly, meter.TargetMode);
        Assert.Equal(TargetMode.BossOnly, window.Settings.TargetMode);
        Assert.False(File.Exists(SettingsPath));
        Assert.Null(window.SettingsDialog);
    }

    [AvaloniaFact]
    public void Opacity_and_rows_apply_without_restarting()
    {
        var (window, _) = Open();
        var dialog = OpenSettings(window);

        dialog.BackgroundOpacity.Value = 1;
        dialog.MaxRows.SelectedIndex = 0;
        dialog.Save();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal((byte)255, ((ISolidColorBrush)((Border)window.Content!).Background!).Color.A);
        Assert.Equal(5, window.GetLogicalDescendants().OfType<RowControl>().Count());
    }

    [AvaloniaFact]
    public void The_ping_toggle_starts_on_and_turns_the_ping_off()
    {
        var (window, _) = Open();
        var dialog = OpenSettings(window);
        Assert.True(dialog.ShowPing.IsChecked);

        dialog.ShowPing.IsChecked = false;

        Assert.False(dialog.Collect().ShowPing);
    }

    [AvaloniaFact]
    public void Numbers_out_of_range_are_clamped_and_unreadable_ones_keep_their_value()
    {
        var (window, _) = Open();
        var dialog = OpenSettings(window);

        dialog.Idle.Text = "999";
        dialog.BossIdle.Text = "soon";
        dialog.HistoryMin.Text = "1";
        var saved = dialog.Collect();

        Assert.Equal(120, saved.IdleTimeoutSec);
        Assert.Equal(30, saved.BossIdleTimeoutSec);
        Assert.Equal(5, saved.HistoryMinSeconds);
    }

    [AvaloniaFact]
    public void The_settings_command_opens_one_window_with_the_eight_sections()
    {
        var (window, _) = Open();
        var dialog = OpenSettings(window);

        window.Apply(OverlayCommand.Settings);

        Assert.Same(dialog, window.SettingsDialog);
        var t = Core.Text.UiText.Current;
        var headings = dialog.GetLogicalDescendants().OfType<TextBlock>().Select(b => b.Text).ToList();
        Assert.All(new[] { t.SecLanguage, t.SecOverlay, t.SecMeter, t.SecHistory, t.SecCapture, t.SecHotkeys, t.SecUpdates, t.SecData }, s => Assert.Contains(s, headings));
    }

    [AvaloniaFact]
    public void The_network_adapter_and_hotkey_fields_show_only_on_windows()
    {
        var (window, _) = Open();
        var linux = OpenSettings(window);
        Assert.DoesNotContain(linux.Device, linux.GetLogicalDescendants());
        Assert.DoesNotContain(linux.HotkeyToggle, linux.GetLogicalDescendants());
        Assert.Contains(Core.Text.UiText.Current.OvHotkeysDesktop, linux.GetLogicalDescendants().OfType<TextBlock>().Select(b => b.Text));
        linux.Close();

        var windows = new SettingsWindow(new UserSettings { CaptureDevice = "vpn" }, Context(isWindows: true));
        windows.Show();

        Assert.Contains(windows.Device, windows.GetLogicalDescendants());
        Assert.Contains(windows.HotkeyToggle, windows.GetLogicalDescendants());
        Assert.Equal(2, windows.Device.SelectedIndex);
        windows.Device.SelectedIndex = 0;
        Assert.Null(windows.Collect().CaptureDevice);
    }

    [AvaloniaFact]
    public void Names_can_follow_the_interface_or_keep_their_own_language()
    {
        var dialog = new SettingsWindow(new UserSettings { NamesLanguage = "ko" }, Context());
        dialog.Show();

        Assert.Equal(2, dialog.NamesBox.SelectedIndex);
        dialog.NamesBox.SelectedIndex = 0;
        Assert.Null(dialog.Collect().NamesLanguage);
        dialog.NamesBox.SelectedIndex = 1;
        Assert.Equal("en", dialog.Collect().NamesLanguage);
    }

    [AvaloniaFact]
    public void Folder_buttons_open_through_the_desktop()
    {
        var dialog = new SettingsWindow(new UserSettings(), Context());
        dialog.Show();
        var t = Core.Text.UiText.Current;

        foreach (var text in new[] { t.OpenDataFolder, t.OpenAppFolder, t.Logs })
            dialog.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, text)).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(["data-dir", "app-dir", "logs-dir"], _opened);
    }

    [Fact]
    public void A_new_adapter_replaces_the_capture_and_keeps_recording_and_hotkeys_rebind_only_when_changed()
    {
        var first = new FakeCapture();
        var session = new MeterSession(new CombatTracker(GameData.Empty, new MeterOptions()), new HistoryStore(Path.Combine(_dir, "fights")), first, null, new HistoryPolicy(), capturesDirectory: _dir);
        session.Start();
        session.ToggleRecording();
        FakeCapture? second = null;
        var rebinds = 0;

        SettingsEffects.Apply(new UserSettings(), new UserSettings { CaptureDevice = "vpn" }, session, device => second = new FakeCapture(device), () => rebinds++);

        Assert.True(first.Disposed);
        Assert.Equal("vpn", second!.Device);
        Assert.True(second.Started);
        Assert.True(session.IsRecording);
        Assert.Equal(0, rebinds);

        SettingsEffects.Apply(new UserSettings(), new UserSettings { HotkeyReset = "Alt+F9" }, session, null, () => rebinds++);
        Assert.Equal(1, rebinds);
    }

    [Fact]
    public void Turning_recording_on_or_off_in_settings_follows_the_session()
    {
        var capture = new FakeCapture();
        var session = new MeterSession(new CombatTracker(GameData.Empty, new MeterOptions()), new HistoryStore(Path.Combine(_dir, "fights")), capture, null, new HistoryPolicy(), capturesDirectory: _dir);

        SettingsEffects.Apply(new UserSettings(), new UserSettings { RecordPackets = true }, session, null, () => { });
        Assert.True(session.IsRecording);

        SettingsEffects.Apply(new UserSettings { RecordPackets = true }, new UserSettings(), session, null, () => { });
        Assert.False(session.IsRecording);
    }

    [Fact]
    public void Names_languages_come_from_the_data_folder()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "npcs"));
        foreach (var code in new[] { "ru", "en", "ko" }) File.WriteAllText(Path.Combine(_dir, "npcs", code + ".json"), "{}");

        Assert.Equal(["en", "ko", "ru"], SettingsContexts.NamesLanguages(_dir));
        Assert.Empty(SettingsContexts.NamesLanguages(Path.Combine(_dir, "missing")));
    }

    [Fact]
    public void Without_npcap_the_adapter_list_says_so()
    {
        var t = Core.Text.UiText.Current;

        var devices = SettingsContexts.Devices(() => throw new DllNotFoundException("wpcap"));

        Assert.Equal([new CaptureDevice(null, t.AdapterAuto), new CaptureDevice(null, t.NpcapMissing)], devices);
        Assert.Equal([new CaptureDevice(null, t.AdapterAuto), new CaptureDevice("eth0", "Ethernet")], SettingsContexts.Devices(() => [("eth0", "Ethernet")]));
    }

    private SettingsContext Context(bool isWindows = false) =>
        new(isWindows, ["en", "ko"], [new(null, "Auto"), new("eth0", "Ethernet"), new("vpn", "VPN")], "0 fights", "Version 1.0.0", true, _opened.Add, "data-dir", "app-dir", "logs-dir");

    private (OverlayWindow Window, MeterOptions Meter) Open()
    {
        var meter = new MeterOptions();
        var tracker = new CombatTracker(GameData.Empty, meter);
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        var tuning = new MeterTuning(meter, GameData.Empty, _dir, session);
        var window = new OverlayWindow(session, new UserSettings(), SettingsPath, null, tuning.Apply) { SettingsSource = () => Context() };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, meter);
    }

    private static SettingsWindow OpenSettings(OverlayWindow window)
    {
        window.Apply(OverlayCommand.Settings);
        Dispatcher.UIThread.RunJobs();
        return window.SettingsDialog!;
    }

    private sealed class FakeCapture(string? device = null) : IEventSource, IRecordable
    {
        public string? Device => device;

        public bool Started { get; private set; }

        public bool Disposed { get; private set; }

        public event Action<GameEvent>? EventDecoded
        {
            add { }
            remove { }
        }

        public event Action<CaptureStatus>? StatusChanged
        {
            add { }
            remove { }
        }

        public CaptureStatus Status => CaptureStatus.Stopped;

        public bool IsRecording { get; private set; }

        public string? RecordingPath => null;

        public void Start() => Started = true;

        public void Stop()
        {
        }

        public void StartRecording(string directory) => IsRecording = true;

        public void StopRecording() => IsRecording = false;

        public void Dispose() => Disposed = true;
    }
}
