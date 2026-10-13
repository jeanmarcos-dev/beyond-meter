using System.Globalization;
using AionMeter.Core.Game;
using AionMeter.Core.Text;
using AionMeter.Core.Timers;

namespace AionMeter.Overlay;

public enum BossState
{
    Countdown,
    Overdue,
    Alive,
    Unknown,
}

public sealed record BossRowView(int NpcCode, string Name, string Zone, BossState State, string Status, string Detail, bool Watch, int RespawnMinutes, string Interval, string Death = "", bool Soon = false, string Source = "", string IntervalTip = "");

public sealed record ServerChoice(int Server, string Label, bool Shown);

public sealed class BossBoard(BossTimers timers, GameData data, IBossAlert alert, Func<DateTimeOffset> clock)
{
    private const int MaxInterval = 48 * 60;
    public static readonly int[] Presets = [0, 15, 30, 45, 60, 90, 120, 180, 240, 360, 480, 720, 1440];
    public static readonly int[] AlertChoices = [0, 1, 2, 5, 10];
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private static UiText T => UiText.Current;

    private DateTimeOffset _lastPoll = DateTimeOffset.MinValue;
    private int? _picked;

    public BossTimers Timers => timers;

    public IBossAlert Alert => alert;

    public bool WatchedOnly { get; set; }

    public int ShownServer => _picked ?? timers.CurrentServer;

    public void ShowServer(int server) => _picked = server == timers.CurrentServer ? null : server;

    public IReadOnlyList<ServerChoice> Servers()
    {
        var current = timers.CurrentServer;
        var servers = timers.Servers();
        if (servers.Count == 0) servers = [current];
        return servers.Select(s => new ServerChoice(s, ServerLabel(s) + (s == current ? "  · " + T.ServerYours : ""), s == ShownServer)).ToList();
    }

    public string ServerLabel(int server) => server == 0 ? T.ServerUnknown : $"{data.ServerName(server)} {GameData.RegionOf(server)}".Trim();

    public static IReadOnlyList<int> PresetsFor(int current) => Presets.Contains(current) ? Presets : [.. Presets.Append(current).Order()];

    public static string IntervalChoice(int minutes) => minutes == 0 ? T.RespawnUnknown : T.Duration(minutes);

    public static string AlertChoice(int minutes) => minutes == 0 ? T.AlertOff : T.Duration(minutes);

    public static BossBoard Create(string dataHome, GameData data, string dataDirectory, bool demo, IBossAlert alert)
    {
        if (!demo) Directory.CreateDirectory(dataHome);
        var timers = new BossTimers(Path.Combine(dataHome, "boss-timers.json"), data, dataDirectory, persistent: !demo);
        return new BossBoard(timers, data, demo ? new NullBossAlert() : alert, () => DateTimeOffset.Now);
    }

    public static int Step(int minutes, int direction)
    {
        var next = direction > 0 ? minutes + (minutes < 60 ? 5 : 15) : minutes - (minutes > 60 ? 15 : 5);
        return Math.Clamp(next, 0, MaxInterval);
    }

    public IReadOnlyList<BossRowView> Rows(int alertMinutes = 0)
    {
        var now = clock();
        return timers.ForServer(ShownServer)
            .Where(t => t.NpcCode > 0 && (!WatchedOnly || t.Watch))
            .OrderBy(t => Rank(t, now))
            .ThenBy(t => t.NextSpawn ?? DateTimeOffset.MaxValue)
            .ThenByDescending(t => t.LastKill ?? DateTimeOffset.MinValue)
            .ThenBy(t => t.NpcCode)
            .Select(t => Row(t, now, alertMinutes))
            .ToList();
    }

    public string? Summary()
    {
        var now = clock();
        var known = timers.ForServer(timers.CurrentServer).Where(t => t.NpcCode > 0).ToList();
        var pool = known.Any(t => t.Watch) ? known.Where(t => t.Watch) : known;
        var next = pool
            .Where(t => !t.AliveNow && t.NextSpawn is { } at && at > now)
            .OrderBy(t => t.NextSpawn)
            .FirstOrDefault();
        return next is null ? null : string.Format(CultureInfo.InvariantCulture, T.OvNextBoss, NameOf(next), Countdown(next.NextSpawn!.Value - now));
    }

    public void Poll(int leadMinutes)
    {
        var now = clock();
        if (now - _lastPoll < PollInterval) return;
        _lastPoll = now;
        foreach (var due in timers.TakeDueAlerts(now, leadMinutes))
            alert.Notify(T.TimersTitle, AlertText(due, now));
    }

