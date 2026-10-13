using System.Globalization;
using AionMeter.Core;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Storage;

namespace AionMeter.Overlay;

public sealed record RowView(uint ActorId, int Place, string Name, GameClass Class, bool IsSelf, double Bar, string Dps, string Damage, string Share, string Crit, string Level = "");

public sealed record HeaderView(string Title, string Time, string Totals, string Source, bool IsLive, bool Recording = false);

public sealed record SkillView(string Name, string Hits, string Damage, string Share, string Max, string Crit, string Back);

public sealed record DetailView(string Title, GameClass Class, string Quality, string Extremes, IReadOnlyList<SkillView> Skills, string Level = "");

public sealed record HistoryItemView(string Path, string When, string Title, string Result, string Zone, string Duration, string Players, string Dps, string Place, bool IsBoss);

public static class MeterView
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Compact(double value)
    {
        var abs = Math.Abs(value);
        return abs switch
        {
            >= 1_000_000_000 => (value / 1_000_000_000).ToString("0.00", Invariant) + "B",
            >= 1_000_000 => (value / 1_000_000).ToString("0.00", Invariant) + "M",
            >= 10_000 => (value / 1_000).ToString("0.0", Invariant) + "K",
            _ => value.ToString("0", Invariant),
        };
    }

    public static string Duration(long ms)
    {
        var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes}:{t.Seconds:00}";
    }

    public static string Percent(double fraction) => (fraction * 100).ToString("0", Invariant) + "%";

    public static string Level(int level) => level > 0 ? $"Nv {level}" : "";

    public static IReadOnlyList<string> Columns(ViewMode mode) =>
        mode == ViewMode.Healing ? ["Jugador", "HPS", "Curación", "%", ""] : ["Jugador", "DPS", "Daño", "%", "Crit"];

    public static IReadOnlyList<RowView> Rows(EncounterSnapshot? snapshot, int max, ViewMode mode = ViewMode.Damage)
    {
        if (snapshot is null || snapshot.Combatants.Count == 0) return [];
        return mode == ViewMode.Healing ? HealingRows(snapshot, max) : DamageRows(snapshot, max);
    }

    private static IReadOnlyList<RowView> DamageRows(EncounterSnapshot snapshot, int max)
    {
        var top = Math.Max(1, snapshot.Combatants.Max(c => c.Damage));
        return snapshot.Combatants
            .OrderByDescending(c => c.Damage)
            .Take(max)
            .Select((c, i) => new RowView(
                c.ActorId,
                i + 1,
                c.IsUnknownSummons ? "Invocaciones" : c.Name,
                c.Class,
                c.IsSelf,
                (double)c.Damage / top,
                Compact(c.Dps),
                Compact(c.Damage),
                Percent(c.Share),
                Percent(c.CritRate),
                Level(c.Level)))
            .ToList();
    }

    private static IReadOnlyList<RowView> HealingRows(EncounterSnapshot snapshot, int max)
    {
        var healers = snapshot.Combatants.Where(c => c.Healing > 0).OrderByDescending(c => c.Healing).ToList();
        if (healers.Count == 0) return [];
        var top = healers[0].Healing;
        var total = healers.Sum(c => c.Healing);
        return healers
            .Take(max)
            .Select((c, i) => new RowView(
                c.ActorId,
                i + 1,
                c.IsUnknownSummons ? "Invocaciones" : c.Name,
                c.Class,
                c.IsSelf,
                (double)c.Healing / top,
                Compact(c.Hps),
                Compact(c.Healing),
                Percent((double)c.Healing / total),
                "",
                Level(c.Level)))
            .ToList();
    }

    public static string? PartyLine(EncounterSnapshot? snapshot) =>
        snapshot is not null && snapshot.Combatants.Any(c => !c.IsUnknownSummons) ? ChatLine.Party(snapshot) : null;

    public static string? PlayerLine(EncounterSnapshot? snapshot, uint actorId) =>
        snapshot is not null && snapshot.Combatants.Any(c => c.ActorId == actorId) ? ChatLine.Player(snapshot, actorId) : null;

    public static string Rate(double? fraction) => fraction is { } f ? Percent(f) : "-";

    public static DetailView Detail(CombatantDetail detail, int maxSkills)
    {
        var q = detail.Quality;
        var quality = $"Golpes {Hits(q.Hits, q.DotTicks)} · Crit {Rate(q.CritRate)} · Espalda {Rate(q.BackRate)} · Perfecto {Rate(q.PerfectRate)}";
        var max = q.MaxHitSkill is { } skill ? $"Máx {Compact(q.MaxHit)} ({skill})" : $"Máx {Compact(q.MaxHit)}";
        var extremes = $"{max} · Recibido {Compact(detail.DamageTaken)} ({detail.HitsTaken} golpes)";
        var skills = detail.Skills
            .Take(maxSkills)
            .Select(s => new SkillView(
                s.Name,
                Hits(s.Hits, s.DotTicks),
                Compact(s.Damage),
                Percent(s.Share),
                Compact(s.Max),
                Rate(s.CritRate),
                Rate(s.BackRate)))
            .ToList();
        return new DetailView(detail.Name, detail.Class, quality, extremes, skills, Level(detail.Level));
    }

    private static string Hits(int hits, int dotTicks) => dotTicks > 0 ? $"{hits}+{dotTicks}" : hits.ToString(Invariant);

    public static HeaderView Header(EncounterSnapshot? snapshot, CaptureStatus status, int historyIndex, int historyCount, ViewMode mode = ViewMode.Damage, bool replay = false, bool recording = false)
    {
        var live = historyIndex < 0;
        var source = live ? (replay ? ReplayText(status) : StatusText(status)) : $"Historial {historyIndex + 1}/{historyCount}";
        if (snapshot is null)
            return new HeaderView(live ? "Esperando combate" : "Pelea no disponible", "0:00", "", source, live, recording);

        var title = snapshot.Boss?.Name ?? snapshot.Title;
        if (snapshot.Boss is { } boss && snapshot.IsActive) title += " · " + Percent(boss.HpFraction);
        if (!live) title = snapshot.StartedAt.ToLocalTime().ToString("dd/MM HH:mm ", Invariant) + title;
        var totals = mode == ViewMode.Healing ? HealingTotals(snapshot) : $"Total {Compact(snapshot.TotalDamage)} · Grupo {Compact(snapshot.PartyDps)}/s";
        return new HeaderView(title, Duration(snapshot.CombatMs), totals, source, live, recording);
    }

    private static string HealingTotals(EncounterSnapshot snapshot) =>
        $"Curación {Compact(snapshot.Combatants.Sum(c => c.Healing))} · Grupo {Compact(snapshot.Combatants.Sum(c => c.Hps))}/s";

    public static IReadOnlyList<HistoryItemView> History(IEnumerable<HistoryEntry> entries) =>
        entries.Select(e =>
            {
                var inFight = e.SelfPlace > 0;
                return new HistoryItemView(
                    e.Path,
                    e.StartedAt.ToLocalTime().ToString("dd/MM HH:mm", Invariant),
                    e.Title,
                    ResultSymbol(e.Reason),
                    e.Zone ?? "",
                    Duration(e.CombatMs),
                    e.Players == 1 ? "1 jugador" : $"{e.Players} jugadores",
                    inFight ? Compact(e.SelfDps) + "/s" : "Grupo " + Compact(e.PartyDps) + "/s",
                    inFight ? "#" + e.SelfPlace.ToString(Invariant) : "",
                    e.IsBoss);
            })
            .ToList();

    public static string ResultSymbol(EncounterEndReason reason) => reason switch
    {
        EncounterEndReason.Kill => "✔",
        EncounterEndReason.Wipe => "✖",
        _ => "•",
    };

    public static string ReplayText(CaptureStatus status) =>
        status.State == CaptureState.Stopped ? "Repetición terminada" : "Repetición";

    public const string GameClosedMessage = "AION 2 is not running";

    public static string StatusText(CaptureStatus status) => status.State switch
    {
        CaptureState.Capturing => "En vivo",
        CaptureState.WaitingForGame when status.Message.StartsWith(GameClosedMessage, StringComparison.Ordinal) => "Juego cerrado",
        CaptureState.WaitingForGame => "Esperando tráfico",
        CaptureState.Starting => "Iniciando",
        CaptureState.Error => "Error: " + status.Message,
        _ => "Sin captura",
    };
}
