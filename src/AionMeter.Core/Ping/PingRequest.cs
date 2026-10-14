using AionMeter.Core.Protocol;

namespace AionMeter.Core.Ping;

public static class PingRequest
{
    private const int MinFrameBytes = 12;
    private const int MaxFrameBytes = 13;

    private static ReadOnlySpan<byte> RelayedWallClock => [0x05, 0x23, 0x0C, 0x0F];
    private static ReadOnlySpan<byte> RelayedCounter => [0x05, 0x23, 0x0D, 0x10];

    public static bool Detect(ReadOnlySpan<byte> payload)
    {
        if (payload.IndexOf(RelayedWallClock) >= 0 || payload.IndexOf(RelayedCounter) >= 0) return true;

        var found = false;
        var offset = 0;
        while (offset < payload.Length)
        {
            if (!Wire.TryVarint(payload, offset, out var len, out var varintBytes)) return false;
            var size = (long)len + varintBytes - 4;
            if (size <= 0 || offset + size > payload.Length) return false;
            found |= size is >= MinFrameBytes and <= MaxFrameBytes;
            offset += (int)size;
        }
        return found;
    }
}
