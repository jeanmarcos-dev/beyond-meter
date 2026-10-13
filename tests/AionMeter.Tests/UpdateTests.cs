using System.Net;
using System.Security.Cryptography;
using AionMeter.Core.Text;
using AionMeter.Core.Updates;

namespace AionMeter.Tests;

public class UpdateTests
{
    // Shape of GitHub's /releases/latest answer (trimmed), as the meter receives it.
    private const string Release = """
        {"tag_name":"v0.2.0","html_url":"https://github.com/o/r/releases/tag/v0.2.0","draft":false,"prerelease":false,
         "published_at":"2026-10-07T10:00:00Z","body":"## What's new\nUpdate check.",
         "assets":[
           {"name":"AION2DpsMeter-v0.2.0-source.zip","size":10,"browser_download_url":"https://e/src.zip","digest":"sha256:aa"},
           {"name":"AION2DpsMeter-Setup-v0.2.0.exe","size":52786865,"browser_download_url":"https://e/setup.exe","digest":"sha256:93AF8A"},
           {"name":"AION2DpsMeter-v0.2.0-win-x64.zip","size":65388791,"browser_download_url":"https://e/win.zip"}]}
        """;

    [Fact]
    public void Latest_release_gives_version_installer_and_digest()
    {
        var r = UpdateFeed.Parse(Release)!;
        Assert.Equal(new Version(0, 2, 0), r.Version);
        Assert.Equal("https://github.com/o/r/releases/tag/v0.2.0", r.PageUrl);
        Assert.Equal(new DateTimeOffset(2026, 10, 7, 10, 0, 0, TimeSpan.Zero), r.PublishedAt);
        Assert.Equal("AION2DpsMeter-Setup-v0.2.0.exe", r.Installer!.Name);
        Assert.Equal(52786865, r.Installer.Size);
        Assert.Equal("93af8a", r.Installer.Sha256);
        Assert.Equal("AION2DpsMeter-v0.2.0-win-x64.zip", r.Portable!.Name);
        Assert.Null(r.Portable.Sha256);
    }

    [Fact]
    public void Drafts_prereleases_and_odd_tags_are_not_updates()
    {
        Assert.Null(UpdateFeed.Parse(Release.Replace("\"prerelease\":false", "\"prerelease\":true")));
        Assert.Null(UpdateFeed.Parse(Release.Replace("\"draft\":false", "\"draft\":true")));
        Assert.Null(UpdateFeed.Parse(Release.Replace("\"v0.2.0\"", "\"nightly\"")));
    }

    [Theory]
    [InlineData("v0.2.0", "0.2.0")]
    [InlineData("0.2", "0.2.0")]
    [InlineData("v1.0.3-beta", "1.0.3")]
    [InlineData("V2", "2.0.0")]
    public void Tags_read_as_major_minor_patch(string tag, string expected) =>
        Assert.Equal(Version.Parse(expected), UpdateFeed.ParseVersion(tag));

    [Fact]
    public void Assembly_version_equals_its_release_tag()
    {
        Assert.Equal(UpdateFeed.ParseVersion("v0.1.0"), UpdateFeed.Normalize(new Version(0, 1, 0, 0)));
        Assert.True(UpdateFeed.ParseVersion("v0.10.0") > UpdateFeed.ParseVersion("v0.9.3"));
    }

    [Fact]
    public void Installer_product_name_ignores_inno_setup_padding()
    {
        // What FileVersionInfo reads from an Inno Setup 7 installer: the name padded to its placeholder's length.
        Assert.True(UpdateFeed.IsProductName("AION2 DPS Meter" + new string(' ', 45), "AION2 DPS Meter"));
        Assert.True(UpdateFeed.IsProductName("AION2 DPS Meter", "AION2 DPS Meter"));
        Assert.False(UpdateFeed.IsProductName("AION2 DPS Meter Pro", "AION2 DPS Meter"));
        Assert.False(UpdateFeed.IsProductName(null, "AION2 DPS Meter"));
    }

