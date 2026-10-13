using System.ComponentModel;
using System.Diagnostics;
using AionMeter.Core.Input;
using Avalonia.Controls;

namespace AionMeter.Overlay.Platform;

public sealed class WindowsDesktop : IDesktopPlatform
{
    public const string AppUserModelId = "JeanmarcosDev.BeyondMeter";
    public const uint HotkeyMessage = 0x0312;
    public const long TransparentStyle = 0x00000020;
    public const long LayeredStyle = 0x00080000;
    public const uint NoRepeat = 0x4000;
    public const int FirstHotkeyId = 0x4D00;

    private readonly IWin32 _native;
    private readonly Func<ProcessStartInfo, Process?> _start;
    private readonly Dictionary<IntPtr, long> _stylesBeforeClickThrough = [];
    private readonly Dictionary<int, OverlayCommand> _hotkeys = [];
    private readonly Dictionary<OverlayCommand, string> _hints = [];
    private readonly HashSet<TopLevel> _hooked = [];
    private IntPtr _hotkeyWindow;
    private Action<OverlayCommand>? _pressed;

    public WindowsDesktop(IWin32 native, Func<string, IBossAlert> alert, Func<string, string?>? environment = null, Func<ProcessStartInfo, Process?>? start = null)
    {
        var env = environment ?? Environment.GetEnvironmentVariable;
        var roaming = NonEmpty(env("APPDATA")) ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = NonEmpty(env("LOCALAPPDATA")) ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        ConfigDirectory = Path.Combine(roaming, "AionMeter");
        DataDirectory = ConfigDirectory;
        CommandChannelPath = Path.Combine(local, "BeyondMeter", "overlay.sock");
        _native = native;
        _start = start ?? Process.Start;
        Alert = alert(DataDirectory);
    }

    public string ConfigDirectory { get; }

    public string? LegacySettingsPath => null;

    public string DataDirectory { get; }

    public string CommandChannelPath { get; }

    public IBossAlert Alert { get; }

    public IReadOnlyDictionary<OverlayCommand, string> ShortcutHints() => new Dictionary<OverlayCommand, string>(_hints);

    public IReadOnlyList<OverlayCommand> RegisterHotkeys(TopLevel window, IReadOnlyDictionary<OverlayCommand, string> hotkeys, Action<OverlayCommand> pressed)
    {
        if (window.TryGetPlatformHandle() is not { } handle) return hotkeys.Keys.ToList();
        if (_hooked.Add(window)) Win32Properties.AddWndProcHookCallback(window, Hook);
        return RegisterHotkeys(handle.Handle, hotkeys, pressed);
    }

    public IReadOnlyList<OverlayCommand> RegisterHotkeys(IntPtr window, IReadOnlyDictionary<OverlayCommand, string> hotkeys, Action<OverlayCommand> pressed)
    {
        UnregisterHotkeys();
        _hotkeyWindow = window;
        _pressed = pressed;
        var failed = new List<OverlayCommand>();
        var id = FirstHotkeyId;
        foreach (var (command, text) in hotkeys)
        {
            if (HotkeyGesture.TryParse(text, out var gesture) && _native.RegisterHotKey(window, id, (uint)gesture.Modifiers | NoRepeat, gesture.VirtualKey))
            {
                _hotkeys[id] = command;
                _hints[command] = text;
            }
            else
            {
                failed.Add(command);
            }
            id++;
        }
        return failed;
    }

    public bool HandleMessage(uint message, IntPtr wParam)
    {
        if (message != HotkeyMessage || !_hotkeys.TryGetValue((int)wParam, out var command)) return false;
        _pressed?.Invoke(command);
        return true;
    }

    public void SetClickThrough(IntPtr window, bool enabled)
    {
        if (enabled)
        {
            if (_stylesBeforeClickThrough.ContainsKey(window)) return;
            var style = _native.GetExStyle(window);
            _stylesBeforeClickThrough[window] = style;
            _native.SetExStyle(window, style | TransparentStyle | LayeredStyle);
            if ((style & LayeredStyle) == 0) _native.MakeLayerOpaque(window);
        }
        else if (_stylesBeforeClickThrough.Remove(window, out var original))
        {
            _native.SetExStyle(window, original);
        }
    }

    public (int X, int Y)? WindowPosition(IntPtr window) => _native.WindowOrigin(window);

    public void Open(string target)
    {
        try
        {
            _start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
        }
    }

    public void Dispose() => UnregisterHotkeys();

    private void UnregisterHotkeys()
    {
        foreach (var id in _hotkeys.Keys) _native.UnregisterHotKey(_hotkeyWindow, id);
        _hotkeys.Clear();
        _hints.Clear();
    }

    private IntPtr Hook(IntPtr window, uint message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (HandleMessage(message, wParam)) handled = true;
        return IntPtr.Zero;
    }

    private static string? NonEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
