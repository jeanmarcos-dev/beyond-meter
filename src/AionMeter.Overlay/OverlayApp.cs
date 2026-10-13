using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Demo;
using AionMeter.Core.Game;
using AionMeter.Core.Storage;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace AionMeter.Overlay;

public sealed record OverlayOptions(bool Demo, string SocketPath, bool Hidden = false, string? ReplayPath = null, bool Fast = false);

public sealed class OverlayApp : Application
{
    public static OverlayOptions Options { get; set; } = new(false, CommandChannel.DefaultPath());

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
        var dataHome = DataHome();
        var settingsPath = OverlaySettings.DefaultPath();
        var settings = OverlaySettings.Load(settingsPath);
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        var data = GameData.Load(dataDirectory, settings.Language);
        var meter = new MeterOptions();
        var tracker = new CombatTracker(data, meter);
        var replaying = options.ReplayPath is not null;
        var throwAway = options.Demo || replaying;
        var history = new HistoryStore(throwAway
            ? Directory.CreateTempSubdirectory("aionmeter-demo").FullName
            : Path.Combine(dataHome, "fights"));
        IEventSource? capture = options.ReplayPath is { } replayPath
            ? new PcapReplaySource(data, replayPath, realtime: !options.Fast)
            : options.Demo ? null : new LiveCapture(data, new LiveCaptureOptions());
        var names = throwAway ? null : new NameCache(Path.Combine(dataHome, "names-cache.json"));
        var board = BossBoard.Create(dataHome, data, dataDirectory, throwAway);
        var session = new MeterSession(tracker, history, capture, names, new HistoryPolicy(), board.Timers, Path.Combine(dataHome, "captures"), replaying);
        var tuning = new MeterTuning(meter, data, dataDirectory, session);
        tuning.Apply(settings);
        var demo = options.Demo ? new DemoFightSource(tracker.Process) : null;

        var x11 = X11Window.Open();
        var window = new OverlayWindow(session, settings, settingsPath, x11, tuning.Apply, board, options.Hidden);

        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        window.Shortcuts = ShortcutHints.Load();
        window.QuitRequested += () => desktop.Shutdown();
        var channel = CommandChannel.Listen(options.SocketPath, command => Dispatcher.UIThread.Post(() => window.Apply(command)));
        desktop.Exit += (_, _) =>
        {
            channel.Dispose();
            demo?.Dispose();
            session.Dispose();
            x11?.Dispose();
        };

        session.Start();
        demo?.Start(TimeSpan.FromSeconds(30));
        if (!options.Hidden) window.Show();
    }

    private static string DataHome()
    {
        var data = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(data))
            data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        return Path.Combine(data, "aionmeter");
    }
}
