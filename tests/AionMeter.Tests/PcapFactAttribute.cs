using SharpPcap;

namespace AionMeter.Tests;

public sealed class PcapFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> Available = new(() => Pcap.LibpcapVersion > new Version(0, 0));

    public PcapFactAttribute()
    {
        if (!Available.Value) Skip = "libpcap or Npcap is not installed";
    }
}
