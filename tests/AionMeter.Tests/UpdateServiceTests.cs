using System.Net;
using System.Security.Cryptography;
using AionMeter.Core.Updates;

namespace AionMeter.Tests;

public sealed class UpdateServiceTests : IDisposable
{
    private static readonly byte[] Installer = [7, 7, 7, 7];

    private readonly string _dir = Directory.CreateTempSubdirectory("update-service").FullName;
    private string? _skipped;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public async Task A_newer_release_is_available()
    {
        var service = Service("v1.1.0", current: "1.0.0");

        Assert.Equal(UpdateCheckResult.Available, await service.CheckAsync());
        Assert.Equal(new Version(1, 1, 0), service.Available!.Version);
    }

    [Fact]
    public async Task The_same_version_is_up_to_date()
    {
        var service = Service("v1.0.0", current: "1.0.0");

        Assert.Equal(UpdateCheckResult.UpToDate, await service.CheckAsync());
        Assert.Null(service.Available);
    }

    [Fact]
    public async Task A_skipped_version_is_not_offered_but_a_newer_one_is()
    {
        var service = Service("v1.1.0", current: "1.0.0");
        await service.CheckAsync();

        service.Skip(service.Available!);

        Assert.Equal("1.1.0", _skipped);
        Assert.Null(service.Available);
        Assert.Equal(UpdateCheckResult.Available, await service.CheckAsync());
    }

    [Fact]
    public async Task A_failed_check_reports_its_error()
    {
        var service = Service(null, current: "1.0.0");

        Assert.Equal(UpdateCheckResult.Failed, await service.CheckAsync());
        Assert.NotNull(service.LastError);
    }

    [Fact]
    public async Task An_installed_copy_downloads_the_update_by_itself()
    {
        var service = Service("v1.1.0", current: "1.0.0", installed: true);
        var changed = 0;
        service.Changed += () => changed++;

        await service.CheckAsync();
        await WaitFor(() => service.Downloaded is not null);

        Assert.Equal(new Version(1, 1, 0), service.Downloaded!.Version);
        Assert.Equal(Installer, File.ReadAllBytes(service.DownloadedPath!));
        Assert.True(changed >= 2);
    }

    [Fact]
    public async Task A_portable_copy_does_not_download_by_itself()
    {
        var service = Service("v1.1.0", current: "1.0.0", installed: false);

        await service.CheckAsync();
        await Task.Delay(100);

        Assert.Null(service.Downloaded);
    }

    [Theory]
    [InlineData(@"C:\Users\a\AppData\Local\Programs\Beyond Meter", @"C:\Users\a\AppData\Local\Programs\Beyond Meter\", true)]
    [InlineData(@"c:\users\a\appdata\local\programs\beyond meter\", @"C:\Users\a\AppData\Local\Programs\Beyond Meter", true)]
    [InlineData(@"C:\Users\a\AppData\Local\Programs\Beyond Meter", @"D:\Games\BeyondMeter", false)]
    [InlineData(null, @"C:\Games", false)]
    [InlineData("", @"C:\Games", false)]
    public void A_copy_is_installed_when_it_runs_from_the_install_location(string? location, string baseDirectory, bool expected)
    {
        Assert.Equal(expected, UpdateService.RunsFrom(location, baseDirectory));
    }

    [Fact]
    public void Old_downloads_are_cleaned_and_recent_ones_kept()
    {
        var folder = Path.Combine(_dir, "downloads");
        Directory.CreateDirectory(folder);
        var old = Path.Combine(folder, "old.exe");
        var recent = Path.Combine(folder, "recent.exe");
        File.WriteAllBytes(old, [1]);
        File.WriteAllBytes(recent, [1]);
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddHours(-2));

        UpdateService.CleanDownloads(folder);

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(recent));
    }

    private UpdateService Service(string? tag, string current, bool installed = false)
    {
        var feed = new UpdateFeed("test", new GitHub(tag), "https://api.test/releases/latest");
        var policy = new UpdatePolicy(() => _skipped, v => _skipped = v, () => true, installed);
        return new UpdateService(Version.Parse(current), feed, policy, Path.Combine(_dir, "downloads"));
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 300 && !condition(); i++) await Task.Delay(10);
        Assert.True(condition());
    }

    private sealed class GitHub(string? tag) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (tag is null) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            if (request.RequestUri!.Host == "files.test")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Installer) });
            var sha = Convert.ToHexStringLower(SHA256.HashData(Installer));
            var json = $$"""
                {"tag_name":"{{tag}}","html_url":"https://e/r","draft":false,"prerelease":false,"body":"",
                 "assets":[{"name":"BeyondMeter-Setup-{{tag}}.exe","size":{{Installer.Length}},"browser_download_url":"https://files.test/setup.exe","digest":"sha256:{{sha}}"}]}
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
