namespace AionMeter.Core.Input;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x1,
    Control = 0x2,
    Shift = 0x4,
    Win = 0x8,
}

public readonly record struct HotkeyGesture(HotkeyModifiers Modifiers, uint VirtualKey)
{
    private static readonly Dictionary<string, uint> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Space"] = 0x20,
        ["Enter"] = 0x0D,
        ["Return"] = 0x0D,
        ["Tab"] = 0x09,
        ["Escape"] = 0x1B,
        ["Back"] = 0x08,
        ["Pause"] = 0x13,
        ["Scroll"] = 0x91,
        ["Insert"] = 0x2D,
        ["Delete"] = 0x2E,
        ["Home"] = 0x24,
        ["End"] = 0x23,
        ["PageUp"] = 0x21,
        ["Prior"] = 0x21,
        ["PageDown"] = 0x22,
        ["Next"] = 0x22,
        ["Left"] = 0x25,
        ["Up"] = 0x26,
        ["Right"] = 0x27,
        ["Down"] = 0x28,
        ["Multiply"] = 0x6A,
        ["Add"] = 0x6B,
        ["Subtract"] = 0x6D,
        ["Decimal"] = 0x6E,
        ["Divide"] = 0x6F,
        ["OemSemicolon"] = 0xBA,
        ["OemPlus"] = 0xBB,
        ["OemComma"] = 0xBC,
        ["OemMinus"] = 0xBD,
        ["OemPeriod"] = 0xBE,
        ["OemQuestion"] = 0xBF,
        ["OemTilde"] = 0xC0,
        ["OemOpenBrackets"] = 0xDB,
        ["OemPipe"] = 0xDC,
        ["OemCloseBrackets"] = 0xDD,
        ["OemQuotes"] = 0xDE,
    };

    public static bool TryParse(string? text, out HotkeyGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || text.TrimEnd().EndsWith('+')) return false;
        var modifiers = HotkeyModifiers.None;
        foreach (var part in parts[..^1])
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= HotkeyModifiers.Control; break;
                case "shift": modifiers |= HotkeyModifiers.Shift; break;
                case "alt": modifiers |= HotkeyModifiers.Alt; break;
                case "win": modifiers |= HotkeyModifiers.Win; break;
                default: return false;
            }
        }
        if (VirtualKeyOf(parts[^1]) is not { } vk) return false;
        gesture = new HotkeyGesture(modifiers, vk);
        return true;
    }

    private static uint? VirtualKeyOf(string key)
    {
        if (key.Length == 1 && char.IsAsciiLetter(key[0])) return char.ToUpperInvariant(key[0]);
        if (key.Length == 1 && char.IsAsciiDigit(key[0])) return key[0];
        if (key.Length == 2 && key[0] is 'D' or 'd' && char.IsAsciiDigit(key[1])) return key[1];
        if (key.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase) && key.Length == 7 && char.IsAsciiDigit(key[6]))
            return 0x60u + (uint)(key[6] - '0');
        if (key.Length is 2 or 3 && key[0] is 'F' or 'f' && int.TryParse(key[1..], out var f) && f is >= 1 and <= 24)
            return 0x70u + (uint)(f - 1);
        return NamedKeys.TryGetValue(key, out var named) ? named : null;
    }
}
