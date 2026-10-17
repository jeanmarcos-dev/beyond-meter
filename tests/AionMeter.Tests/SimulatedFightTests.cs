using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Overlay;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class SimulatedFightTests : IDisposable
{
    private const uint Me = 100, Boss = 500;

    private readonly string _dir = Directory.CreateTempSubdirectory("simulated-fights").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void A_replayed_file_takes_over_from_live_capture_and_is_not_saved()
    {
        var live = new ScriptedSource();
        var session = Session(live);
        session.Start();
        var replay = new ScriptedSource();

        session.ReplayFile(replay);
        Assert.True(replay.Started);
        Assert.True(session.ReplayingFile);

        live.Emit(new SelfIdentifiedEvent(0, 7, "Live", 1, GameClass.Cleric));
        Assert.Null(session.Tracker.Snapshot(null, 0));

        Fight(replay, 1_000);
        replay.Finish();

        Assert.False(session.ReplayingFile);
        Assert.Empty(session.History.List());
        live.Emit(new SelfIdentifiedEvent(0, 7, "Live", 1, GameClass.Cleric));
        Assert.Equal("Live", session.Tracker.SelfName);
    }

    [Fact]
    public void A_demo_fight_is_not_saved_even_after_it_ends()
    {
        var session = Session(null);

        session.StartDemo(TimeSpan.FromMilliseconds(300));
        SpinWait.SpinUntil(() => !session.DemoRunning, TimeSpan.FromSeconds(5));
        session.Tracker.Tick(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 600_000);

        Assert.Empty(session.History.List());
    }

    [AvaloniaFact]
    public void The_history_and_demo_commands_show_a_hidden_overlay()
    {
        var session = Session(null);
        var window = new OverlayWindow(session, new UserSettings(), Path.Combine(_dir, "settings.json"), null, startHidden: true);

        window.Apply(OverlayCommand.History);
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.IsVisible);

        window.Apply(OverlayCommand.Toggle);
        window.Apply(OverlayCommand.Demo);
        Assert.True(window.IsVisible);
        Assert.True(session.DemoRunning);
        session.Dispose();
    }

    [AvaloniaFact]
    public void Replaying_a_missing_file_does_nothing()
    {
        var session = Session(null);
        var window = new OverlayWindow(session, new UserSettings(), Path.Combine(_dir, "settings.json"), null, startHidden: true)
        {
            ReplaySource = _ => new ScriptedSource(),
        };

        Assert.False(window.Replay(Path.Combine(_dir, "missing.pcap")));
        Assert.False(session.ReplayingFile);
    }

    [Fact]
    public void Boss_mode_saves_boss_fights_only_even_when_every_fight_is_kept()
    {
        const uint Mob = 501;
        var live = new ScriptedSource();
        var session = Session(live, TargetMode.BossOnly);
        session.Start();
        live.Emit(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        live.Emit(new NpcSeenEvent(0, Mob, 2000002, 50_000));

        for (var t = 1_000L; t <= 60_000; t += 1_000) live.Emit(new DamageEvent(t, Me, Mob, 16040000, 1_000, HitFlags.None));
        session.Tracker.Tick(120_000);
        Assert.Empty(session.History.List());

        Fight(live, 200_000);
        session.Tracker.Tick(300_000);
        Assert.Single(session.History.List());
    }

    private MeterSession Session(IEventSource? capture, TargetMode mode = TargetMode.All) =>
        new(new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = mode }), new HistoryStore(Path.Combine(_dir, "fights")), capture, null, new HistoryPolicy(HistoryMode.All, 5));

    private static void Fight(ScriptedSource source, long start)
    {
        source.Emit(new SelfIdentifiedEvent(start, Me, "Ilvane", 1304, GameClass.Elementalist));
        source.Emit(new NpcSeenEvent(start, Boss, 2300243, 120_000_000));
        for (var t = start; t < start + 30_000; t += 1_000) source.Emit(new DamageEvent(t, Me, Boss, 16040000, 1_000, HitFlags.None));
    }

    private sealed class ScriptedSource : IEventSource
    {
        public bool Started { get; private set; }

        public event Action<GameEvent>? EventDecoded;

        public event Action<CaptureStatus>? StatusChanged;

        public CaptureStatus Status { get; private set; } = CaptureStatus.Stopped;

        public void Emit(GameEvent e) => EventDecoded?.Invoke(e);

        public void Finish()
        {
            Status = CaptureStatus.Stopped;
            StatusChanged?.Invoke(Status);
        }

        public void Start() => Started = true;

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}
