using System.Globalization;
using AionMeter.Core.Settings;
using AionMeter.Core.Text;

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

    public static string? TakenWarning(UiText text, IReadOnlyDictionary<OverlayCommand, string> hotkeys, IReadOnlyList<OverlayCommand> taken) =>
        taken.Count == 0 ? null : string.Format(CultureInfo.InvariantCulture, text.OvHotkeyTaken, string.Join(", ", taken.Select(command => hotkeys[command])));
}
