using System.Text.Json;
using System.Text.RegularExpressions;
using AionMeter.Core.Settings;

namespace AionMeter.Tests;

public sealed class LinuxDocsTests
{
    private static readonly Regex Code = new("`([^`]*)`");

    [Fact]
    public void The_documented_preferences_are_the_keys_of_the_settings_file_with_their_defaults()
    {
        var documented = DocumentedDefaults();
        using var json = JsonDocument.Parse(new UserSettings().ToJson());
        var actual = json.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => Plain(p.Value));

        Assert.Equal(actual.Keys.Order(), documented.Keys.Order());
        Assert.All(actual, kv => Assert.Equal(kv.Value, documented[kv.Key]));
    }

    [Fact]
    public void The_documented_commands_are_the_overlay_commands()
    {
        var doc = File.ReadAllText(Repository.Path("docs", "linux-overlay.md"));
        var commands = Section(doc, "## Commands")
            .Where(line => line.StartsWith("| `", StringComparison.Ordinal))
            .SelectMany(line => Code.Matches(line.Split('|')[1]).Select(m => m.Groups[1].Value))
            .ToHashSet();

        Assert.All(Enum.GetValues<AionMeter.Overlay.OverlayCommand>(), c => Assert.Contains(AionMeter.Overlay.OverlayCommands.Name(c), commands));
    }

    private static Dictionary<string, string> DocumentedDefaults()
    {
        var doc = File.ReadAllText(Repository.Path("docs", "linux-overlay.md"));
        var result = new Dictionary<string, string>();
        foreach (var line in Section(doc, "## Preferences").Where(l => l.StartsWith("| `", StringComparison.Ordinal)))
        {
            var cells = line.Split('|');
            var keys = Code.Matches(cells[1]).Select(m => m.Groups[1].Value).ToList();
            var defaults = Code.Matches(cells[2]).Select(m => m.Groups[1].Value).ToList();
            Assert.Equal(keys.Count, defaults.Count);
            for (var i = 0; i < keys.Count; i++) result[keys[i]] = defaults[i];
        }
        return result;
    }

    private static IEnumerable<string> Section(string doc, string heading)
    {
        var start = doc.IndexOf(heading + "\n", StringComparison.Ordinal);
        Assert.True(start >= 0, heading);
        var end = doc.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return doc[start..(end < 0 ? doc.Length : end)].ReplaceLineEndings("\n").Split('\n');
    }

    private static string Plain(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!,
        JsonValueKind.Null => "null",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => value.GetRawText(),
    };
}
