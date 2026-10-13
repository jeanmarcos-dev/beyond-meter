using System.Text.RegularExpressions;

namespace AionMeter.Tests;

public sealed class IdentityTests
{
    private static readonly Regex OldName = new(@"AION2 ?DPS ?Meter", RegexOptions.IgnoreCase);

    private static readonly string[] SourceExtensions = [".cs", ".xaml", ".csproj", ".manifest", ".iss", ".ps1"];

    private static readonly (string File, string Allowed)[] Exceptions =
    [
        ("*", "AION2DpsMeter.iss"),
        ("*", "cyberbadger6969"),
        ("installer/AION2DpsMeter.iss", "#define OldAppName \"AION2 DPS Meter\""),
        ("installer/AION2DpsMeter.iss", "#define OldAppExe \"AION2DpsMeter.exe\""),
    ];

    [Fact]
    public void Windows_app_installer_and_shipped_files_never_use_the_original_name()
    {
        var offending = WindowsSources()
            .SelectMany(file => File.ReadLines(Repository.Path(file)).Select((line, i) => (file, line, number: i + 1)))
            .Where(x => OldName.IsMatch(x.line) && !IsAllowed(x.file, x.line))
            .Select(x => $"{x.file}:{x.number}: {x.line.Trim()}")
            .ToList();

        Assert.True(offending.Count == 0, "Original name found:\n" + string.Join("\n", offending));
    }

    [Fact]
    public void Installer_keeps_the_original_app_id_so_it_installs_over_it()
    {
        var iss = File.ReadAllText(Repository.Path("installer", "AION2DpsMeter.iss"));

        Assert.Contains("AppId={{AE1B2590-0A93-4500-9CB1-0F543771957A}", iss);
        Assert.Contains("#define AppName \"Beyond Meter\"", iss);
        Assert.Contains("#define AppExe \"BeyondMeter.exe\"", iss);
    }

    [Fact]
    public void App_and_installer_share_the_product_name_the_updater_checks()
    {
        var appInfo = File.ReadAllText(Repository.Path("src", "AionMeter.Core", "Text", "AppInfo.cs"));
        var iss = File.ReadAllText(Repository.Path("installer", "AION2DpsMeter.iss"));

        var appName = Regex.Match(appInfo, @"const string Name = ""([^""]+)""").Groups[1].Value;
        var installerName = Regex.Match(iss, @"#define AppName ""([^""]+)""").Groups[1].Value;

        Assert.Equal("Beyond Meter", appName);
        Assert.Equal(appName, installerName);
    }

    private static bool IsAllowed(string file, string line) =>
        Exceptions.Any(e => (e.File == "*" || e.File == file) && line.Contains(e.Allowed, StringComparison.Ordinal));

    private static IEnumerable<string> WindowsSources()
    {
        var appFiles = new[] { Repository.Path("src", "AionMeter.App"), Repository.Path("src", "AionMeter.Core", "Text") }
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            .Where(f => SourceExtensions.Contains(Path.GetExtension(f)))
            .Select(f => Path.GetRelativePath(Repository.Root, f).Replace('\\', '/'))
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/"));
        return [.. appFiles, "installer/AION2DpsMeter.iss", "build-release.ps1", "INSTALL.txt"];
    }
}
