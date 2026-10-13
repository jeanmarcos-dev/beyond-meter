using System.Text.RegularExpressions;

namespace AionMeter.Tests;

public sealed class InstallerTests
{
    private static readonly string Script = File.ReadAllText(Repository.Path("installer", "AION2DpsMeter.iss"));

    [Fact]
    public void Installer_offers_spanish_with_inno_setup_translation()
    {
        Assert.Contains("Name: \"es\"; MessagesFile: \"compiler:Languages\\Spanish.isl\"", Script);
    }

    [Fact]
    public void Every_installer_message_has_a_spanish_version_with_the_same_values()
    {
        var english = Messages("en");
        var spanish = Messages("es");

        Assert.Equal(english.Keys.Order(), spanish.Keys.Order());
        Assert.All(english, kv => Assert.Equal(Values(kv.Value), Values(spanish[kv.Key])));
        Assert.All(english, kv => Assert.NotEqual(kv.Value, spanish[kv.Key]));
    }

    [Fact]
    public void Shortcuts_carry_the_app_user_model_id_of_the_notifications()
    {
        var shortcuts = Section("[Icons]").Where(line => line.StartsWith("Name:", StringComparison.Ordinal)).ToList();

        Assert.Equal(2, shortcuts.Count);
        Assert.All(shortcuts, line => Assert.Contains("AppUserModelID: \"{#AppUserModelId}\"", line));
        Assert.Contains($"#define AppUserModelId \"{AionMeter.Overlay.Platform.WindowsDesktop.AppUserModelId}\"", Script);
    }

    [Theory]
    [InlineData("PresentationFramework.dll")]
    [InlineData("PresentationCore.dll")]
    [InlineData("wpfgfx_cor3.dll")]
    [InlineData("System.Windows.Forms.dll")]
    [InlineData("System.Private.Windows.Core.dll")]
    [InlineData("UIAutomationTypes.dll")]
    [InlineData("System.Xaml.dll")]
    [InlineData("es/PresentationFramework.resources.dll")]
    [InlineData("zh-Hant/WindowsBase.resources.dll")]
    public void An_update_removes_the_files_of_the_wpf_app(string file)
    {
        Assert.True(Removed(file), file);
    }

    [Theory]
    [InlineData("BeyondMeter.exe")]
    [InlineData("BeyondMeter.dll")]
    [InlineData("Avalonia.Base.dll")]
    [InlineData("libSkiaSharp.dll")]
    [InlineData("System.Windows.dll")]
    [InlineData("System.Private.CoreLib.dll")]
    [InlineData("System.Drawing.dll")]
    [InlineData("Microsoft.Win32.Registry.dll")]
    [InlineData("System.Security.Cryptography.dll")]
    [InlineData("app.ico")]
    [InlineData("data/npcs/en.json")]
    public void An_update_keeps_the_files_of_the_avalonia_app(string file)
    {
        Assert.False(Removed(file), file);
    }

    private static bool Removed(string file) =>
        Section("[InstallDelete]")
            .Select(line => Regex.Match(line, @"^Type: (files|filesandordirs); Name: ""\{app\}\\([^""]+)"""))
            .Where(m => m.Success)
            .Any(m => m.Groups[1].Value == "files"
                ? !file.Contains('/') && Regex.IsMatch(file, "^" + Regex.Escape(m.Groups[2].Value).Replace(@"\*", ".*") + "$", RegexOptions.IgnoreCase)
                : file.StartsWith(m.Groups[2].Value + "/", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> Section(string header) =>
        Script.ReplaceLineEndings("\n").Split('\n')
            .SkipWhile(line => line != header)
            .Skip(1)
            .TakeWhile(line => !line.StartsWith('['));

    private static Dictionary<string, string> Messages(string language) =>
        Regex.Matches(Script, $@"^{language}\.(\w+)=(.*)$", RegexOptions.Multiline)
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.TrimEnd('\r'));

    private static List<string> Values(string message) =>
        Regex.Matches(message, @"%\d|\{#\w+\}").Select(m => m.Value).Order().ToList();
}
