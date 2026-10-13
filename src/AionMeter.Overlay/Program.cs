using AionMeter.Overlay;
using AionMeter.Overlay.Platform;
using Avalonia;

var parsed = OverlayArguments.Parse(args);
if (parsed.Error is { } error)
{
    Console.Error.WriteLine(error);
    return 2;
}

var desktop = DesktopPlatforms.ForThisSystem();
var socket = desktop.CommandChannelPath;
if (parsed.ReplayPath is { } replayPath)
{
    if (CommandChannel.IsListening(socket))
    {
        Console.Error.WriteLine("Ya hay un overlay abierto: ciérralo con 'aionmeter-overlay quit' antes de repetir una grabación.");
        return 1;
    }
    if (ReplayFile.Problem(replayPath) is { } problem)
    {
        Console.Error.WriteLine(problem);
        return 1;
    }
}
else
{
    if (CommandChannel.TrySend(socket, parsed.Command)) return 0;
    if (!OverlayCommands.StartsInstance(parsed.Command))
    {
        Console.Error.WriteLine("El overlay no está abierto.");
        return 1;
    }
}

if (!desktop.ClaimInstance())
{
    CommandChannel.TrySend(socket, OverlayCommand.Show);
    return 0;
}

OverlayApp.Desktop = desktop;
OverlayApp.Options = new OverlayOptions(parsed.Demo, socket, parsed.Hidden, parsed.ReplayPath, parsed.Fast, parsed.Updated);
return AppBuilder.Configure<OverlayApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
