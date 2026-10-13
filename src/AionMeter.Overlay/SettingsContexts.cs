using System.Globalization;
using AionMeter.Core.Capture;
using AionMeter.Core.Storage;
using AionMeter.Core.Text;
using AionMeter.Overlay.Platform;
using SharpPcap;

namespace AionMeter.Overlay;

public static class SettingsContexts
{
    public static SettingsContext For(IDesktopPlatform desktop, HistoryStore history, string dataDirectory, bool isWindows, Func<IReadOnlyList<(string Name, string Description)>> listDevices, UpdateCoordinator? updates = null)
    {
        var t = UiText.Current;
        return new SettingsContext(
            isWindows,
            NamesLanguages(dataDirectory),
            isWindows ? Devices(listDevices) : [],
            string.Format(CultureInfo.InvariantCulture, t.HistoryInfo, history.List().Count, history.Directory),
            updates?.VersionText() ?? string.Format(CultureInfo.InvariantCulture, t.OvVersion, AppVersion()),
            updates?.CanInstall ?? false,
            desktop.Open,
            dataDirectory,
            desktop.DataDirectory,
            Path.Combine(desktop.DataDirectory, "logs"),
            updates is null ? null : updates.CheckNowAsync);
    }

    public static IReadOnlyList<string> NamesLanguages(string dataDirectory)
    {
        var folder = Path.Combine(dataDirectory, "npcs");
        return Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.json").Select(Path.GetFileNameWithoutExtension).OfType<string>().Order(StringComparer.Ordinal).ToList()
            : [];
    }

    public static IReadOnlyList<CaptureDevice> Devices(Func<IReadOnlyList<(string Name, string Description)>> listDevices)
    {
        var t = UiText.Current;
        var devices = new List<CaptureDevice> { new(null, t.AdapterAuto) };
        try
        {
            devices.AddRange(listDevices().Select(d => new CaptureDevice(d.Name, d.Description)));
        }
        catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException or PcapException)
        {
            devices.Add(new CaptureDevice(null, t.NpcapMissing));
        }
        return devices;
    }

    public static string AppVersion() => (typeof(SettingsContexts).Assembly.GetName().Version ?? new Version(0, 0, 0)).ToString(3);
}
