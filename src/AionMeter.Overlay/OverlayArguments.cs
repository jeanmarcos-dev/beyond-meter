namespace AionMeter.Overlay;

public sealed record OverlayArguments(OverlayCommand Command, bool Demo, bool Hidden, string? ReplayPath, bool Fast, string? Error)
{
    public static OverlayArguments Parse(IReadOnlyList<string> args)
    {
        string? word = null;
        string? replay = null;
        bool demo = false, hidden = false, fast = false, replayFlag = false;
        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--demo":
                    demo = true;
                    break;
                case "--hidden":
                    hidden = true;
                    break;
                case "--fast":
                    fast = true;
                    break;
                case "--replay":
                    replayFlag = true;
                    if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal)) replay = args[++i];
                    break;
                case var other when !other.StartsWith("--", StringComparison.Ordinal):
                    word ??= other;
                    break;
            }
        }

        var command = word is null ? OverlayCommand.Show : OverlayCommands.Parse(word);
        if (command is null) return Fail(OverlayCommands.Usage());
        if (replayFlag && replay is null) return Fail("--replay necesita la ruta de una grabación .pcap.");
        if (fast && !replayFlag) return Fail("--fast solo se usa junto con --replay.");
        if (replayFlag && demo) return Fail("--replay y --demo no se pueden combinar.");
        if (replayFlag && !OverlayCommands.StartsInstance(command.Value)) return Fail("--replay abre un overlay nuevo; no se combina con el comando " + OverlayCommands.Name(command.Value) + ".");
        return new OverlayArguments(command.Value, demo, hidden, replay, fast, null);
    }

    private static OverlayArguments Fail(string error) => new(OverlayCommand.Show, false, false, null, false, error);
}
