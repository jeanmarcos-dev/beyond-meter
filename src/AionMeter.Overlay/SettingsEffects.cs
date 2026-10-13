using AionMeter.Core.Capture;
using AionMeter.Core.Settings;

namespace AionMeter.Overlay;

public static class SettingsEffects
{
    public static void Apply(UserSettings previous, UserSettings next, MeterSession session, Func<string?, IEventSource>? liveCapture, Action rebindHotkeys)
    {
        if (liveCapture is not null && previous.CaptureDevice != next.CaptureDevice) session.ReplaceCapture(liveCapture(next.CaptureDevice));
        if (previous.RecordPackets != next.RecordPackets && session.IsRecording != next.RecordPackets) session.ToggleRecording();
        if (!SameHotkeys(previous, next)) rebindHotkeys();
    }

    public static bool SameHotkeys(UserSettings a, UserSettings b) =>
        a.HotkeyToggleOverlay == b.HotkeyToggleOverlay
        && a.HotkeyReset == b.HotkeyReset
        && a.HotkeyClickThrough == b.HotkeyClickThrough
        && a.HotkeyTimers == b.HotkeyTimers;
}
