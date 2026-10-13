using AionMeter.Core.Settings;

namespace AionMeter.Overlay;

public static class HotkeyBindings
{
    public static IReadOnlyDictionary<OverlayCommand, string> From(UserSettings settings) =>
        new Dictionary<OverlayCommand, string>
        {
            [OverlayCommand.Toggle] = settings.HotkeyToggleOverlay,
            [OverlayCommand.Reset] = settings.HotkeyReset,
            [OverlayCommand.Lock] = settings.HotkeyClickThrough,
            [OverlayCommand.Timers] = settings.HotkeyTimers,
        }
        .Where(binding => !string.IsNullOrWhiteSpace(binding.Value))
        .ToDictionary();
}
