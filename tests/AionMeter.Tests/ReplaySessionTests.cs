using System.Diagnostics;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class ReplaySessionTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("replay-session").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [PcapFact]
    public async Task A_fast_replay_shows_the_recorded_fight_without_waiting_for_real_time_and_leaves_no_trace()
    {
        var pcap = GamePcap.Write(Path.Combine(_dir, "game.pcap"));
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        using var session = new MeterSession(
            tracker, history, new PcapReplaySource(GameData.Empty, pcap, realtime: false), null,
            new HistoryPolicy(HistoryMode.All, 1), capturesDirectory: Path.Combine(_dir, "captures"), replay: true);
        var clock = Stopwatch.StartNew();

        session.Start();
        await WaitFor(() => session.Status.Message.StartsWith("Replay finished", StringComparison.Ordinal));

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(GamePcap.SpanSeconds / 2));
        var snapshot = session.Current(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var self = Assert.Single(snapshot!.Combatants);
        Assert.Equal(GamePcap.SelfName, self.Name);
        Assert.True(self.Damage > 0);
        Assert.Empty(history.List());
        Assert.False(Directory.Exists(Path.Combine(_dir, "captures")));
    }

    [PcapFact]
    public async Task A_replay_is_not_closed_by_the_wall_clock_while_it_runs()
    {
        var pcap = GamePcap.Write(Path.Combine(_dir, "game.pcap"));
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        using var session = new MeterSession(
            tracker, new HistoryStore(Path.Combine(_dir, "fights")), new PcapReplaySource(GameData.Empty, pcap, realtime: false), null,
            new HistoryPolicy(HistoryMode.All, 1), replay: true);
        session.Start();
        await WaitFor(() => session.Status.Message.StartsWith("Replay finished", StringComparison.Ordinal));

        session.Tick(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        Assert.True(session.GameRunning);
        Assert.NotNull(session.Current(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "the replay never finished");
            await Task.Delay(20);
        }
    }
}