    private string AlertText(BossTimer due, DateTimeOffset now)
    {
        var next = due.NextSpawn!.Value;
        var minutes = (int)Math.Ceiling((next - now).TotalMinutes);
        var at = next.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);
        return minutes > 0
            ? string.Format(CultureInfo.InvariantCulture, T.AlertSoon, NameOf(due), minutes, at)
            : string.Format(CultureInfo.InvariantCulture, T.AlertNow, NameOf(due), at);
    }

    public void ToggleWatch(int npcCode)
    {
        if (Find(npcCode) is { } t) timers.SetWatch(t.ServerId, npcCode, !t.Watch);
    }

    public void StepInterval(int npcCode, int direction)
    {
        if (Find(npcCode) is { } t) timers.SetRespawn(t.ServerId, npcCode, Step(t.RespawnMinutes, direction));
    }

    public void SetInterval(int npcCode, int minutes)
    {
        if (Find(npcCode) is { } t) timers.SetRespawn(t.ServerId, npcCode, Math.Clamp(minutes, 0, MaxInterval));
    }

    public void MarkDead(int npcCode)
    {
        if (Find(npcCode) is { } t) timers.MarkKilled(t.ServerId, npcCode, clock());
    }

    public void Remove(int npcCode)
    {
        if (Find(npcCode) is { } t) timers.Remove(t.ServerId, npcCode);
    }

    private BossTimer? Find(int npcCode) => timers.ForServer(ShownServer).FirstOrDefault(t => t.NpcCode == npcCode);

    private static int Rank(BossTimer t, DateTimeOffset now) => State(t, now) switch
    {
        BossState.Alive or BossState.Overdue => 0,
        BossState.Countdown => 1,
        _ => 2,
    };

    private static BossState State(BossTimer t, DateTimeOffset now) =>
        t.AliveNow ? BossState.Alive
        : t.NextSpawn is { } next ? (next > now ? BossState.Countdown : BossState.Overdue)
        : BossState.Unknown;

    private BossRowView Row(BossTimer t, DateTimeOffset now, int alertMinutes)
    {
        var state = State(t, now);
        var (status, detail) = state switch
        {
            BossState.Alive => (T.OvBossAlive, t.AliveSince is { } since ? string.Format(CultureInfo.InvariantCulture, T.TimerSince, When(since, now)) : ""),
            BossState.Countdown => (Countdown(t.NextSpawn!.Value - now), string.Format(CultureInfo.InvariantCulture, T.TimerAt, When(t.NextSpawn.Value, now))),
            BossState.Overdue => (T.OvShouldBeUp, string.Format(CultureInfo.InvariantCulture, T.TimerSince, When(t.NextSpawn!.Value, now))),
            _ => (T.OvUnknownInterval, t.FromGame && t.ListedTime is null ? T.TimerNoTime : T.TimerNeedsInterval),
        };
        var interval = t.RespawnMinutes > 0 ? (t.RespawnLearned ? "≈ " : "") + T.Duration(t.RespawnMinutes)
            : t.FromGame ? T.RespawnFromGame : T.RespawnUnknown;
        var death = t.LastKill is { } kill ? string.Format(CultureInfo.InvariantCulture, T.TimerKilled, When(kill, now))
            : t.LastSeenDead is { } dead ? string.Format(CultureInfo.InvariantCulture, T.TimerFoundDead, When(dead, now)) : "";
        var soon = state == BossState.Countdown && (t.NextSpawn!.Value - now).TotalMinutes <= Math.Max(1, alertMinutes);
        var source = t.FromGame && t.ListedAt is { } listed ? string.Format(CultureInfo.InvariantCulture, T.SourceGame, When(listed, now)) : T.SourceMeter;
        return new BossRowView(t.NpcCode, NameOf(t), ZoneOf(t), state, status, detail, t.Watch, t.RespawnMinutes, interval, death, soon, source, t.RespawnLearned ? T.TimerLearned : T.TipRespawn);
    }

    private string NameOf(BossTimer t) => t.NpcCode > 0 ? data.NpcName(t.NpcCode) : string.Format(CultureInfo.InvariantCulture, T.UnknownSlotBoss, -t.NpcCode % 100);

    private string ZoneOf(BossTimer t)
    {
        var zone = timers.MapName(t.MapId, T.Code)
                   ?? (t.SlotId != 0 ? string.Format(CultureInfo.InvariantCulture, T.MapNumber, t.MapId) : t.MapId != 0 ? data.MapName(t.MapId) : t.Zone) ?? "";
        return zone == "Open world" ? T.OpenWorld : zone;
    }

    private static string Countdown(TimeSpan left) =>
        left.TotalHours >= 1
            ? left.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : left.ToString(@"mm\:ss", CultureInfo.InvariantCulture);

    private static string When(DateTimeOffset at, DateTimeOffset now) =>
        at.ToLocalTime().Date == now.ToLocalTime().Date ? at.ToLocalTime().ToString("HH:mm") : at.ToLocalTime().ToString("dd.MM HH:mm");
}
