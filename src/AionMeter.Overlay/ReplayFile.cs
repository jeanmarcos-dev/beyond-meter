using SharpPcap;
using SharpPcap.LibPcap;

namespace AionMeter.Overlay;

public static class ReplayFile
{
    public static readonly IReadOnlyList<string> Patterns = ["*.pcap", "*.pcapng"];

    public static string? Problem(string path)
    {
        if (!File.Exists(path)) return "No existe la grabación: " + path;
        try
        {
            using var reader = new CaptureFileReaderDevice(path);
            reader.Open();
            return null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return $"No se puede leer la grabación {path}: {ex.Message}";
        }
    }
}
