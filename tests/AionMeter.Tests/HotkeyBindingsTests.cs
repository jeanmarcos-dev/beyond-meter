using AionMeter.Core.Settings;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class HotkeyBindingsTests
{
    [Fact]
    public void The_default_hotkeys_are_those_of_the_windows_app()
    {
        var bindings = HotkeyBindings.From(new UserSettings());

        Assert.Equal(new Dictionary<OverlayCommand, string>
        {
            [OverlayCommand.Toggle] = "Ctrl+Shift+D",
            [OverlayCommand.Reset] = "Ctrl+Shift+R",
            [OverlayCommand.Lock] = "Ctrl+Shift+L",
            [OverlayCommand.Timers] = "Ctrl+Shift+T",
        }, bindings);
    }

    [Fact]
    public void A_cleared_hotkey_is_left_out()
    {
        var bindings = HotkeyBindings.From(new UserSettings { HotkeyReset = " ", HotkeyTimers = "" });

        Assert.Equal([OverlayCommand.Toggle, OverlayCommand.Lock], bindings.Keys.Order());
    }
}
