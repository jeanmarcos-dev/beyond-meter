using AionMeter.Core.Input;

namespace AionMeter.Overlay.Platform;

public interface IDesktopPlatform : IDisposable
{
    string ConfigDirectory { get; }

    string DataDirectory { get; }

    string CommandChannelPath { get; }

    IBossAlert Alert { get; }

    IReadOnlyDictionary<OverlayCommand, string> ShortcutHints();

    IReadOnlyList<OverlayCommand> RegisterHotkeys(IntPtr window, IReadOnlyDictionary<OverlayCommand, HotkeyGesture> hotkeys, Action<OverlayCommand> pressed);

    void SetClickThrough(IntPtr window, bool enabled);

    (int X, int Y)? WindowPosition(IntPtr window);

    void Open(string target);
}
