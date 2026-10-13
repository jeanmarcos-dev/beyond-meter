using System.Collections.Concurrent;
using System.Text.Json;

namespace AionMeter.Core.Assets;

public sealed record RemoteImageOptions(
    string BaseUrl,
    string CacheDirectory,
    string Extension,
    string? RequiredMediaType = null,
    TimeSpan? RememberMisses = null,
    string? UserAgent = null);

public sealed class RemoteImageCache : IDisposable
{
    private static readonly TimeSpan StaleDownload = TimeSpan.FromMinutes(5);

    private readonly RemoteImageOptions _options;
    private readonly HttpClient _http;
    private readonly Action<string> _log;
    private readonly string _missesFile;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _misses = new();
    private readonly ConcurrentDictionary<string, byte> _pending = new();
    private readonly object _missesLock = new();

    public RemoteImageCache(RemoteImageOptions options, HttpMessageHandler? handler = null, Action<string>? log = null)
    {
        _options = options;
        _log = log ?? (_ => { });
        _http = new HttpClient(handler ?? new HttpClientHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        if (options.UserAgent is { } agent) _http.DefaultRequestHeaders.UserAgent.ParseAdd(agent);
        Directory.CreateDirectory(options.CacheDirectory);
        _missesFile = Path.Combine(options.CacheDirectory, "missing.json");
        if (options.RememberMisses is not null) LoadMisses();
        RemoveStaleDownloads();
    }

    public bool Enabled { get; set; } = true;

    public bool Busy => !_pending.IsEmpty;

    public event Action<string>? Ready;

    public string PathOf(string name) => Path.Combine(_options.CacheDirectory, name.Replace('/', '_') + _options.Extension);

    public string? Get(string name)
    {
        var path = PathOf(name);
        if (File.Exists(path)) return path;
        if (!Enabled || IsKnownMiss(name)) return null;
        if (_pending.TryAdd(name, 0)) _ = DownloadAsync(name, path);
        return null;
    }

    public async Task<string?> DownloadAsync(string name, string path)
    {
        try
        {
            using var response = await _http.GetAsync(_options.BaseUrl + name + _options.Extension).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode ||
                (_options.RequiredMediaType is { } type && response.Content.Headers.ContentType?.MediaType != type))
            {
                RememberMiss(name);
                return null;
            }
            var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            var tmp = $"{path}.{Guid.NewGuid():N}.tmp";
            await File.WriteAllBytesAsync(tmp, bytes).ConfigureAwait(false);
            try
            {
                File.Move(tmp, path, overwrite: true);
            }
            catch (IOException) when (File.Exists(path))
            {
                File.Delete(tmp);
            }
            Ready?.Invoke(name);
            return path;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
        {
            _log($"{name}{_options.Extension} not downloaded: {ex.Message}");
            return null;
        }
        finally
        {
            _pending.TryRemove(name, out _);
        }
    }

    private bool IsKnownMiss(string name) =>
        _misses.TryGetValue(name, out var missed) &&
        (_options.RememberMisses is not { } keep || DateTimeOffset.UtcNow - missed < keep);

    private void RememberMiss(string name)
    {
        _misses[name] = DateTimeOffset.UtcNow;
        if (_options.RememberMisses is null) return;
        try
        {
            lock (_missesLock)
                File.WriteAllText(_missesFile, JsonSerializer.Serialize(_misses.ToDictionary(m => m.Key, m => m.Value)));
        }
        catch (IOException)
        {
        }
    }

    private void LoadMisses()
    {
        try
        {
            if (!File.Exists(_missesFile)) return;
            var saved = JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(File.ReadAllText(_missesFile));
            if (saved is null) return;
            foreach (var (name, when) in saved) _misses[name] = when;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
        }
    }

    private void RemoveStaleDownloads()
    {
        try
        {
            foreach (var tmp in Directory.EnumerateFiles(_options.CacheDirectory, "*.tmp"))
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(tmp) > StaleDownload)
                    File.Delete(tmp);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    public void Dispose() => _http.Dispose();
}
