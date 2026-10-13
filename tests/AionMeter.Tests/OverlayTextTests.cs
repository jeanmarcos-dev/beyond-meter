using System.Text.RegularExpressions;

namespace AionMeter.Tests;

public sealed class OverlayTextTests
{
    private static readonly Regex Literal = new(@"(\$?)@?""((?:[^""\\\n]|\\.)*)""");

    private static readonly Regex Hole = new(@"\{[^}]*\}");

    private static readonly Regex Word = new(@"[^\W\d_]{3,}");

    private static readonly Regex Link = new(@"^https?://\S+$");

    private static readonly Regex TechnicalUse = new(
        @"Console\.Error|Name = ""|Import\(|Definitions\(|Path\.Combine\(|env\(""|XElement\(|XAttribute\(|SetValue\(""|ArgumentList\.Add\(|Run\(""|ProcessStartInfo\(|avares://|CreateSubKey\(|CreateTempSubdirectory\(|ProgramName =|KeybindingsPath =|\.Length\.\.\]|StartsWith\(""|Stat\(""|EnumerateFiles\(");

    private static readonly HashSet<string> CommandLineFiles = ["Program.cs", "ReplayFile.cs", "OverlayCommand.cs", "OverlayArguments.cs"];

    private static readonly HashSet<string> NotInterfaceText =
    [
        "DPS",
        "HPS",
        "Open world",
        "AION 2 is not running",
        ".tmp",
        "ms-winsoundevent:Notification.Reminder",
        "JeanmarcosDev.BeyondMeter",
        "Control",
        "Primary",
        "Ctrl",
    ];

    [Fact]
    public void Overlay_texts_come_from_the_interface_text_table()
    {
        var root = Repository.Path("src", "AionMeter.Overlay");
        var fixedTexts = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(root, file) && !CommandLineFiles.Contains(Path.GetFileName(file)))
            .SelectMany(file => File.ReadLines(file).Select((line, i) => (file, line, number: i + 1)))
            .Where(x => !TechnicalUse.IsMatch(x.line))
            .SelectMany(x => Literal.Matches(x.line).Select(m => (x.file, x.number, text: m.Groups[2].Value, interpolated: m.Groups[1].Length > 0)))
            .Where(x => Word.IsMatch(x.interpolated ? Hole.Replace(x.text, "") : x.text) && !NotInterfaceText.Contains(x.text) && !Link.IsMatch(x.text))
            .Select(x => $"{Path.GetRelativePath(root, x.file)}:{x.number}: \"{x.text}\"")
            .ToList();

        Assert.True(fixedTexts.Count == 0, "Fixed texts in the overlay:\n" + string.Join("\n", fixedTexts));
    }

    [Fact]
    public void The_scan_catches_a_fixed_label()
    {
        Assert.Matches(Word, Literal.Match("_toolbar.Children.Add(ToolButton(\"Reiniciar\", Reset));").Groups[2].Value);
        Assert.Matches(TechnicalUse, "new TextBlock { Name = \"HistoryCount\" }");
        Assert.DoesNotMatch(TechnicalUse, "new TextBlock { Text = \"Historial\" }");
    }

    private static bool IsBuildOutput(string root, string file)
    {
        var first = Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar)[0];
        return first is "bin" or "obj";
    }
}