    [Fact]
    public async Task Downloads_are_checked_against_size_and_sha256()
    {
        var body = new byte[300_000];
        new Random(7).NextBytes(body);
        var sha = Convert.ToHexStringLower(SHA256.HashData(body));
        var dir = Path.Combine(Path.GetTempPath(), "aionmeter-update-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var feed = new UpdateFeed("test", new FixedResponse(body));
            var progress = new List<double>();
            var path = await feed.DownloadAsync(new ReleaseAsset("Setup.exe", body.Length, "https://e/Setup.exe", sha), dir,
                new SyncProgress(progress.Add));
            Assert.Equal(body, await File.ReadAllBytesAsync(path));
            Assert.Equal(1.0, progress[^1]);

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                feed.DownloadAsync(new ReleaseAsset("Tampered.exe", body.Length, "https://e/T.exe", new string('0', 64)), dir, null));
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                feed.DownloadAsync(new ReleaseAsset("Short.exe", body.Length + 1, "https://e/S.exe", null), dir, null));
            // Nothing but the good file is left behind.
            Assert.Equal(["Setup.exe"], Directory.GetFiles(dir).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task An_installer_already_downloaded_is_not_fetched_again()
    {
        var body = new byte[100_000];
        new Random(3).NextBytes(body);
        var asset = new ReleaseAsset("Setup.exe", body.Length, "https://e/Setup.exe", Convert.ToHexStringLower(SHA256.HashData(body)));
        var dir = Path.Combine(Path.GetTempPath(), "aionmeter-update-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var server = new FixedResponse(body);
            using var feed = new UpdateFeed("test", server);
            await feed.DownloadAsync(asset, dir, null); // the background download
            await feed.DownloadAsync(asset, dir, null); // the player clicks Update later
            Assert.Equal(1, server.Requests);

            File.WriteAllBytes(Path.Combine(dir, "Setup.exe"), new byte[body.Length]); // damaged on disk: fetched again
            await feed.DownloadAsync(asset, dir, null);
            Assert.Equal(2, server.Requests);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // The shape of the real 0.2.x release notes: download links, then a Russian and an English part.
    private const string Notes = """
        **Download:** [Setup](https://e/setup.exe) · [zip](https://e/app.zip)

        ## Что нового в 0.2.2

        - **Видно, что метр работает.** Оверлей появляется
          при запуске.

        **Обновление:** метр сам предложит.

        ## What's new in 0.2.2

        - **You can see the meter is running.** The overlay comes up on start.
        """;

    [Fact]
    public void Release_notes_show_the_interface_language_only()
    {
        var ru = ReleaseNotes.Lines(Notes, "ru");
        Assert.Equal(("Что нового в 0.2.2", true), ru[0]);
        Assert.Contains(ru, l => l.Text == "• Видно, что метр работает. Оверлей появляется при запуске.");
        Assert.DoesNotContain(ru, l => l.Text.Contains("Download") || l.Text.Contains("What's new"));

        var en = ReleaseNotes.Lines(Notes, "en");
        Assert.Equal([("What's new in 0.2.2", true), ("• You can see the meter is running. The overlay comes up on start.", false)], en);
    }

    [Fact]
    public void Notes_of_the_version_in_the_build_props_keep_every_section()
    {
        var version = Repository.BuildProperty("Version");
        var markdown = File.ReadAllText(Repository.Path("release-notes", version + ".md"));
        var headings = markdown.Split('\n').Where(l => l.StartsWith("## ")).Select(l => l[3..].Trim()).ToList();

        var shown = UiText.Languages.Select(language => ReleaseNotes.Lines(markdown, language)).ToList();

        Assert.NotEmpty(headings);
        Assert.All(shown, lines => Assert.True(lines[0].Heading));
        Assert.Equal(UiText.Languages.Length, shown.Select(lines => lines[0].Text).Distinct().Count());
        Assert.Equal(headings, shown.SelectMany(lines => lines.Where(l => l.Heading).Select(l => l.Text)));
    }

    private const string MarkedNotes = """
        **Download:** [Setup](https://e/setup.exe)

        <!-- lang: en -->
        ## What's new in 1.1.0

        - Spanish interface.

        <!-- lang: es -->
        ## Novedades de la 1.1.0

        - Interfaz en español.

        ### Instalar

        - Ejecuta el instalador.

        <!-- lang: ru -->
        ## Что нового в 1.1.0

        - Испанский интерфейс.
        """;

    [Theory]
    [InlineData("es", "Novedades de la 1.1.0")]
    [InlineData("en", "What's new in 1.1.0")]
    [InlineData("ru", "Что нового в 1.1.0")]
    public void Marked_notes_show_the_sections_of_the_interface_language(string language, string heading)
    {
        var lines = ReleaseNotes.Lines(MarkedNotes, language);

        Assert.Equal((heading, true), lines[0]);
        Assert.Equal(language == "es" ? 2 : 1, lines.Count(l => l.Heading));
        Assert.DoesNotContain(lines, l => l.Text.Contains("<!--") || l.Text.Contains("lang:"));
    }

    [Fact]
    public void Spanish_section_keeps_its_subsections()
    {
        var lines = ReleaseNotes.Lines(MarkedNotes, "es");

        Assert.Equal([("Novedades de la 1.1.0", true), ("• Interfaz en español.", false), ("", false), ("Instalar", true), ("• Ejecuta el instalador.", false)], lines);
    }

    [Fact]
    public void Unmarked_notes_without_spanish_are_shown_whole_in_spanish()
    {
        var whole = ReleaseNotes.Lines(Notes, "es");

        Assert.Contains(whole, l => l.Text == "Что нового в 0.2.2");
        Assert.Contains(whole, l => l.Text == "What's new in 0.2.2");
    }

    [Fact]
    public void Readme_example_of_marked_notes_shows_one_language()
    {
        var readme = File.ReadAllText(Repository.Path("README.md")).ReplaceLineEndings("\n");
        var example = System.Text.RegularExpressions.Regex.Match(readme, "```markdown\n(.*?)```", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;

        Assert.Equal(("Novedades de la 1.1.0", true), Assert.Single(ReleaseNotes.Lines(example, "es"), l => l.Heading));
        Assert.Equal(("What's new in 1.1.0", true), Assert.Single(ReleaseNotes.Lines(example, "en"), l => l.Heading));
    }

    [Fact]
    public void Markers_never_show_up_as_text()
    {
        var lines = ReleaseNotes.Lines("<!-- lang: es -->\n## Novedades\n\n- Algo.", "es");

        Assert.Equal([("Novedades", true), ("• Algo.", false)], lines);
    }

    [Fact]
    public void Updates_come_from_the_fork_releases()
    {
        Assert.Equal("https://api.github.com/repos/jeanmarcos-dev/beyond-meter/releases/latest", UpdateFeed.LatestReleaseApi);
        Assert.Equal("https://github.com/jeanmarcos-dev/beyond-meter/releases/latest", UpdateFeed.ReleasesPage);
    }

    [Fact]
    public void Only_a_beyond_meter_installer_is_accepted()
    {
        Assert.True(UpdateFeed.IsProductName("Beyond Meter" + new string(' ', 45), "Beyond Meter"));
        Assert.False(UpdateFeed.IsProductName("AION2 DPS Meter", "Beyond Meter"));
    }

    [Fact]
    public void Fork_release_files_are_recognised()
    {
        var release = Release.Replace("AION2DpsMeter-", "BeyondMeter-").Replace("v0.2.0", "v1.1.0");

        var r = UpdateFeed.Parse(release)!;

        Assert.Equal(new Version(1, 1, 0), r.Version);
        Assert.Equal("BeyondMeter-Setup-v1.1.0.exe", r.Installer!.Name);
        Assert.Equal("BeyondMeter-v1.1.0-win-x64.zip", r.Portable!.Name);
    }

    [Fact]
    public void Single_language_notes_are_shown_whole() =>
        Assert.Equal(("Fixes", true), ReleaseNotes.Lines("## Fixes\n\n- One", "ru")[0]);

    private sealed class FixedResponse(byte[] body) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
        }
    }

    private sealed class SyncProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
