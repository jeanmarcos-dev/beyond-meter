namespace AionMeter.Overlay;

public enum OverlayCommand
{
    Show,
    Toggle,
    Lock,
    Reset,
    Live,
    Older,
    Newer,
    Skills,
    Timers,
    Breakdown,
    Copy,
    CopySelf,
    Mode,
    Heal,
    Reload,
    Record,
    Settings,
    History,
    Demo,
    Quit,
}

public static class OverlayCommands
{
    public static OverlayCommand? Parse(string? text)
    {
        var word = text?.Trim() ?? "";
        if (word.Length == 0 || !word.All(char.IsAsciiLetter)) return null;
        return Enum.TryParse<OverlayCommand>(word, ignoreCase: true, out var command) ? command : null;
    }

    public static string Usage() =>
        $"Uso: aionmeter-overlay [{string.Join('|', Enum.GetValues<OverlayCommand>().Select(Name))}] [--demo] [--hidden] [--replay <archivo.pcap> [--fast]]";

    public static string Name(OverlayCommand command) => command.ToString().ToLowerInvariant();

    public static bool StartsInstance(OverlayCommand command) => command is OverlayCommand.Show or OverlayCommand.Toggle;
}
