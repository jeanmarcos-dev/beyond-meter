using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AionMeter.Overlay;

public static partial class ShortcutHints
{
    private const string ProgramName = "aionmeter-overlay";
    private const string KeybindingsPath = "/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/";

    public static IReadOnlyDictionary<OverlayCommand, string> Load()
    {
        var info = new ProcessStartInfo("dconf")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add("dump");
        info.ArgumentList.Add(KeybindingsPath);
        try
        {
            using var process = Process.Start(info);
            if (process is null) return Empty();
            process.StandardInput.Close();
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(2_000) || !output.Wait(500)) return Empty();
            return process.ExitCode == 0 ? Parse(output.Result) : Empty();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return Empty();
        }
    }

    public static IReadOnlyDictionary<OverlayCommand, string> Parse(string dump)
    {
        var map = new Dictionary<OverlayCommand, string>();
        string? binding = null, command = null;
        foreach (var raw in dump.Split('\n').Append("["))
        {
            var line = raw.Trim();
            if (line.StartsWith('['))
            {
                Add(map, binding, command);
                binding = command = null;
            }
            else if (line.StartsWith("binding=", StringComparison.Ordinal))
            {
                binding = Unquote(line["binding=".Length..]);
            }
            else if (line.StartsWith("command=", StringComparison.Ordinal))
            {
                command = Unquote(line["command=".Length..]);
            }
        }
        return map;
    }

    public static string Format(string accelerator)
    {
        var modifiers = Modifier().Matches(accelerator).Select(m => m.Groups[1].Value switch
        {
            "Control" or "Primary" or "Ctrl" => "Ctrl",
            var other => other,
        });
        var key = Modifier().Replace(accelerator, "");
        if (key.Length == 1) key = key.ToUpperInvariant();
        return string.Join('+', modifiers.Append(key));
    }

    public static string? Footer(IReadOnlyDictionary<OverlayCommand, string> shortcuts, bool locked)
    {
        var parts = new List<string>();
        if (shortcuts.TryGetValue(OverlayCommand.Lock, out var lockKey)) parts.Add($"{lockKey} {(locked ? "soltar" : "fijar")}");
        if (shortcuts.TryGetValue(OverlayCommand.Toggle, out var toggleKey)) parts.Add($"{toggleKey} ocultar");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static void Add(Dictionary<OverlayCommand, string> map, string? binding, string? command)
    {
        if (string.IsNullOrEmpty(binding) || command is null) return;
        var words = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2 || Path.GetFileName(words[0]) != ProgramName) return;
        if (OverlayCommands.Parse(words[1]) is { } parsed) map.TryAdd(parsed, Format(binding));
    }

    private static string Unquote(string value) => value.Trim().Trim('\'');

    private static IReadOnlyDictionary<OverlayCommand, string> Empty() => new Dictionary<OverlayCommand, string>();

    [GeneratedRegex("<([^>]+)>")]
    private static partial Regex Modifier();
}
