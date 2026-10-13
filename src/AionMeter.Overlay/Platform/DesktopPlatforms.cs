namespace AionMeter.Overlay.Platform;

public static class DesktopPlatforms
{
#if WINDOWS
    public static IDesktopPlatform ForThisSystem() =>
        new WindowsDesktop(new Win32Native(), data => new ToastAlert(Path.Combine(data, "logs", "alerts.log")));
#else
    public static IDesktopPlatform ForThisSystem() => new LinuxDesktop();
#endif
}
