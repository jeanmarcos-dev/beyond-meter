using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;

namespace AionMeter.Overlay.Platform;

public sealed class LinuxDesktop : IDesktopPlatform
{
    private readonly Lazy<X11Window?> _x11 = new(X11Window.Open);
    private readonly Func<ProcessStartInfo, Process?> _start;

    public LinuxDesktop(Func<string, string?>? environment = null, Func<ProcessStartInfo, Process?>? start = null)
    {
        var env = environment ?? Environment.GetEnvironmentVariable;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        ConfigDirectory = Path.Combine(NonEmpty(env("XDG_CONFIG_HOME")) ?? Path.Combine(home, ".config"), "aionmeter");
        DataDirectory = Path.Combine(NonEmpty(env("XDG_DATA_HOME")) ?? Path.Combine(home, ".local", "share"), "aionmeter");
        CommandChannelPath = Path.Combine(NonEmpty(env("XDG_RUNTIME_DIR")) ?? Path.GetTempPath(), "aionmeter-overlay.sock");
        _start = start ?? Process.Start;
        Alert = new DesktopBossAlert(_start);
    }

    public string ConfigDirectory { get; }

    public string DataDirectory { get; }

    public string CommandChannelPath { get; }

    public IBossAlert Alert { get; }

    public IReadOnlyDictionary<OverlayCommand, string> ShortcutHints() => Overlay.ShortcutHints.Load();

    public IReadOnlyList<OverlayCommand> RegisterHotkeys(TopLevel window, IReadOnlyDictionary<OverlayCommand, string> hotkeys, Action<OverlayCommand> pressed) => [];

    public void SetClickThrough(IntPtr window, bool enabled) => _x11.Value?.SetClickThrough(window, enabled);

    public (int X, int Y)? WindowPosition(IntPtr window) => _x11.Value?.RootPosition(window);

    public void Open(string target)
    {
        var info = new ProcessStartInfo("xdg-open") { UseShellExecute = false, CreateNoWindow = true };
        info.ArgumentList.Add(target);
        try
        {
            _start(info)?.Dispose();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
        }
    }

    public void Dispose()
    {
        if (_x11.IsValueCreated) _x11.Value?.Dispose();
    }

    private static string? NonEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
