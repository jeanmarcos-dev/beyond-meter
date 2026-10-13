using AionMeter.Core.Input;

namespace AionMeter.Tests;

public sealed class HotkeyGestureTests
{
    [Theory]
    [InlineData("Ctrl+Shift+D", HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x44)]
    [InlineData("Alt+F9", HotkeyModifiers.Alt, 0x78)]
    [InlineData("control + shift + t", HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x54)]
    [InlineData("Win+5", HotkeyModifiers.Win, 0x35)]
    [InlineData("Ctrl+D5", HotkeyModifiers.Control, 0x35)]
    [InlineData("Ctrl+NumPad7", HotkeyModifiers.Control, 0x67)]
    [InlineData("Shift+F24", HotkeyModifiers.Shift, 0x87)]
    [InlineData("Ctrl+Space", HotkeyModifiers.Control, 0x20)]
    [InlineData("Ctrl+PageUp", HotkeyModifiers.Control, 0x21)]
    [InlineData("Ctrl+OemTilde", HotkeyModifiers.Control, 0xC0)]
    [InlineData("F12", HotkeyModifiers.None, 0x7B)]
    public void Accepted_gestures_give_modifiers_and_virtual_key(string text, HotkeyModifiers modifiers, int vk)
    {
        Assert.True(HotkeyGesture.TryParse(text, out var gesture));
        Assert.Equal(new HotkeyGesture(modifiers, (uint)vk), gesture);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("Ctrl+")]
    [InlineData("Hyper+D")]
    [InlineData("Ctrl+Banana")]
    [InlineData("Ctrl+F25")]
    public void Invalid_gestures_are_rejected(string? text)
    {
        Assert.False(HotkeyGesture.TryParse(text, out _));
    }
}
