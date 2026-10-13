using System.ComponentModel;
using System.Diagnostics;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Storage;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class BossBoardTests : IDisposable
{
    private static readonly DateTimeOffset Now = BossWorld.Now;

    private readonly BossWorld _world = new();

    public void Dispose() => _world.Dispose();

    private (BossBoard Board, FakeBossAlert Alert, Func<DateTimeOffset> Clock) Open(Action<AionMeter.Core.Timers.BossTimers>? seed = null)
    {
        var timers = _world.Timers();
        timers.SetCurrentServer(BossWorld.Server);
        seed?.Invoke(timers);
        var alert = new FakeBossAlert();
        var at = Now;
        return (new BossBoard(timers, _world.Data, alert, () => at), alert, () => at);
    }

    [Fact]
    public void Rows_show_countdown_alive_overdue_and_unknown_states()
    {
        var (board, _, _) = Open(t =>
        {
            t.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, Now.AddMinutes(12).AddSeconds(34)), BossWorld.Alive(2, Now.AddHours(-1))));
        });

        var rows = board.Rows();

        var linx = rows.Single(r => r.NpcCode == BossWorld.Linx);
        Assert.Equal(BossState.Countdown, linx.State);
        Assert.Equal("12:34", linx.Status);
        Assert.Equal("Linx", linx.Name);
        Assert.Equal("Altgard", linx.Zone);
        Assert.Contains(Now.AddMinutes(12).AddSeconds(34).ToString("HH:mm"), linx.Detail);
        var gartua = rows.Single(r => r.NpcCode == BossWorld.Gartua);
        Assert.Equal(BossState.Alive, gartua.State);
        Assert.Equal("Alive", gartua.Status);
        Assert.Contains(Now.AddHours(-1).ToString("HH:mm"), gartua.Detail);
    }

    [Fact]
    public void A_passed_respawn_reads_as_should_be_alive()
    {
        var (board, _, _) = Open(t => t.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, Now.AddMinutes(-3)), BossWorld.Dead(2, Now.AddHours(2)))));

        var linx = board.Rows().Single(r => r.NpcCode == BossWorld.Linx);

        Assert.Equal(BossState.Overdue, linx.State);
        Assert.Equal("Should be up", linx.Status);
    }

    [Fact]
    public void A_kill_without_an_interval_reads_as_unknown()
    {
        var (board, _, _) = Open(t => t.OnNotice(new BossNotice(BossWorld.Linx, BossWorld.Map, "Altgard", BossNoticeKind.Killed, Now.ToUnixTimeMilliseconds(), ServerId: BossWorld.Server)));

        var linx = board.Rows().Single();

        Assert.Equal(BossState.Unknown, linx.State);
        Assert.Equal("Unknown interval", linx.Status);
    }

    [Fact]
    public void Only_the_current_servers_timers_are_listed()
    {
        var (board, _, _) = Open(t =>
        {
            t.OnNotice(new BossNotice(BossWorld.Linx, BossWorld.Map, "Altgard", BossNoticeKind.Killed, Now.ToUnixTimeMilliseconds(), ServerId: 2002));
            t.SetCurrentServer(BossWorld.Server);
        });

        Assert.Empty(board.Rows());
    }

    [Fact]
    public void Summary_names_the_next_watched_boss_with_its_countdown()
    {
        var (board, _, _) = Open(t =>
        {
            t.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, Now.AddMinutes(12).AddSeconds(34)), BossWorld.Dead(2, Now.AddMinutes(5))));
            t.SetWatch(BossWorld.Server, BossWorld.Linx, true);
        });

        Assert.Equal("Next: Linx 12:34", board.Summary());
    }

    [Fact]
    public void Summary_is_empty_without_a_watched_boss()
    {
        var (board, _, _) = Open(t => t.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, Now.AddMinutes(12)), BossWorld.Dead(2, Now.AddMinutes(5)))));

        Assert.Null(board.Summary());
    }

    [Fact]
    public void Toggling_watch_flips_and_persists()
    {
        var (board, _, _) = Open(t => t.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, Now.AddMinutes(12)), BossWorld.Dead(2, Now.AddMinutes(5)))));

        board.ToggleWatch(BossWorld.Linx);

        Assert.True(board.Rows().Single(r => r.NpcCode == BossWorld.Linx).Watch);
        Assert.True(_world.Timers().ForServer(BossWorld.Server).Single(t => t.NpcCode == BossWorld.Linx).Watch);
        board.ToggleWatch(BossWorld.Linx);
        Assert.False(board.Rows().Single(r => r.NpcCode == BossWorld.Linx).Watch);
    }

    [Theory]
    [InlineData(0, 1, 5)]
    [InlineData(55, 1, 60)]
    [InlineData(60, 1, 75)]
    [InlineData(75, -1, 60)]
    [InlineData(60, -1, 55)]
    [InlineData(5, -1, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(2880, 1, 2880)]
    public void Interval_steps_by_five_minutes_up_to_an_hour_then_by_fifteen(int from, int direction, int expected)
    {
        Assert.Equal(expected, BossBoard.Step(from, direction));
    }

    [Fact]
    public void Stepping_the_interval_persists_it_as_set_by_hand()
    {
        var (board, _, _) = Open(t => t.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, Now.AddMinutes(12)), BossWorld.Dead(2, Now.AddMinutes(5)))));

        for (var i = 0; i < 16; i++) board.StepInterval(BossWorld.Linx, 1);

        var saved = _world.Timers().ForServer(BossWorld.Server).Single(t => t.NpcCode == BossWorld.Linx);
        Assert.Equal(120, saved.RespawnMinutes);
        Assert.False(saved.RespawnLearned);
    }

    [Fact]
    public void Marking_dead_starts_the_countdown_and_removing_drops_the_row()
    {
        var (board, _, _) = Open(t =>
        {
            t.OnList(BossWorld.List(BossWorld.Server, BossWorld.Alive(1, Now.AddHours(-1)), BossWorld.Alive(2, Now.AddHours(-1))));
            t.SetRespawn(BossWorld.Server, BossWorld.Linx, 120);
        });

        board.MarkDead(BossWorld.Linx);

        var linx = board.Rows().Single(r => r.NpcCode == BossWorld.Linx);
        Assert.Equal(BossState.Countdown, linx.State);
        Assert.True(linx.Watch);
        board.Remove(BossWorld.Linx);
        Assert.DoesNotContain(board.Rows(), r => r.NpcCode == BossWorld.Linx);
    }

    [Fact]
    public void Poll_alerts_the_lead_time_before_a_watched_respawn_and_not_again()
    {
        var back = Now.AddMinutes(40);
        var timers = _world.Timers();
        timers.SetCurrentServer(BossWorld.Server);
        timers.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, back), BossWorld.Dead(2, back.AddHours(1))));
        timers.SetWatch(BossWorld.Server, BossWorld.Linx, true);
        var alert = new FakeBossAlert();
        var at = back.AddMinutes(-3);
        var board = new BossBoard(timers, _world.Data, alert, () => at);

        board.Poll(2);
        Assert.Empty(alert.Sent);

        at = back.AddMinutes(-2);
        board.Poll(2);
        var (title, body) = Assert.Single(alert.Sent);
        Assert.Contains("Linx", title);
        Assert.Contains("Altgard", body);
        Assert.Contains(back.ToString("HH:mm"), body);

        at = back.AddMinutes(-1);
        board.Poll(2);
        Assert.Single(alert.Sent);
    }

    [Fact]
    public void Poll_asks_the_timers_at_most_once_a_second()
    {
        var back = Now.AddMinutes(40);
        var timers = _world.Timers();
        timers.SetCurrentServer(BossWorld.Server);
        timers.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, back), BossWorld.Dead(2, back.AddHours(1))));
        timers.SetWatch(BossWorld.Server, BossWorld.Linx, true);
        var alert = new FakeBossAlert();
        var at = back.AddMinutes(-3);
        var board = new BossBoard(timers, _world.Data, alert, () => at);

        at = back.AddMinutes(-2).AddMilliseconds(-500);
        board.Poll(2);
        at = back.AddMinutes(-2);
        board.Poll(2);
        Assert.Empty(alert.Sent);

        at = back.AddMinutes(-2).AddSeconds(1);
        board.Poll(2);
        Assert.Single(alert.Sent);
    }

    [Fact]
    public void Poll_with_no_lead_never_alerts()
    {
        var back = Now.AddMinutes(1);
        var (board, alert, _) = Open(t =>
        {
            t.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, back), BossWorld.Dead(2, back.AddHours(1))));
            t.SetWatch(BossWorld.Server, BossWorld.Linx, true);
        });

        board.Poll(0);

        Assert.Empty(alert.Sent);
    }

    [Fact]
    public void Demo_boards_neither_persist_nor_notify()
    {
        var home = Path.Combine(_world.Directory, "demo-home");

        var board = BossBoard.Create(home, _world.Data, _world.Directory, demo: true, new FakeDesktop().Alert);
        board.Timers.OnNotice(new BossNotice(BossWorld.Linx, BossWorld.Map, "Altgard", BossNoticeKind.Killed, Now.ToUnixTimeMilliseconds(), ServerId: BossWorld.Server));
        board.Timers.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, Now.AddHours(1)), BossWorld.Dead(2, Now.AddHours(2))));

        Assert.False(Directory.Exists(home));
        Assert.IsType<NullBossAlert>(board.Alert);
    }

    [Fact]
    public void Live_boards_save_under_the_data_home_and_notify_the_desktop()
    {
        var home = Path.Combine(_world.Directory, "live-home");

        var desktop = new FakeDesktop();
        var board = BossBoard.Create(home, _world.Data, _world.Directory, demo: false, desktop.Alert);
        board.Timers.OnNotice(new BossNotice(BossWorld.Linx, BossWorld.Map, "Altgard", BossNoticeKind.Killed, Now.ToUnixTimeMilliseconds(), ServerId: BossWorld.Server));

        Assert.True(File.Exists(Path.Combine(home, "boss-timers.json")));
        Assert.Same(desktop.Alert, board.Alert);
    }

    [Fact]
    public void Session_feeds_boss_events_and_the_character_server_to_the_timers()
    {
        var timers = _world.Timers();
        var tracker = new CombatTracker(_world.Data, new MeterOptions());
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_world.Directory, "fights")), null, null, new HistoryPolicy(), timers);

        tracker.Process(new SelfIdentifiedEvent(0, 100, "Ilvane", BossWorld.Server, GameClass.Elementalist));
        tracker.Process(BossWorld.List(0, BossWorld.Dead(1, Now.AddHours(1)), BossWorld.Dead(2, Now.AddHours(2))));
        tracker.Process(new NpcSeenEvent(Now.ToUnixTimeMilliseconds(), 500, BossWorld.Gartua, 160_000_000));
        tracker.Tick(Now.ToUnixTimeMilliseconds());

        Assert.Equal(BossWorld.Server, timers.CurrentServer);
        Assert.Equal(2, timers.ForServer(BossWorld.Server).Count);
        session.Dispose();
    }

    [Fact]
    public void Session_without_timers_ignores_boss_events()
    {
        var tracker = new CombatTracker(_world.Data, new MeterOptions());
        using var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_world.Directory, "fights")), null, null, new HistoryPolicy());

        tracker.Process(BossWorld.List(0, BossWorld.Dead(1, Now.AddHours(1)), BossWorld.Dead(2, Now.AddHours(2))));
        tracker.Tick(Now.ToUnixTimeMilliseconds());

        Assert.False(File.Exists(_world.TimersFile));
    }
}

