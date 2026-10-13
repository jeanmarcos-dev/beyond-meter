using System.Diagnostics;

namespace AionMeter.Tests;

public sealed class LinuxFactAttribute : FactAttribute
{
    public LinuxFactAttribute()
    {
        if (!OperatingSystem.IsLinux()) Skip = "Linux only";
    }
}

public sealed class InstallScriptTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("install-script").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Home => Path.Combine(_root, "home");

    private string Installed => Path.Combine(Home, ".local", "share", "aionmeter-overlay");

    private string Link => Path.Combine(Home, ".local", "bin", "aionmeter-overlay");

    [LinuxFact]
    public void Installs_the_overlay_and_links_the_command()
    {
        var package = Package("first");

        var result = Run(package);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("first", File.ReadAllText(Path.Combine(Installed, "aionmeter-overlay")));
        Assert.True(File.Exists(Path.Combine(Installed, "data", "npcs", "en.json")));
        Assert.Equal(Path.Combine(Installed, "aionmeter-overlay"), new FileInfo(Link).LinkTarget);
    }

    [LinuxFact]
    public void Running_it_again_replaces_the_program_and_keeps_user_files()
    {
        Assert.Equal(0, Run(Package("first")).ExitCode);
        File.WriteAllText(Path.Combine(Installed, "stale.dll"), "old");
        var settings = Path.Combine(Home, ".config", "aionmeter", "overlay.json");
        var fight = Path.Combine(Home, ".local", "share", "aionmeter", "fights", "fight.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settings)!);
        Directory.CreateDirectory(Path.GetDirectoryName(fight)!);
        File.WriteAllText(settings, "{\"Locked\":true}");
        File.WriteAllText(fight, "{}");

        var result = Run(Package("second"));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("second", File.ReadAllText(Path.Combine(Installed, "aionmeter-overlay")));
        Assert.False(File.Exists(Path.Combine(Installed, "stale.dll")));
        Assert.Equal("{\"Locked\":true}", File.ReadAllText(settings));
        Assert.True(File.Exists(fight));
    }

    [LinuxFact]
    public void Skipping_setcap_says_how_to_grant_capture_by_hand()
    {
        var result = Run(Package("first"));

        Assert.Contains("setcap cap_net_raw,cap_net_admin+eip", result.Output);
    }

    private string Package(string binary)
    {
        var package = Path.Combine(_root, "package-" + binary);
        Directory.CreateDirectory(Path.Combine(package, "app", "data", "npcs"));
        File.WriteAllText(Path.Combine(package, "app", "aionmeter-overlay"), binary);
        File.WriteAllText(Path.Combine(package, "app", "data", "npcs", "en.json"), "{}");
        File.Copy(Repository.Path("packaging", "linux", "install.sh"), Path.Combine(package, "install.sh"));
        return package;
    }

    private static (int ExitCode, string Output) Run(string package)
    {
        var info = new ProcessStartInfo("bash", [Path.Combine(package, "install.sh")])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.Environment["HOME"] = Path.Combine(Path.GetDirectoryName(package)!, "home");
        info.Environment["AIONMETER_NO_SETCAP"] = "1";
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, output.Result + error.Result);
    }
}
