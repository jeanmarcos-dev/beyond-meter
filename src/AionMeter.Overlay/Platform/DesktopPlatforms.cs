namespace AionMeter.Overlay.Platform;

public static class DesktopPlatforms
{
    public static IDesktopPlatform ForThisSystem() => new LinuxDesktop();
}
