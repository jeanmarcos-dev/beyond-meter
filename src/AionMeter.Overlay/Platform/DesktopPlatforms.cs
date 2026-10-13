namespace AionMeter.Overlay.Platform;

public static class DesktopPlatforms
{
#if WINDOWS
    public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{AE1B2590-0A93-4500-9CB1-0F543771957A}_is1";

    public static IDesktopPlatform ForThisSystem() =>
        new WindowsDesktop(
            new Win32Native(),
            data => new ToastAlert(Path.Combine(data, "logs", "alerts.log")),
            notice: data => new ToastAlert(Path.Combine(data, "logs", "alerts.log"), silent: true),
            installed: InstalledByTheInstaller);

    private static bool InstalledByTheInstaller()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UninstallKey);
            return AionMeter.Core.Updates.UpdateService.RunsFrom(key?.GetValue("InstallLocation") as string, Path.GetFullPath(AppContext.BaseDirectory));
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }
#else
    public static IDesktopPlatform ForThisSystem() => new LinuxDesktop();
#endif
}
