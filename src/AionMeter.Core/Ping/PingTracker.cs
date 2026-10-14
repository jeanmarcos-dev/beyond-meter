using AionMeter.Core.Events;

namespace AionMeter.Core.Ping;

public sealed class PingTracker
{
    public const long StaleAfterMs = 30_000;

    private const long UnrealClockOffsetMs = 16_777_216_000;
    private const long DotnetEpochOffsetMs = 62_135_596_800_000;
    private const long MinPingMs = 1;
    private const long MaxPingMs = 9_999;
    private const long OffsetToleranceMs = 15;
    private const long PerfAgreementMs = 25;
    private const int PerfMissesToDistrust = 2;

    private readonly IPerfClock? _clock;
    private readonly object _gate = new();
    private readonly Queue<long> _requests = new();
    private List<long> _previousOffsets = new();
    private long? _learnedOffset;
    private int _perfMisses;
    private bool _perfDistrusted;
    private (long AtMs, int Ms)? _last;

    public PingTracker(IPerfClock? clock)
    {
        _clock = clock;
    }

    public void Process(GameEvent e)
    {
        lock (_gate)
        {
            switch (e)
            {
                case PingRequestEvent r:
                    _requests.Enqueue(r.TimeMs);
                    while (r.TimeMs - _requests.Peek() > MaxPingMs) _requests.Dequeue();
                    break;
                case PingReplyEvent r:
                    OnReply(r.TimeMs, r.ClientStamp);
                    break;
                case GameFlowLockedEvent:
                    Clear();
                    break;
            }
        }
    }

    public int? Current(long nowMs)
    {
        lock (_gate)
        {
            return _last is { } last && nowMs - last.AtMs <= StaleAfterMs ? last.Ms : null;
        }
    }

    public void Reset()
    {
        lock (_gate) Clear();
    }

    private void Clear()
    {
        _requests.Clear();
        _previousOffsets = new();
        _learnedOffset = null;
        _perfMisses = 0;
        _perfDistrusted = false;
        _last = null;
    }

    private void OnReply(long arrivalMs, long stamp)
    {
        var perf = _perfDistrusted ? null : FromPerfClock(stamp, arrivalMs);
        var paired = FromRequest(stamp, arrivalMs);
        if (perf is { } p && paired is { } q)
        {
            if (Math.Abs(p - q) > PerfAgreementMs)
            {
                perf = null;
                if (++_perfMisses >= PerfMissesToDistrust) _perfDistrusted = true;
            }
            else
            {
                _perfMisses = 0;
            }
        }

        var rtt = perf ?? paired ?? Valid(arrivalMs - (stamp - DotnetEpochOffsetMs)) ?? FromLearnedOffset(stamp, arrivalMs);
        if (rtt is { } ms) _last = (arrivalMs, (int)ms);
    }

    private long? FromPerfClock(long stamp, long arrivalMs)
    {
        if (_clock is null) return null;
        var now = _clock.Read();
        var sentCounterMs = stamp - UnrealClockOffsetMs;
        if (sentCounterMs < 0 || sentCounterMs > now.CounterMs) return null;
        return Valid(arrivalMs - (sentCounterMs + (now.WallMs - now.CounterMs)));
    }

    private long? FromRequest(long stamp, long arrivalMs)
    {
        var offsets = _requests
            .Where(t => arrivalMs - t is >= 0 and <= MaxPingMs)
            .Select(t => t - stamp)
            .ToList();

        long? picked = offsets.Count switch
        {
            0 => null,
            1 => offsets[0],
            _ => (_learnedOffset is { } known ? Near(known, offsets) : null) ?? FirstAgreeing(offsets, _previousOffsets),
        };

        if (picked is { } offset)
        {
            if (_learnedOffset is { } known && Math.Abs(offset - known) <= OffsetToleranceMs)
                _learnedOffset = offset;
            else if (Near(offset, _previousOffsets) is not null)
                _learnedOffset = offset;
            else
                _learnedOffset = null;
        }
        _previousOffsets = offsets;

        return picked is null ? null : Valid(arrivalMs - (stamp + picked.Value));
    }

    private long? FromLearnedOffset(long stamp, long arrivalMs) =>
        _learnedOffset is { } offset ? Valid(arrivalMs - (stamp + offset)) : null;

    private static long? FirstAgreeing(List<long> offsets, List<long> previous)
    {
        foreach (var offset in offsets)
            if (Near(offset, previous) is not null) return offset;
        return null;
    }

    private static long? Near(long offset, IEnumerable<long> candidates)
    {
        foreach (var c in candidates)
            if (Math.Abs(c - offset) <= OffsetToleranceMs) return c;
        return null;
    }

    private static long? Valid(long rtt) => rtt is >= MinPingMs and <= MaxPingMs ? rtt : null;
}
