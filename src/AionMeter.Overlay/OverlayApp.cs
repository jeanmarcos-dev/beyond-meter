using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Demo;
using AionMeter.Core.Game;
using AionMeter.Core.Logging;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Core.Text;
using AionMeter.Core.Updates;
using AionMeter.Overlay.Platform;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace AionMeter.Overlay;

public sealed record OverlayOptions(bool Demo, string SocketPath, bool Hidden = false, string? ReplayPath = null, bool Fast = false, bool Updated = false);

public sealed class OverlayApp : Application
{
    public static IDesktopPlatform Desktop { get; set; } = DesktopPlatforms.ForThisSystem();

    public static OverlayOptions Options { get; set; } = new(false, Desktop.CommandChannelPath);

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Styles.Add(new Style(x => x.OfType<Button>())
        {
            Setters = { new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Hand)) },
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) Start(desktop);
        base.OnFrameworkInitializationCompleted();
    }

    private static void Start(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var options = Options;
        var desktopPlatform = Desktop;
        var dataHome = desktopPlatform.DataDirectory;
        var settingsPath = SettingsFile.PathIn(desktopPlatform.ConfigDirectory);
        var settings = SettingsFile.Load(desktopPlatform.ConfigDirectory, desktopPlatform.LegacySettingsPath);
        UiText.Use(settings.Language);
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        var data = GameData.Load(dataDirectory, settings.EffectiveNamesLanguage);
        var meter = new MeterOptions();
        var tracker = new CombatTracker(data, meter);
        var replaying = options.ReplayPath is not null;
        var throwAway = options.Demo || replaying;
        var history = new HistoryStore(throwAway
            ? Directory.CreateTempSubdirectory("aionmeter-demo").FullName
            : Path.Combine(dataHome, "fights"));
        IEventSource? capture = options.ReplayPath is { } replayPath
            ? new PcapReplaySource(data, replayPath, realtime: !options.Fast)
            : options.Demo ? null : NewLiveCapture(data, settings.CaptureDevice);
        var names = throwAway ? null : new NameCache(Path.Combine(dataHome, "names-cache.json"));
        var board = BossBoard.Create(dataHome, data, dataDirectory, throwAway, desktopPlatform.Alert);
        var session = new MeterSession(tracker, history, capture, names, new HistoryPolicy(), board.Timers, Path.Combine(dataHome, "captures"), replaying);
        var log = new AppLog(Path.Combine(dataHome, "logs"));
        log.Info($"{AppInfo.Name} {SettingsContexts.AppVersion()} on {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        session.Log = message => log.Error(message);
        if (capture is LiveCapture) capture.StatusChanged += status => log.Info($"Capture {status.State}: {status.Message}");
        if (!throwAway) tracker.FieldBossListed += log.FieldBossList;
        var tuning = new MeterTuning(meter, data, dataDirectory, session);
        tuning.Apply(settings);
        var demo = options.Demo ? new DemoFightSource(tracker.Process) : null;

        var window = new OverlayWindow(session, settings, settingsPath, desktopPlatform, tuning.Apply, board, OverlayStartsHidden(options, settings));

        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        void BindHotkeys(UserSettings current)
        {
            var hotkeys = HotkeyBindings.From(current);
            var taken = desktopPlatform.RegisterHotkeys(window, hotkeys, window.Apply);
            window.Shortcuts = desktopPlatform.ShortcutHints();
            if (HotkeyBindings.TakenWarning(UiText.Current, hotkeys, taken) is { } warning) desktopPlatform.Notice.Notify(AppInfo.Name, warning);
        }
        BindHotkeys(settings);
        var updates = replaying ? null : NewUpdates(window, desktopPlatform, log);
        window.Updates = updates;
        if (updates is not null)
        {
            updates.ShutdownRequested += () => window.Apply(OverlayCommand.Quit);
            updates.Changed += () => Dispatcher.UIThread.Post(window.Refresh);
            if (options.Updated) updates.AnnounceUpdated();
        }
        window.SettingsSource = () => SettingsContexts.For(desktopPlatform, history, dataDirectory, OperatingSystem.IsWindows(), LiveCapture.ListDevices, updates);
        Func<string?, IEventSource>? liveCapture = capture is LiveCapture ? device => NewLiveCapture(data, device) : null;
        window.SettingsChanged += (previous, next) => SettingsEffects.Apply(previous, next, session, liveCapture, () => BindHotkeys(next));
        var images = ImageStore.For(dataDirectory, dataHome, settings.DownloadIcons);
        window.Images = images;
        window.ReplaySource = path => new PcapReplaySource(data, path, realtime: false);
        TrayIcon.SetIcons(Current!, [TrayMenu.Create(window, desktopPlatform.Alert, updates)]);
        window.QuitRequested += () => desktop.Shutdown();
        var channel = CommandChannel.Listen(options.SocketPath, command => Dispatcher.UIThread.Post(() => window.Apply(command)));
        desktop.Exit += (_, _) =>
        {
            channel.Dispose();
            demo?.Dispose();
            session.Dispose();
            images.Dispose();
            updates?.Dispose();
            log.Info("Exit");
            desktopPlatform.Dispose();
        };

        session.Start();
        if (settings.RecordPackets && capture is LiveCapture) session.ToggleRecording();
        demo?.Start(TimeSpan.FromSeconds(30));
        if (!options.Hidden) window.Show();
        if (NpcapNotice.Needed(desktopPlatform, capture is LiveCapture)) new NpcapNotice(desktopPlatform).Show();
    }

    public static bool OverlayStartsHidden(OverlayOptions options, UserSettings settings) =>
        options.Hidden || (!settings.OverlayVisible && !settings.ShowOnStart);

    private static UpdateCoordinator NewUpdates(OverlayWindow window, IDesktopPlatform desktop, AppLog log)
    {
        var current = UpdateFeed.Normalize(typeof(OverlayApp).Assembly.GetName().Version ?? new Version(0, 0, 0));
        var downloads = Path.Combine(Path.GetTempPath(), "BeyondMeter-update");
        UpdateService.CleanDownloads(downloads);
        var policy = UpdateCoordinator.Policy(() => window.Settings, window.SaveSettings, desktop.IsInstalledCopy);
        var service = new UpdateService(current, new UpdateFeed($"BeyondMeter/{current.ToString(3)}"), policy, downloads, log.Info);
        return new UpdateCoordinator(service, desktop, () => window.Settings, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private static LiveCapture NewLiveCapture(GameData data, string? device) =>
        new(data, new LiveCaptureOptions { DeviceName = OperatingSystem.IsWindows() ? device : null });

}
