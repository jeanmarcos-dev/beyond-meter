namespace AionMeter.Core.Ping;

public readonly record struct PerfReading(long CounterMs, long WallMs);

public interface IPerfClock
{
    PerfReading Read();
}
