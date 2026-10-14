using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AionMeter.Core.Ping;

public static class PerfClocks
{
    public static IPerfClock? ForThisMachine() =>
        OperatingSystem.IsWindows() ? new StopwatchClock()
        : OperatingSystem.IsLinux() ? new MonotonicRawClock()
        : null;

    private static long WallMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private sealed class StopwatchClock : IPerfClock
    {
        public PerfReading Read()
        {
            var ticks = Stopwatch.GetTimestamp();
            return new((long)((Int128)ticks * 1000 / Stopwatch.Frequency), WallMs());
        }
    }

    private sealed class MonotonicRawClock : IPerfClock
    {
        private const int ClockMonotonicRaw = 4;

        public PerfReading Read()
        {
            clock_gettime(ClockMonotonicRaw, out var ts);
            return new(ts.Seconds * 1000 + ts.Nanoseconds / 1_000_000, WallMs());
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Timespec
        {
            public long Seconds;
            public long Nanoseconds;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int clock_gettime(int clockId, out Timespec time);
    }
}
