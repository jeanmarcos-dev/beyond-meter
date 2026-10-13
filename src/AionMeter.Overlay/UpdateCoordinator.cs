using System.Globalization;
using AionMeter.Core.Settings;
using AionMeter.Core.Text;
using AionMeter.Core.Updates;
using AionMeter.Overlay.Platform;

namespace AionMeter.Overlay;

public sealed record UpdateMoment(bool Fighting, bool? GameRunning);

public sealed class UpdateCoordinator : IDisposable
{
    public static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(20);
    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(2);
    public static readonly TimeSpan QuietAfterStart = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan QuietWhilePlaying = TimeSpan.FromMinutes(10);

    private readonly IDesktopPlatform _desktop;
    private readonly Func<UserSettings> _settings;
    private readonly Func<long> _clock;
    private readonly long _startedAt;
    private long _nextCheck;
    private long _lastEngaged;
    private bool _checkingByHand;
    private bool _installing;
    private Version? _announced;
    private Version? _installFailed;

    public UpdateCoordinator(UpdateService service, IDesktopPlatform desktop, Func<UserSettings> settings, Func<long> clock)
    {
        Service = service;
        _desktop = desktop;
        _settings = settings;
        _clock = clock;
        _startedAt = _lastEngaged = clock();
        _nextCheck = _startedAt + (long)FirstCheckDelay.TotalMilliseconds;
        Service.Changed += OnServiceChanged;
    }

    public UpdateService Service { get; }

    public bool CanInstall => _desktop.IsInstalledCopy;

    public event Action? Changed;

    public event Action<ReleaseInfo>? WindowRequested;

    public event Action? ShutdownRequested;

    public static UpdatePolicy Policy(Func<UserSettings> settings, Action save, bool installed) => new(
        () => settings().SkippedUpdate,
        version =>
        {
            settings().SkippedUpdate = version;
            save();
        },
        () => settings().AutoInstallUpdates,
        installed);

    public string? Banner()
    {
        if (Service.Available is not { } release) return null;
        var t = UiText.Current;
        var version = release.Version.ToString(3);
        return Service.AutoInstall && Service.Downloaded?.Version == release.Version
            ? string.Format(CultureInfo.InvariantCulture, t.UpdateBannerReady, version)
            : string.Format(CultureInfo.InvariantCulture, t.UpdateBanner, version);
    }

    public string? TrayEntry() =>
        Service.Available is { } release ? string.Format(CultureInfo.InvariantCulture, UiText.Current.TrayUpdate, release.Version.ToString(3)) : null;

    public string VersionText()
    {
        var t = UiText.Current;
        var format = CanInstall ? t.VersionInstalled : OperatingSystem.IsWindows() ? t.VersionPortable : t.OvVersion;
        return string.Format(CultureInfo.InvariantCulture, format, Service.Current.ToString(3));
    }

    public void OpenBanner()
    {
        if (Service.Available is not { } release) return;
        if (Service.AutoInstall && Service.Downloaded?.Version == release.Version) InstallNow();
        else WindowRequested?.Invoke(release);
    }

    public async Task<string> CheckNowAsync()
    {
        var t = UiText.Current;
        UpdateCheckResult result;
        _checkingByHand = true;
        try
        {
            result = await Service.CheckAsync();
        }
        finally
        {
            _checkingByHand = false;
        }
        if (result == UpdateCheckResult.Available && Service.Latest is { } latest) WindowRequested?.Invoke(latest);
        return result switch
        {
            UpdateCheckResult.Available => string.Format(CultureInfo.InvariantCulture, t.UpdateFound, Service.Latest!.Version.ToString(3)),
            UpdateCheckResult.UpToDate => t.UpToDate,
            _ => string.Format(CultureInfo.InvariantCulture, t.UpdateCheckFailed, Service.LastError),
        };
    }

    public async Task CheckFromTrayAsync()
    {
        var text = await CheckNowAsync();
        if (Service.Available is null) _desktop.Notice.Notify(AppInfo.Name, text);
    }

    public void Tick(UpdateMoment moment)
    {
        var now = _clock();
        if (moment.Fighting) _lastEngaged = now;
        if (now >= _nextCheck)
        {
            _nextCheck = now + (long)CheckInterval.TotalMilliseconds;
            if (_settings().CheckUpdates) _ = Service.CheckAsync();
        }
        if (QuietEnough(moment, now)) InstallNow();
    }

    public bool InstallNow()
    {
        if (_installing || Service.Downloaded is not { } release || Service.DownloadedPath is not { } path) return false;
        _installing = true;
        try
        {
            _desktop.LaunchInstaller(path, quiet: true);
            ShutdownRequested?.Invoke();
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or NotSupportedException
                                       or System.ComponentModel.Win32Exception)
        {
            _installing = false;
            _installFailed = release.Version;
            return false;
        }
    }

    public void AnnounceUpdated() =>
        _desktop.Notice.Notify(AppInfo.Name, string.Format(CultureInfo.InvariantCulture, UiText.Current.UpdatedBalloon, Service.Current.ToString(3)));

    public void Dispose()
    {
        Service.Changed -= OnServiceChanged;
        Service.Dispose();
    }

    private bool QuietEnough(UpdateMoment moment, long now)
    {
        if (_installing || !Service.AutoInstall || Service.Downloaded is not { } ready || Service.Available?.Version != ready.Version) return false;
        if (ready.Version == _installFailed || moment.Fighting) return false;
        var quiet = now - Math.Max(_lastEngaged, _startedAt);
        if (quiet < QuietAfterStart.TotalMilliseconds) return false;
        return moment.GameRunning != true || quiet >= QuietWhilePlaying.TotalMilliseconds;
    }

    private void OnServiceChanged()
    {
        Announce();
        Changed?.Invoke();
    }

    private void Announce()
    {
        if (Service.Available is not { } available || available.Version == _announced) return;
        var t = UiText.Current;
        var version = available.Version.ToString(3);
        if (Service.AutoInstall && !Service.DownloadFailed)
        {
            if (Service.Downloaded?.Version != available.Version) return;
            _announced = available.Version;
            if (!_checkingByHand) _desktop.Notice.Notify(t.UpdateTitle, string.Format(CultureInfo.InvariantCulture, t.UpdateReadyBalloon, version));
            return;
        }
        _announced = available.Version;
        if (!_checkingByHand) _desktop.Notice.Notify(t.UpdateTitle, string.Format(CultureInfo.InvariantCulture, t.UpdateBalloon, version));
    }
}
