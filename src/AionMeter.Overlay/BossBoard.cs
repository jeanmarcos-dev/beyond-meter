using System.Globalization;
using AionMeter.Core.Game;
using AionMeter.Core.Timers;

namespace AionMeter.Overlay;

public enum BossState
{
    Countdown,
    Overdue,
    Alive,
    Unknown,
}

public sealed record BossRowView(int NpcCode, string Name, string Zone, BossState State, string Status, string Detail, bool Watch, int RespawnMinutes, string Interval);

public sealed class BossBoard(BossTimers timers, GameData data, IBossAlert alert, Func<DateTimeOffset> clock)
{
    private const int MaxInterval = 48 * 60;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private DateTimeOffset _lastPoll = DateTimeOffset.MinValue;

    public BossTimers Timers => timers;

    public IBossAlert Alert => alert;

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

    public IReadOnlyList<BossRowView> Rows()
    {
        var now = clock();
        return timers.ForServer(timers.CurrentServer)
            .OrderBy(t => Rank(t, now))
            .ThenBy(t => t.NextSpawn ?? DateTimeOffset.MaxValue)
            .ThenBy(t => t.NpcCode)
            .Select(t => Row(t, now))
            .ToList();
    }

    public string? Summary()
    {
        var now = clock();
        var next = timers.ForServer(timers.CurrentServer)
            .Where(t => t.Watch && !t.AliveNow && t.NextSpawn is { } at && at > now)
            .OrderBy(t => t.NextSpawn)
            .FirstOrDefault();
        return next is null ? null : $"Próximo: {NameOf(next)} {Countdown(next.NextSpawn!.Value - now)}";
    }

    public void Poll(int leadMinutes)
    {
        var now = clock();
        if (now - _lastPoll < PollInterval) return;
        _lastPoll = now;
        foreach (var due in timers.TakeDueAlerts(now, leadMinutes))
            alert.Notify($"Reaparece {NameOf(due)}", $"{ZoneOf(due)} · {due.NextSpawn!.Value.ToLocalTime():HH:mm}");
    }

    public void ToggleWatch(int npcCode)
    {
        if (Find(npcCode) is { } t) timers.SetWatch(t.ServerId, npcCode, !t.Watch);
    }

    public void StepInterval(int npcCode, int direction)
    {
        if (Find(npcCode) is { } t) timers.SetRespawn(t.ServerId, npcCode, Step(t.RespawnMinutes, direction));
    }

    public void MarkDead(int npcCode)
    {
        if (Find(npcCode) is { } t) timers.MarkKilled(t.ServerId, npcCode, clock());
    }

    public void Remove(int npcCode)
    {
        if (Find(npcCode) is { } t) timers.Remove(t.ServerId, npcCode);
    }

    private BossTimer? Find(int npcCode) => timers.ForServer(timers.CurrentServer).FirstOrDefault(t => t.NpcCode == npcCode);

    private static int Rank(BossTimer t, DateTimeOffset now) => State(t, now) switch
    {
        BossState.Countdown => 0,
        BossState.Overdue => 1,
        BossState.Alive => 2,
        _ => 3,
    };

    private static BossState State(BossTimer t, DateTimeOffset now) =>
        t.AliveNow ? BossState.Alive
        : t.NextSpawn is { } next ? (next > now ? BossState.Countdown : BossState.Overdue)
        : BossState.Unknown;

    private BossRowView Row(BossTimer t, DateTimeOffset now)
    {
        var state = State(t, now);
        var (status, detail) = state switch
        {
            BossState.Alive => ("Vivo", t.AliveSince is { } since ? $"desde {When(since, now)}" : ""),
            BossState.Countdown => (Countdown(t.NextSpawn!.Value - now), $"a las {When(t.NextSpawn.Value, now)}"),
            BossState.Overdue => ("Debería estar vivo", $"desde {When(t.NextSpawn!.Value, now)}"),
            _ => ("Intervalo desconocido", ""),
        };
        var interval = t.RespawnMinutes > 0 ? (t.RespawnLearned ? "≈ " : "") + $"{t.RespawnMinutes} min" : "? min";
        return new BossRowView(t.NpcCode, NameOf(t), ZoneOf(t), state, status, detail, t.Watch, t.RespawnMinutes, interval);
    }

    private string NameOf(BossTimer t) => t.NpcCode > 0 ? data.NpcName(t.NpcCode) : $"Jefe #{-t.NpcCode % 100}";

    private string ZoneOf(BossTimer t)
    {
        var zone = timers.MapName(t.MapId, "en") ?? (t.MapId != 0 ? data.MapName(t.MapId) : t.Zone) ?? "";
        return zone == "Open world" ? "Mundo abierto" : zone;
    }

    private static string Countdown(TimeSpan left) =>
        left.TotalHours >= 1
            ? left.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : left.ToString(@"mm\:ss", CultureInfo.InvariantCulture);

    private static string When(DateTimeOffset at, DateTimeOffset now) =>
        at.ToLocalTime().Date == now.ToLocalTime().Date ? at.ToLocalTime().ToString("HH:mm") : at.ToLocalTime().ToString("dd.MM HH:mm");
}
