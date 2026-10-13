using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Timers;

namespace AionMeter.Tests;

public sealed class BossTimerTests : IDisposable
{
    private const int Map = 1110, Linx = 2400419, Gartua = 2400800, ServerA = 1001, ServerB = 1002;

    private static readonly DateTimeOffset Now = new(2026, 10, 6, 21, 0, 0, TimeSpan.Zero);

    private readonly string _dir = Directory.CreateTempSubdirectory("boss-timers").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string TimersFile => Path.Combine(_dir, "boss-timers.json");

    private BossTimers Open(bool persistent = true)
    {
        var data = GameData.Empty;
        data.Npcs[Linx] = new NpcDef(Linx, "Linx", IsBoss: true, IsDummy: false);
        data.Npcs[Gartua] = new NpcDef(Gartua, "Gartua", IsBoss: true, IsDummy: false);
        File.WriteAllText(Path.Combine(_dir, "field_boss_maps.json"), """{"maps":{"1110":{"block":2400,"en":"Altgard"}}}""");
        return new BossTimers(TimersFile, data, _dir, persistent);
    }

    private static FieldBossListEvent List(int server, params FieldBossSlot[] slots) =>
        new(Now.ToUnixTimeMilliseconds(), Map, slots.Length, slots, [(byte)server, (byte)slots.Length, .. slots.Select(s => (byte)s.SlotId)], server);

    private static FieldBossSlot Dead(int place, DateTimeOffset back) => new(Map * 100 + place, false, back.ToUnixTimeMilliseconds());

    private static FieldBossSlot Alive(int place, DateTimeOffset since) => new(Map * 100 + place, true, since.ToUnixTimeMilliseconds());

    private static BossNotice Killed(int code, DateTimeOffset at, int server = ServerA) =>
        new(code, Map, "Altgard", BossNoticeKind.Killed, at.ToUnixTimeMilliseconds(), ServerId: server);

    private static BossNotice Seen(int code, BossNoticeKind kind, DateTimeOffset at, int server = ServerA) =>
        new(code, Map, "Altgard", kind, at.ToUnixTimeMilliseconds(), ServerId: server);

    [Fact]
    public void The_game_list_gives_the_respawn_time()
    {
        var timers = Open();
        var back = Now.AddMinutes(40);

        timers.OnList(List(ServerA, Dead(1, back), Alive(2, Now.AddHours(-1))));

        var linx = timers.ForServer(ServerA).Single(t => t.NpcCode == Linx);
        Assert.Equal(back.ToUnixTimeMilliseconds(), linx.NextSpawn!.Value.ToUnixTimeMilliseconds());
        Assert.True(timers.ForServer(ServerA).Single(t => t.NpcCode == Gartua).AliveNow);
    }

    [Fact]
    public void A_witnessed_kill_with_a_known_interval_sets_the_respawn()
    {
        var timers = Open();
        timers.OnNotice(Killed(Linx, Now));
        timers.SetRespawn(ServerA, Linx, 60);

        var linx = timers.ForServer(ServerA).Single();

        Assert.Equal(Now.AddMinutes(60).ToUnixTimeMilliseconds(), linx.NextSpawn!.Value.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void Timers_are_kept_per_server()
    {
        var timers = Open();
        timers.OnNotice(Killed(Linx, Now, ServerA));
        timers.OnNotice(Killed(Gartua, Now, ServerB));

        Assert.Equal([Linx], timers.ForServer(ServerA).Select(t => t.NpcCode));
        Assert.Equal([Gartua], timers.ForServer(ServerB).Select(t => t.NpcCode));
        Assert.Equal(ServerB, timers.CurrentServer);

        timers.SetCurrentServer(ServerA);

        Assert.Equal(ServerA, timers.CurrentServer);
        Assert.Equal([Gartua], timers.ForServer(ServerB).Select(t => t.NpcCode));
    }

    [Fact]
    public void A_passed_respawn_without_a_sighting_is_overdue()
    {
        var timers = Open();
        timers.OnList(List(ServerA, Dead(1, Now.AddMinutes(-3)), Dead(2, Now.AddMinutes(30))));

        var linx = timers.ForServer(ServerA).Single(t => t.NpcCode == Linx);

        Assert.True(linx.NextSpawn < Now);
        Assert.False(linx.AliveNow);
    }

    [Fact]
    public void Marking_dead_starts_the_countdown_from_the_interval()
    {
        var timers = Open();
        timers.OnList(List(ServerA, Alive(1, Now.AddHours(-1)), Alive(2, Now.AddHours(-1))));
        timers.SetRespawn(ServerA, Linx, 120);
        var before = DateTimeOffset.Now;

        timers.MarkKilled(ServerA, Linx);

        var linx = timers.ForServer(ServerA).Single(t => t.NpcCode == Linx);
        Assert.InRange(linx.NextSpawn!.Value, before.AddMinutes(120), DateTimeOffset.Now.AddMinutes(120));
        Assert.True(linx.Watch);
    }

    [Fact]
    public void A_watched_boss_alerts_once_per_respawn()
    {
        var timers = Open();
        var back = Now.AddMinutes(40);
        timers.OnList(List(ServerA, Dead(1, back), Dead(2, back.AddHours(1))));
        timers.SetWatch(ServerA, Linx, true);

        Assert.Empty(timers.TakeDueAlerts(back.AddMinutes(-3), 2));
        Assert.Equal([Linx], timers.TakeDueAlerts(back.AddMinutes(-2), 2).Select(t => t.NpcCode));
        Assert.Empty(timers.TakeDueAlerts(back.AddMinutes(-1), 2));
    }

    [Fact]
    public void An_unwatched_boss_does_not_alert()
    {
        var timers = Open();
        var back = Now.AddMinutes(40);
        timers.OnList(List(ServerA, Dead(1, back), Dead(2, back.AddHours(1))));

        Assert.Empty(timers.TakeDueAlerts(back.AddMinutes(-1), 2));
    }

    [Fact]
    public void A_boss_already_alive_does_not_alert()
    {
        var timers = Open();
        var back = Now.AddMinutes(40);
        timers.OnList(List(ServerA, Dead(1, back), Dead(2, back.AddHours(1))));
        timers.SetWatch(ServerA, Linx, true);
        timers.OnNotice(Seen(Linx, BossNoticeKind.Alive, back.AddMinutes(-1)));

        Assert.Empty(timers.TakeDueAlerts(back.AddMinutes(-1), 2));
    }

    [Fact]
    public void A_transient_instance_never_writes_the_file()
    {
        var timers = Open(persistent: false);

        timers.OnNotice(Killed(Linx, Now));

        Assert.False(File.Exists(TimersFile));
    }

    [Fact]
    public void A_persistent_instance_reloads_what_it_saved()
    {
        Open().OnNotice(Killed(Linx, Now));

        var timers = Open();

        Assert.Equal([Linx], timers.ForServer(ServerA).Select(t => t.NpcCode));
    }
}
