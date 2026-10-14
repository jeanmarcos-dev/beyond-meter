using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Ping;
using AionMeter.Core.Storage;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class PingSessionTests : IDisposable
{
    private const long Now = 1_790_000_000_000;
    private const long UnrealOffset = 16_777_216_000;
    private const long GlobalStamp = UnrealOffset + 999_000;

    private readonly string _dir = Directory.CreateTempSubdirectory("ping-session").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private sealed class FakeClock : IPerfClock
    {
        public PerfReading Read() => new(1_000_000, Now);
    }

    private sealed class FakeSource : IEventSource
    {
        public event Action<GameEvent>? EventDecoded;
        public event Action<CaptureStatus>? StatusChanged;
        public CaptureStatus Status => new(CaptureState.Capturing, "Capturing");

        public void Emit(GameEvent e) => EventDecoded?.Invoke(e);

        public void Start() => StatusChanged?.Invoke(Status);

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }

    private MeterSession Session(FakeSource source, bool replay = false) =>
        new(new CombatTracker(GameData.Empty, new MeterOptions()), new HistoryStore(Path.Combine(_dir, "fights")), source, null,
            new HistoryPolicy(), replay: replay, perfClock: new FakeClock());

    [Fact]
    public void A_live_session_reads_the_ping_through_the_machine_clock()
    {
        var source = new FakeSource();
        using var session = Session(source);
        session.Start();

        source.Emit(new PingReplyEvent(Now - 955, GlobalStamp));

        Assert.Equal(45, session.Ping(Now));
    }

    [Fact]
    public void A_replay_ignores_the_machine_clock_and_pairs_requests()
    {
        var source = new FakeSource();
        using var session = Session(source, replay: true);
        session.Start();

        source.Emit(new PingReplyEvent(Now - 955, GlobalStamp));
        Assert.Null(session.Ping(Now));

        source.Emit(new PingRequestEvent(Now));
        source.Emit(new PingReplyEvent(Now + 64, GlobalStamp + 10_000));
        Assert.Equal(64, session.Ping(Now + 3_600_000));
    }

    [Fact]
    public void Replacing_the_capture_clears_the_ping()
    {
        var source = new FakeSource();
        using var session = Session(source);
        session.Start();
        source.Emit(new PingReplyEvent(Now - 955, GlobalStamp));

        session.ReplaceCapture(new FakeSource());

        Assert.Null(session.Ping(Now));
    }

    [Theory]
    [InlineData(1, "1 ms", PingLevel.Good)]
    [InlineData(99, "99 ms", PingLevel.Good)]
    [InlineData(100, "100 ms", PingLevel.Fair)]
    [InlineData(170, "170 ms", PingLevel.Fair)]
    [InlineData(171, "171 ms", PingLevel.Poor)]
    [InlineData(null, "— ms", PingLevel.None)]
    public void The_ping_is_shown_with_its_level(int? ms, string text, PingLevel level)
    {
        Assert.Equal(new PingView(text, level), MeterView.Ping(ms));
    }
}
