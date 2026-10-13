using Avalonia.Controls;

namespace AionMeter.Overlay.Platform;

public interface IDesktopPlatform : IDisposable
{
    string ConfigDirectory { get; }

    string? LegacySettingsPath { get; }

    string DataDirectory { get; }

    string CommandChannelPath { get; }

    IBossAlert Alert { get; }

    bool CaptureLibraryMissing { get; }

    IReadOnlyDictionary<OverlayCommand, string> ShortcutHints();

    IReadOnlyList<OverlayCommand> RegisterHotkeys(TopLevel window, IReadOnlyDictionary<OverlayCommand, string> hotkeys, Action<OverlayCommand> pressed);

    void SetClickThrough(IntPtr window, bool enabled);

    (int X, int Y)? WindowPosition(IntPtr window);

    void Open(string target);
}
