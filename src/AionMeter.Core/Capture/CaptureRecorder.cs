using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace AionMeter.Core.Capture;

public sealed class CaptureRecorder : IDisposable
{
    private readonly object _gate = new();
    private CaptureFileWriterDevice? _writer;

    public string? Path { get; private set; }

    public bool IsRecording => Path is not null;

    public string Start(string directory, LinkLayers linkType)
    {
        lock (_gate)
        {
            StopLocked();
            Directory.CreateDirectory(directory);
            var path = NewPath(directory);
            var writer = new CaptureFileWriterDevice(path);
            writer.Open(new DeviceConfiguration { LinkLayerType = linkType });
            _writer = writer;
            Path = path;
            return path;
        }
    }

    public void Write(RawCapture packet)
    {
        lock (_gate) _writer?.Write(packet);
    }

    public void Stop()
    {
        lock (_gate) StopLocked();
    }

    public void Dispose() => Stop();

    private void StopLocked()
    {
        _writer?.Close();
        _writer = null;
        Path = null;
    }

    private static string NewPath(string directory)
    {
        var stem = $"aion2_{DateTime.Now:yyyyMMdd_HHmmss}";
        var path = System.IO.Path.Combine(directory, stem + ".pcap");
        for (var n = 1; File.Exists(path); n++) path = System.IO.Path.Combine(directory, $"{stem}_{n}.pcap");
        return path;
    }
}
