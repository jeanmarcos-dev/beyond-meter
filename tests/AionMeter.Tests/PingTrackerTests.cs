using AionMeter.Core.Events;
using AionMeter.Core.Ping;

namespace AionMeter.Tests;

public class PingTrackerTests
{
    private const long Now = 1_790_000_000_000;
    private const long Clock = 17_552_452_660;
    private const long UnrealOffset = 16_777_216_000;
    private const long DotnetEpoch = 62_135_596_800_000;

    private sealed class FakeClock(long counterMs, long wallMs) : IPerfClock
    {
        public PerfReading Read() => new(counterMs, wallMs);
    }

    private static PingTracker Tracker(IPerfClock? clock = null) => new(clock);

    private static void Request(PingTracker t, long at) => t.Process(new PingRequestEvent(at));

    private static void Reply(PingTracker t, long at, long stamp) => t.Process(new PingReplyEvent(at, stamp));

    [Fact]
    public void A_global_stamp_is_read_through_the_performance_counter()
    {
        var t = Tracker(new FakeClock(1_000_000, Now));

        Reply(t, Now - 955, UnrealOffset + 999_000);

        Assert.Equal(45, t.Current(Now));
    }

    [Fact]
    public void A_wall_clock_stamp_needs_no_request()
    {
        var t = Tracker();

        Reply(t, Now + 80, Now + DotnetEpoch);

        Assert.Equal(80, t.Current(Now + 80));
    }

    [Fact]
    public void Without_a_counter_the_reply_is_timed_against_its_request()
    {
        var t = Tracker();

        Request(t, Now);
        Reply(t, Now + 64, Clock);

        Assert.Equal(64, t.Current(Now + 64));
    }

    [Fact]
    public void Two_paired_pings_teach_the_offset_for_unpaired_replies()
    {
        var t = Tracker();
        Request(t, Now);
        Reply(t, Now + 64, Clock);
        Request(t, Now + 10_000);
        Reply(t, Now + 10_090, Clock + 10_000);
        Assert.Equal(90, t.Current(Now + 10_090));

        Reply(t, Now + 20_045, Clock + 20_000);

        Assert.Equal(45, t.Current(Now + 20_045));
    }

    [Fact]
    public void A_clock_jump_is_not_reported_and_the_offset_is_relearned()
    {
        var t = Tracker();
        for (var n = 0; n < 2; n++)
        {
            Request(t, Now + n * 10_000);
            Reply(t, Now + n * 10_000 + 60, Clock + n * 10_000);
        }
        Assert.Equal(60, t.Current(Now + 10_060));

        const long jump = 2_320;
        Request(t, Now + 20_000 + jump);
        Reply(t, Now + 20_070 + jump, Clock + 20_000);
        Assert.Equal(70, t.Current(Now + 20_070 + jump));

        Reply(t, Now + 30_055 + jump, Clock + 30_000);
        Assert.Equal(70, t.Current(Now + 30_055 + jump));

        for (var n = 4; n < 6; n++)
        {
            Request(t, Now + n * 10_000 + jump);
            Reply(t, Now + n * 10_000 + 80 + jump, Clock + n * 10_000);
        }
        Reply(t, Now + 60_045 + jump, Clock + 60_000);
        Assert.Equal(45, t.Current(Now + 60_045 + jump));
    }

    [Fact]
    public void A_counter_that_disagrees_twice_is_no_longer_trusted()
    {
        var counterNow = Clock - UnrealOffset + 100_000;
        var t = Tracker(new FakeClock(counterNow, Now + 100_000 - 100));
        for (var n = 0; n < 2; n++)
        {
            Request(t, Now + n * 10_000);
            Reply(t, Now + n * 10_000 + 60, Clock + n * 10_000);
            Assert.Equal(60, t.Current(Now + n * 10_000 + 60));
        }

        Reply(t, Now + 20_050, Clock + 20_000);

        Assert.Equal(50, t.Current(Now + 20_050));
    }

    [Fact]
    public void A_counter_that_agrees_with_the_request_is_kept()
    {
        var counterNow = Clock - UnrealOffset + 100_000;
        var t = Tracker(new FakeClock(counterNow, Now + 100_000 - 10));
        for (var n = 0; n < 2; n++)
        {
            Request(t, Now + n * 10_000);
            Reply(t, Now + n * 10_000 + 60, Clock + n * 10_000);
        }

        Reply(t, Now + 20_050, Clock + 20_000);

        Assert.Equal(60, t.Current(Now + 20_050));
    }

    [Theory]
    [InlineData(-300)]
    [InlineData(0)]
    [InlineData(10_000)]
    public void Out_of_range_samples_leave_the_current_ping_alone(long rtt)
    {
        var t = Tracker();
        Reply(t, Now + 80, Now + DotnetEpoch);

        Reply(t, Now + 1_000 + rtt, Now + 1_000 + DotnetEpoch);

        Assert.Equal(80, t.Current(Now + 1_000));
    }

    [Fact]
    public void The_ping_expires_thirty_seconds_after_the_last_sample()
    {
        var t = Tracker();
        Reply(t, Now + 80, Now + DotnetEpoch);

        Assert.Equal(80, t.Current(Now + 80 + 30_000));
        Assert.Null(t.Current(Now + 80 + 30_001));
    }

    [Fact]
    public void A_new_game_flow_clears_the_ping_and_what_was_learned()
    {
        var t = Tracker();
        for (var n = 0; n < 2; n++)
        {
            Request(t, Now + n * 10_000);
            Reply(t, Now + n * 10_000 + 60, Clock + n * 10_000);
        }

        t.Process(new GameFlowLockedEvent(Now + 15_000));
        Assert.Null(t.Current(Now + 15_000));

        Reply(t, Now + 20_045, Clock + 20_000);
        Assert.Null(t.Current(Now + 20_045));
    }

    [Fact]
    public void Reset_clears_the_ping()
    {
        var t = Tracker();
        Reply(t, Now + 80, Now + DotnetEpoch);

        t.Reset();

        Assert.Null(t.Current(Now + 80));
    }

    [Fact]
    public void Several_requests_in_the_window_are_told_apart_by_the_known_offset()
    {
        var t = Tracker();
        for (var n = 0; n < 2; n++)
        {
            Request(t, Now + n * 10_000);
            Reply(t, Now + n * 10_000 + 60, Clock + n * 10_000);
        }

        Request(t, Now + 17_000);
        Request(t, Now + 20_000);
        Reply(t, Now + 20_075, Clock + 20_000);

        Assert.Equal(75, t.Current(Now + 20_075));
    }

    [Fact]
    public void No_ping_before_any_reply()
    {
        Assert.Null(Tracker().Current(Now));
    }

    [Fact]
    public void The_machine_clock_advances_and_pairs_with_the_wall_clock()
    {
        var clock = PerfClocks.ForThisMachine();
        Assert.NotNull(clock);

        var first = clock.Read();
        Thread.Sleep(20);
        var second = clock.Read();

        Assert.InRange(second.CounterMs - first.CounterMs, 15, 2_000);
        Assert.InRange(second.WallMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1_000, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
}