public sealed class DesktopBossAlertTests
{
    [Fact]
    public void Notify_runs_notify_send_and_the_alarm_sound_without_a_shell()
    {
        var started = new List<ProcessStartInfo>();
        var alert = new DesktopBossAlert(psi =>
        {
            started.Add(psi);
            return null;
        });

        alert.Notify("Reaparece Linx", "Altgard · 21:40");

        var notify = started[0];
        Assert.Equal("notify-send", notify.FileName);
        Assert.Equal(["--app-name", "AION meter", "--urgency=critical", "--", "Reaparece Linx", "Altgard · 21:40"], notify.ArgumentList);
        var sound = started[1];
        Assert.Equal("canberra-gtk-play", sound.FileName);
        Assert.Equal(["-i", "alarm-clock-elapsed"], sound.ArgumentList);
        Assert.All(started, psi =>
        {
            Assert.False(psi.UseShellExecute);
            Assert.True(psi.RedirectStandardInput);
            Assert.True(psi.RedirectStandardOutput);
            Assert.True(psi.RedirectStandardError);
        });
    }

    [Fact]
    public void A_missing_executable_is_given_up_after_the_first_failure()
    {
        var attempts = new List<string>();
        var alert = new DesktopBossAlert(psi =>
        {
            attempts.Add(psi.FileName);
            throw new Win32Exception(2);
        });

        alert.Notify("a", "b");
        alert.Notify("c", "d");

        Assert.Equal(["notify-send", "canberra-gtk-play"], attempts);
    }

    [Fact]
    public void One_missing_tool_does_not_stop_the_other()
    {
        var attempts = new List<string>();
        var alert = new DesktopBossAlert(psi =>
        {
            attempts.Add(psi.FileName);
            if (psi.FileName == "notify-send") throw new Win32Exception(2);
            return null;
        });

        alert.Notify("a", "b");
        alert.Notify("c", "d");

        Assert.Equal(["notify-send", "canberra-gtk-play", "canberra-gtk-play"], attempts);
    }
}
