using System.Runtime.InteropServices;
using AionMeter.Core.Text;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace AionMeter.Overlay.Platform;

public sealed class ToastAlert(string errorLog, bool silent = false, IBossAlert? fallback = null) : IBossAlert
{
    private bool _registered;

    public void Notify(string title, string body)
    {
        try
        {
            Register();
            var xml = new XmlDocument();
            xml.LoadXml(ToastContent.Xml(title, body, silent));
            ToastNotificationManager.CreateToastNotifier(WindowsDesktop.AppUserModelId).Show(new ToastNotification(xml));
        }
        catch (Exception ex) when (ex is COMException or UnauthorizedAccessException or ArgumentException or IOException)
        {
            Report(ex);
            fallback?.Notify(title, body);
        }
    }

    private void Register()
    {
        if (_registered) return;
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{WindowsDesktop.AppUserModelId}");
        key.SetValue("DisplayName", AppInfo.Name);
        var icon = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(icon)) key.SetValue("IconUri", icon);
        _registered = true;
    }

    private void Report(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(errorLog)!);
            File.AppendAllText(errorLog, $"{DateTime.Now:O} {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
        }
        catch (Exception logError) when (logError is IOException or UnauthorizedAccessException)
        {
        }
    }
}
