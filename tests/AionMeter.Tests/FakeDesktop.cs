using Avalonia.Controls;
using AionMeter.Overlay;
using AionMeter.Overlay.Platform;

namespace AionMeter.Tests;

public sealed class FakeDesktop : IDesktopPlatform, IBossAlert
{
    public FakeDesktop(string? root = null)
    {
        var dir = root ?? Path.GetTempPath();
        ConfigDirectory = Path.Combine(dir, "config");
        DataDirectory = Path.Combine(dir, "data");
        CommandChannelPath = Path.Combine(dir, "overlay.sock");
    }

    public string ConfigDirectory { get; }

    public string? LegacySettingsPath { get; set; }

    public string DataDirectory { get; }

    public string CommandChannelPath { get; }

    public IBossAlert Alert => this;

    public bool CaptureLibraryMissing { get; set; }

    public IBossAlert Notice => new Recorder(Notices);

    public List<(string Title, string Body)> Notices { get; } = [];

    public bool IsInstalledCopy { get; set; }

    public bool ClaimInstance() => true;

    public List<(string Path, bool Quiet)> Installers { get; } = [];

    public void LaunchInstaller(string path, bool quiet) => Installers.Add((path, quiet));

    private sealed class Recorder(List<(string Title, string Body)> into) : IBossAlert
    {
        public void Notify(string title, string body) => into.Add((title, body));
    }

    public List<(string Title, string Body)> Notifications { get; } = [];

    public List<(IntPtr Window, bool Enabled)> ClickThrough { get; } = [];

    public List<string> Opened { get; } = [];

    public Dictionary<OverlayCommand, string> Hotkeys { get; } = [];

    public HashSet<OverlayCommand> TakenHotkeys { get; } = [];

    public Action<OverlayCommand>? Pressed { get; private set; }

    public Dictionary<OverlayCommand, string> Hints { get; } = [];

    public void Notify(string title, string body) => Notifications.Add((title, body));

    public IReadOnlyDictionary<OverlayCommand, string> ShortcutHints() => Hints;

    public IReadOnlyList<OverlayCommand> RegisterHotkeys(TopLevel window, IReadOnlyDictionary<OverlayCommand, string> hotkeys, Action<OverlayCommand> pressed)
    {
        Hotkeys.Clear();
        foreach (var (command, gesture) in hotkeys)
            if (!TakenHotkeys.Contains(command)) Hotkeys[command] = gesture;
        Pressed = pressed;
        return hotkeys.Keys.Where(TakenHotkeys.Contains).ToList();
    }

    public void SetClickThrough(IntPtr window, bool enabled) => ClickThrough.Add((window, enabled));

    public (int X, int Y)? WindowPosition(IntPtr window) => null;

    public void Open(string target) => Opened.Add(target);

    public void Dispose()
    {
    }
}
