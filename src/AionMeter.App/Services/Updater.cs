using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using AionMeter.Core.Updates;
using Microsoft.Win32;

namespace AionMeter.App.Services;

/// <summary>
/// New versions of the meter. Asks GitHub shortly after start and every few hours (Settings → Updates) and raises
/// <see cref="Changed"/> for the tray, the overlay and the update window. A copy installed by the installer updates
/// itself: it downloads the new installer in the background (<see cref="Downloaded"/>), and the app runs it at a quiet
/// moment without any window; the installer replaces this copy and starts the new one. A portable copy gets the
/// download page.
/// </summary>
public sealed class Updater : IDisposable
{
    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(2);
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{AE1B2590-0A93-4500-9CB1-0F543771957A}_is1"; // AppId in installer/AION2DpsMeter.iss

    private readonly AppSettings _settings;
    private readonly UpdateService _service;
    private DispatcherTimer? _timer;

    public Updater(AppSettings settings)
    {
        _settings = settings;
        var current = UpdateFeed.Normalize(typeof(Updater).Assembly.GetName().Version ?? new Version(0, 0, 0));
        var policy = new UpdatePolicy(
            () => _settings.SkippedUpdate,
            version =>
            {
                _settings.SkippedUpdate = version;
                _settings.Save();
            },
            () => _settings.AutoInstallUpdates,
            IsInstalled);
        _service = new UpdateService(current, new UpdateFeed($"BeyondMeter/{current.ToString(3)}"), policy, DownloadFolder, Log.Info);
        _service.Changed += () => Changed?.Invoke();
    }

    /// <summary>This copy's version (major.minor.patch).</summary>
    public Version Current => _service.Current;

    /// <summary>The newest release GitHub reported, newer or not; null before the first successful check.</summary>
    public ReleaseInfo? Latest => _service.Latest;

    public string? LastError => _service.LastError;

    /// <summary>A release newer than this copy, unless the user chose to skip that version.</summary>
    public ReleaseInfo? Available => _service.Available;

    /// <summary>True when this copy runs from the folder the installer put it in (it can update itself).</summary>
    public static bool IsInstalled { get; } = DetectInstalled();

    /// <summary>This copy fetches and installs new versions by itself (installed copies, unless turned off).</summary>
    public bool AutoInstall => _service.AutoInstall;

    /// <summary>The update whose installer is downloaded and checked, waiting for a quiet moment to be installed.</summary>
    public ReleaseInfo? Downloaded => _service.Downloaded;

    /// <summary>The background download of <see cref="Available"/> failed: fall back to asking the player.</summary>
    public bool DownloadFailed => _service.DownloadFailed;

    /// <summary>Raised on the UI thread after every check and when a version is skipped.</summary>
    public event Action? Changed;

    /// <summary>Automatic checks: the first shortly after start, then every few hours, while the setting is on.</summary>
    public void Start()
    {
        if (_timer is not null) return;
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = FirstCheckDelay };
        _timer.Tick += (_, _) =>
        {
            _timer.Interval = CheckInterval;
            if (_settings.CheckUpdates) _ = CheckAsync();
        };
        _timer.Start();
    }

    /// <summary>Asks GitHub now. <see cref="UpdateCheckResult.Available"/> also for a skipped version (the user asked).</summary>
    public Task<UpdateCheckResult> CheckAsync() => _service.CheckAsync();

    /// <summary>Stop announcing this version; a newer one is announced again.</summary>
    public void Skip(ReleaseInfo release) => _service.Skip(release);

    /// <summary>Downloads the release's installer to the temp folder, checked against GitHub's size and SHA-256.</summary>
    public Task<string> DownloadInstallerAsync(ReleaseInfo release, IProgress<double>? progress, CancellationToken ct) =>
        _service.DownloadInstallerAsync(release, progress, ct);

    /// <summary>Runs the downloaded installer without any window; the caller exits right after.</summary>
    public void InstallDownloaded()
    {
        if (_service.DownloadedPath is not { } path) throw new InvalidOperationException("No update downloaded");
        LaunchInstaller(path, quiet: true);
    }

    /// <summary>
    /// Starts the downloaded installer: <paramref name="quiet"/> with no window at all (automatic updates), otherwise
    /// with a small progress window (the player clicked Update). It closes this copy if it is still running (Restart
    /// Manager), installs over it and starts the new version (<c>/RELAUNCH=1</c>, see the [Run] section of the
    /// installer). The caller exits right after.
    /// </summary>
    public static void LaunchInstaller(string path, bool quiet = false)
    {
        if (!UpdateFeed.IsProductName(FileVersionInfo.GetVersionInfo(path).ProductName, AppInfo.Name))
            throw new InvalidDataException($"{Path.GetFileName(path)} is not the {AppInfo.Name} installer");
        Process.Start(new ProcessStartInfo(path, (quiet ? "/VERYSILENT" : "/SILENT") + " /SUPPRESSMSGBOXES /NORESTART /RELAUNCH=1")
            { UseShellExecute = true });
    }

    public static void OpenReleasePage(ReleaseInfo? release)
    {
        try
        {
            Process.Start(new ProcessStartInfo(release?.PageUrl ?? UpdateFeed.ReleasesPage) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Log.Error("Could not open the release page", ex);
        }
    }

    private static string DownloadFolder => Path.Combine(Path.GetTempPath(), "BeyondMeter-update");

    /// <summary>Installers of earlier updates are no longer needed (the one running right now is locked and stays).</summary>
    public static void CleanDownloads() => UpdateService.CleanDownloads(DownloadFolder);

    /// <summary>The render tool shows the update window and the overlay's update button with a made-up release.</summary>
    internal void Preview(ReleaseInfo release) => _service.Preview(release);

    private static bool DetectInstalled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(UninstallKey);
            return UpdateService.RunsFrom(key?.GetValue("InstallLocation") as string, Path.GetFullPath(AppContext.BaseDirectory));
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _timer?.Stop();
        _service.Dispose();
    }
}
