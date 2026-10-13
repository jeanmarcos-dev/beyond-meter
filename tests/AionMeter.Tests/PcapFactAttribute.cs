using SharpPcap;

namespace AionMeter.Tests;

public sealed class PcapFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> Available = new(() =>
    {
        try
        {
            return !string.IsNullOrEmpty(Pcap.Version);
        }
        catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException or EntryPointNotFoundException)
        {
            return false;
        }
    });

    public PcapFactAttribute()
    {
        if (!Available.Value) Skip = "libpcap or Npcap is not installed";
    }
}
