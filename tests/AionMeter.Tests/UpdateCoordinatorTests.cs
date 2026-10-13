using AionMeter.Core.Combat;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Core.Text;
using AionMeter.Core.Updates;
using AionMeter.Overlay;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class UpdateCoordinatorTests : IDisposable
{
    private const string PageUrl = "https://e/r";
    private static readonly UpdateMoment Idle = new(false, false);

    private readonly string _dir = Directory.CreateTempSubdirectory("update-coordinator").FullName;
    private readonly FakeDesktop _desktop = new();
    private readonly UserSettings _settings = new();
    private readonly List<ReleaseInfo> _windows = [];
    private int _shutdowns;
    private long _now;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static UiText T => UiText.Current;

    private static string Format(string format, string version) => string.Format(format, version);

    [Fact]
    public async Task The_first_check_waits_twenty_seconds_and_announces_the_release_once()
    {
        var updates = Coordinator("v1.1.0");

        updates.Tick(Idle);
        Assert.Null(updates.Service.Latest);

        _now = 20_000;
        updates.Tick(Idle);
        await WaitFor(() => _desktop.Notices.Count == 1);
        Assert.Equal((T.UpdateTitle, Format(T.UpdateBalloon, "1.1.0")), _desktop.Notices.Single());
        Assert.Equal(Format(T.UpdateBanner, "1.1.0"), updates.Banner());
        Assert.Equal(Format(T.TrayUpdate, "1.1.0"), updates.TrayEntry());

        await updates.CheckNowAsync();
        Assert.Single(_desktop.Notices);
    }

    [Fact]
    public async Task Nothing_is_checked_while_checking_is_off()
    {
        _settings.CheckUpdates = false;
        var updates = Coordinator("v1.1.0");

        _now = 60_000;
        updates.Tick(Idle);
        await Task.Delay(50);

        Assert.Null(updates.Service.Latest);
        Assert.Null(updates.Banner());
        Assert.Null(updates.TrayEntry());
    }

    [Fact]
    public async Task An_installed_copy_installs_quietly_once_the_first_minute_has_passed()
    {
        var updates = await Ready();
        Assert.Equal((T.UpdateTitle, Format(T.UpdateReadyBalloon, "1.1.0")), _desktop.Notices.Single());
        Assert.Equal(Format(T.UpdateBannerReady, "1.1.0"), updates.Banner());

        _now = 59_000;
        updates.Tick(Idle);
        Assert.Empty(_desktop.Installers);

        _now = 60_000;
        updates.Tick(Idle);
        updates.Tick(Idle);
        Assert.Equal([(updates.Service.DownloadedPath!, true)], _desktop.Installers);
        Assert.Equal(1, _shutdowns);
    }

    [Fact]
    public async Task While_the_game_runs_the_install_waits_ten_minutes_after_the_last_fight()
    {
        var updates = await Ready();
        var fightEnds = 120_000L;

        _now = fightEnds;
        updates.Tick(new UpdateMoment(true, true));
        _now = fightEnds + (long)UpdateCoordinator.QuietWhilePlaying.TotalMilliseconds - 1;
        updates.Tick(new UpdateMoment(false, true));
        Assert.Empty(_desktop.Installers);

        _now++;
        updates.Tick(new UpdateMoment(false, true));
        Assert.Single(_desktop.Installers);
    }

    [Fact]
    public async Task The_banner_opens_the_window_or_installs_when_the_update_is_ready()
    {
        var offered = Coordinator("v1.1.0");
        await offered.Service.CheckAsync();
        offered.OpenBanner();
        Assert.Equal(new Version(1, 1, 0), _windows.Single().Version);

        var ready = await Ready();
        ready.OpenBanner();
        Assert.Single(_windows);
        Assert.Single(_desktop.Installers);
    }

    [Fact]
    public async Task Checking_by_hand_reports_the_result_and_opens_the_window_without_a_balloon()
    {
        Assert.Equal(Format(T.UpdateFound, "1.1.0"), await Coordinator("v1.1.0").CheckNowAsync());
        Assert.Single(_windows);
        Assert.Empty(_desktop.Notices);

        Assert.Equal(T.UpToDate, await Coordinator("v1.0.0").CheckNowAsync());
        var failing = Coordinator(null);
        var failed = await failing.CheckNowAsync();
        Assert.Equal(Format(T.UpdateCheckFailed, failing.Service.LastError!), failed);
        Assert.Single(_windows);
    }

    [Fact]
    public async Task Checking_from_the_tray_shows_a_notice_only_when_no_window_opens()
    {
        await Coordinator("v1.0.0").CheckFromTrayAsync();
        await Coordinator("v1.1.0").CheckFromTrayAsync();

        Assert.Equal([(AppInfo.Name, T.UpToDate)], _desktop.Notices);
        Assert.Single(_windows);
    }

    [Fact]
    public async Task Skipping_hides_the_offer_and_remembers_the_version()
    {
        var updates = Coordinator("v1.1.0");
        await updates.CheckNowAsync();

        updates.Service.Skip(updates.Service.Available!);

        Assert.Null(updates.Banner());
        Assert.Null(updates.TrayEntry());
        Assert.Equal("1.1.0", _settings.SkippedUpdate);
    }

    [Fact]
    public void The_first_start_after_an_update_names_the_new_version()
    {
        Coordinator("v1.0.0").AnnounceUpdated();

        Assert.Equal([(AppInfo.Name, Format(T.UpdatedBalloon, "1.0.0"))], _desktop.Notices);
    }

    [Fact]
    public void The_version_line_says_how_this_copy_updates()
    {
        Assert.Equal(Format(T.VersionInstalled, "1.0.0"), Coordinator("v1.0.0", installed: true).VersionText());
        Assert.Equal(Format(OperatingSystem.IsWindows() ? T.VersionPortable : T.OvVersion, "1.0.0"), Coordinator("v1.0.0").VersionText());
    }

    [AvaloniaFact]
    public async Task A_copy_that_cannot_install_opens_the_release_page()
    {
        var updates = Coordinator("v1.1.0");
        await updates.Service.CheckAsync();
        var window = new UpdateWindow(updates, updates.Service.Available!, _desktop);
        window.Show();

        Assert.Equal(T.UpdateOpenPage, window.UpdateButton.Content);
        await window.Update();

        Assert.Equal([PageUrl], _desktop.Opened);
        Assert.Empty(_desktop.Installers);
        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public async Task An_installed_copy_downloads_and_starts_the_installer_from_the_window()
    {
        _settings.AutoInstallUpdates = false;
        var updates = Coordinator("v1.1.0", installed: true);
        await updates.Service.CheckAsync();
        var window = new UpdateWindow(updates, updates.Service.Available!, _desktop);
        var installing = 0;
        window.Installing += () => installing++;
        window.Show();

        Assert.Equal(T.UpdateNow, window.UpdateButton.Content);
        await window.Update();

        var (path, quiet) = _desktop.Installers.Single();
        Assert.False(quiet);
        Assert.Equal(UpdateServiceTests.Installer, File.ReadAllBytes(path));
        Assert.Equal(1, installing);
    }

    [AvaloniaFact]
    public async Task Skip_in_the_window_closes_it_and_drops_the_offer()
    {
        var updates = Coordinator("v1.1.0");
        await updates.Service.CheckAsync();
        var window = new UpdateWindow(updates, updates.Service.Available!, _desktop);
        window.Show();

        window.SkipButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.IsVisible);
        Assert.Null(updates.Banner());
    }

    [AvaloniaFact]
    public async Task The_overlay_shows_the_banner_and_keeps_a_single_update_window()
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions());
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        var overlay = new OverlayWindow(session, _settings, Path.Combine(_dir, "settings.json"), _desktop);
        var updates = Coordinator("v1.1.0");
        overlay.Updates = updates;
        overlay.Show();
        var banner = overlay.GetLogicalDescendants().OfType<Button>().Single(b => b.Name == "UpdateBanner");
        Assert.False(banner.IsVisible);

        await updates.CheckNowAsync();
        overlay.Refresh();
        var dialog = overlay.UpdateDialog!;
        Assert.True(banner.IsVisible);
        Assert.Equal(Format(T.UpdateBanner, "1.1.0"), banner.Content);

        banner.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Same(dialog, overlay.UpdateDialog);

        dialog.Close();
        Assert.Null(overlay.UpdateDialog);
    }

    private async Task<UpdateCoordinator> Ready()
    {
        _settings.CheckUpdates = false;
        var updates = Coordinator("v1.1.0", installed: true);
        await updates.Service.CheckAsync();
        await WaitFor(() => updates.Service.Downloaded is not null && _desktop.Notices.Contains((T.UpdateTitle, Format(T.UpdateReadyBalloon, "1.1.0"))));
        return updates;
    }

    private UpdateCoordinator Coordinator(string? tag, bool installed = false)
    {
        _desktop.IsInstalledCopy = installed;
        var feed = new UpdateFeed("test", new UpdateServiceTests.GitHub(tag), "https://api.test/releases/latest");
        var policy = UpdateCoordinator.Policy(() => _settings, () => { }, installed);
        var service = new UpdateService(new Version(1, 0, 0), feed, policy, Path.Combine(_dir, "downloads", Guid.NewGuid().ToString("N")));
        var updates = new UpdateCoordinator(service, _desktop, () => _settings, () => _now);
        updates.WindowRequested += _windows.Add;
        updates.ShutdownRequested += () => _shutdowns++;
        return updates;
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 300 && !condition(); i++) await Task.Delay(10);
        Assert.True(condition());
    }
}
