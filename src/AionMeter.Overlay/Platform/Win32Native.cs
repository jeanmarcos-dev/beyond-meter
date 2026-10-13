using System.Runtime.InteropServices;

namespace AionMeter.Overlay.Platform;

public interface IWin32
{
    long GetExStyle(IntPtr window);

    void SetExStyle(IntPtr window, long style);

    void MakeLayerOpaque(IntPtr window);

    bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);

    void UnregisterHotKey(IntPtr window, int id);

    (int X, int Y)? WindowOrigin(IntPtr window);
}

public sealed partial class Win32Native : IWin32
{
    private const int ExStyleIndex = -20;
    private const uint LayerAlpha = 0x2;

    public long GetExStyle(IntPtr window) => GetWindowLongPtr(window, ExStyleIndex);

    public void SetExStyle(IntPtr window, long style) => SetWindowLongPtr(window, ExStyleIndex, (nint)style);

    public void MakeLayerOpaque(IntPtr window) => SetLayeredWindowAttributes(window, 0, 255, LayerAlpha);

    public bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key) => RegisterHotKeyNative(window, id, modifiers, key);

    public void UnregisterHotKey(IntPtr window, int id) => UnregisterHotKeyNative(window, id);

    public (int X, int Y)? WindowOrigin(IntPtr window) => GetWindowRect(window, out var rect) ? (rect.Left, rect.Top) : null;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr(nint window, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLongPtr(nint window, int index, nint value);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetLayeredWindowAttributes(nint window, uint colorKey, byte alpha, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "RegisterHotKey")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKeyNative(nint window, int id, uint modifiers, uint key);

    [LibraryImport("user32.dll", EntryPoint = "UnregisterHotKey")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKeyNative(nint window, int id);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint window, out Rect rect);

    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
