using System.Text.Json;

namespace AionMeter.Core.Updates;

public enum UpdateCheckResult
{
    UpToDate,
    Available,
    Failed,
}

public sealed record UpdatePolicy(Func<string?> SkippedVersion, Action<string> SkipVersion, Func<bool> AutoInstall, bool Installed);

public sealed class UpdateService : IDisposable
{
    private readonly UpdateFeed _feed;
    private readonly UpdatePolicy _policy;
    private readonly string _downloadFolder;
    private readonly Action<string> _log;
    private Task<UpdateCheckResult>? _running;
    private bool _downloading;

    public UpdateService(Version current, UpdateFeed feed, UpdatePolicy policy, string downloadFolder, Action<string>? log = null)
    {
        Current = UpdateFeed.Normalize(current);
        _feed = feed;
        _policy = policy;
        _downloadFolder = downloadFolder;
        _log = log ?? (_ => { });
    }

    public Version Current { get; }

    public ReleaseInfo? Latest { get; private set; }

    public string? LastError { get; private set; }

    public ReleaseInfo? Available =>
        Latest is { } r && r.Version > Current && r.Version.ToString(3) != _policy.SkippedVersion() ? r : null;

    public bool Installed => _policy.Installed;

    public bool AutoInstall => _policy.Installed && _policy.AutoInstall();

    public ReleaseInfo? Downloaded { get; private set; }

    public string? DownloadedPath { get; private set; }

    public bool DownloadFailed { get; private set; }

    public event Action? Changed;

    public async Task<UpdateCheckResult> CheckAsync()
    {
        if (_running is { } running) return await running;
        _running = RunCheckAsync();
        try
        {
            return await _running;
        }
        finally
        {
            _running = null;
        }
    }

    public void Skip(ReleaseInfo release)
    {
        _policy.SkipVersion(release.Version.ToString(3));
        Changed?.Invoke();
    }

    public Task<string> DownloadInstallerAsync(ReleaseInfo release, IProgress<double>? progress, CancellationToken ct) =>
        release.Installer is { } installer
            ? _feed.DownloadAsync(installer, _downloadFolder, progress, ct)
            : throw new InvalidOperationException($"Release {release.Tag} has no installer");

    public void Preview(ReleaseInfo release) => Latest = release;

    public static bool RunsFrom(string? installLocation, string baseDirectory)
    {
        if (string.IsNullOrEmpty(installLocation)) return false;
        static string Normal(string path) => path.Replace('/', '\\').TrimEnd('\\');
        return string.Equals(Normal(installLocation), Normal(baseDirectory), StringComparison.OrdinalIgnoreCase);
    }

    public static void CleanDownloads(string folder)
    {
        try
        {
            if (!Directory.Exists(folder)) return;
            foreach (var file in Directory.EnumerateFiles(folder))
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > TimeSpan.FromHours(1))
                    File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private async Task<UpdateCheckResult> RunCheckAsync()
    {
        try
        {
            Latest = await _feed.GetLatestAsync();
            LastError = null;
            _log($"Update check: newest release {Latest?.Tag ?? "none"}, this copy {Current.ToString(3)}.");
            if (Available is { Installer: not null } update && AutoInstall) _ = PrepareAsync(update);
            return Latest is { } r && r.Version > Current ? UpdateCheckResult.Available : UpdateCheckResult.UpToDate;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            LastError = ex is TaskCanceledException ? "timeout" : ex.Message;
            _log($"Update check failed: {LastError}");
            return UpdateCheckResult.Failed;
        }
        finally
        {
            Changed?.Invoke();
        }
    }

    private async Task PrepareAsync(ReleaseInfo release)
    {
        if (_downloading || Downloaded?.Version == release.Version) return;
        _downloading = true;
        DownloadFailed = false;
        try
        {
            DownloadedPath = await DownloadInstallerAsync(release, null, CancellationToken.None);
            Downloaded = release;
            _log($"Update {release.Tag} downloaded: {DownloadedPath}");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or InvalidOperationException
                                       or OperationCanceledException)
        {
            DownloadFailed = true;
            _log($"Update {release.Tag} not downloaded: {ex.Message}");
        }
        finally
        {
            _downloading = false;
            Changed?.Invoke();
        }
    }

    public void Dispose() => _feed.Dispose();
}
