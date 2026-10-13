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

    private static Dictionary<string, string> Messages(string language) =>
        Regex.Matches(Script, $@"^{language}\.(\w+)=(.*)$", RegexOptions.Multiline)
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.TrimEnd('\r'));

    private static List<string> Values(string message) =>
        Regex.Matches(message, @"%\d|\{#\w+\}").Select(m => m.Value).Order().ToList();
}
