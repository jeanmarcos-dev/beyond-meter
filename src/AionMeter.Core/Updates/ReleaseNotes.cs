using System.Text.RegularExpressions;

namespace AionMeter.Core.Updates;

/// <summary>Release notes (GitHub Markdown) as plain lines for the update window.</summary>
public static class ReleaseNotes
{
    /// <summary>
    /// Keeps the words and drops the markup: wrapped lines join their paragraph or list item, headings are flagged,
    /// runs of blank lines become one. Notes written in several languages are cut down to the sections in
    /// <paramref name="language"/>: a <c>&lt;!-- lang: es --&gt;</c> line sets the language of the sections after it,
    /// and unmarked sections are "ru" when their heading has Cyrillic letters, else "en". The text before the first
    /// heading (download links, useless inside the app) is left out then.
    /// </summary>
    public static List<(string Text, bool Heading)> Lines(string markdown, string language)
    {
        var all = Parse(markdown);
        var sections = new List<List<Line>>();
        foreach (var line in all)
        {
            if (line.Heading) sections.Add([line]);
            else if (sections.Count > 0) sections[^1].Add(line);
        }
        var wanted = sections.Where(s => LanguageOf(s[0]) == language).ToList();
        if (wanted.Count == 0 || wanted.Count == sections.Count) return Plain(all); // one language only: show everything

        var lines = new List<(string Text, bool Heading)>();
        foreach (var section in wanted)
        {
            if (lines.Count > 0 && lines[^1].Text.Length > 0) lines.Add(("", false));
            lines.AddRange(Plain(section));
        }
        while (lines.Count > 0 && lines[^1].Text.Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    private sealed record Line(string Text, bool Heading, string? Language = null);

    private static readonly Regex LanguageMarker = new(@"^<!--\s*lang:\s*([A-Za-z-]+)\s*-->$");

    private static string LanguageOf(Line heading) => heading.Language ?? (IsRussian(heading.Text) ? "ru" : "en");

    private static List<(string Text, bool Heading)> Plain(IEnumerable<Line> lines) => lines.Select(l => (l.Text, l.Heading)).ToList();

    private static bool IsRussian(string text) => text.Any(c => c is >= 'А' and <= 'я' or 'Ё' or 'ё');

    private static List<Line> Parse(string markdown)
    {
        var lines = new List<Line>();
        string? language = null;
        var open = false; // the last line is a paragraph or list item that a wrapped line continues
        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                if (lines.Count > 0 && lines[^1].Text.Length > 0 && !lines[^1].Heading) lines.Add(new("", false));
                open = false;
                continue;
            }
            if (LanguageMarker.Match(line) is { Success: true } marker)
            {
                language = marker.Groups[1].Value.ToLowerInvariant();
                open = false;
                continue;
            }
            var heading = Regex.Match(line, @"^#{1,6}\s+(.*)$");
            var item = Regex.IsMatch(line, @"^([-*+]|\d+[.)])\s+");
            line = Clean(heading.Success ? heading.Groups[1].Value : line);
            if (heading.Success)
            {
                lines.Add(new(line, true, language));
                open = false;
            }
            else if (open && !item)
            {
                lines[^1] = new(lines[^1].Text + " " + line, false);
            }
            else
            {
                lines.Add(new(Regex.Replace(line, @"^[-*+]\s+", "• "), false));
                open = true;
            }
        }
        while (lines.Count > 0 && lines[^1].Text.Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    private static string Clean(string s) =>
        Regex.Replace(s, @"!?\[([^\]]*)\]\([^)]*\)", "$1").Replace("**", "").Replace("__", "").Replace("`", "");
}
