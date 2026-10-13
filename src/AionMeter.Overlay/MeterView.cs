using System.Globalization;
using AionMeter.Core;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Storage;
using AionMeter.Core.Text;

namespace AionMeter.Overlay;

public sealed record RowView(uint ActorId, int Place, string Name, GameClass Class, bool IsSelf, double Bar, string Dps, string Damage, string Share, string Crit, string Level = "", string GearScore = "", string CombatPower = "");

public sealed record HeaderView(string Title, string Time, string Totals, string Source, bool IsLive, bool Recording = false);

public sealed record SkillView(string Name, string Hits, string Damage, string Share, string Max, string Crit, string Back);

public sealed record DetailView(string Title, GameClass Class, string Quality, string Extremes, IReadOnlyList<SkillView> Skills, string Level = "");

public sealed record BossBarView(double Fraction, string Text, string Percent);

public sealed record LimitView(string Text, bool Urgent, string Tip);

public sealed record HistoryItemView(string Path, string When, string Title, string Result, string Zone, string Duration, string Players, string Dps, string Place, bool IsBoss, int BossCode = 0, int TargetCode = 0);

public static class MeterView
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static UiText T => UiText.Current;

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

    public static string Level(int level) => level > 0 ? string.Format(Invariant, T.OvLevel, level) : "";

    public static IReadOnlyList<string> Columns(ViewMode mode) =>
        mode == ViewMode.Healing ? [T.OvPlayer, "HPS", T.OvHealing, "%", ""] : [T.OvPlayer, "DPS", T.RowDamage, "%", T.RowCrit];

    public static IReadOnlyList<RowView> Rows(EncounterSnapshot? snapshot, int max, ViewMode mode = ViewMode.Damage, bool showGear = true, bool barsRelativeToTop = true)
    {
        if (snapshot is null || snapshot.Combatants.Count == 0) return [];
        var rows = mode == ViewMode.Healing ? HealingRows(snapshot, max, barsRelativeToTop) : DamageRows(snapshot, max, barsRelativeToTop);
        if (!showGear) return rows;
        var gear = snapshot.Combatants.ToDictionary(c => c.ActorId);
        return rows.Select(r => gear.TryGetValue(r.ActorId, out var c) ? r with { GearScore = GearScore(c.GearScore), CombatPower = CombatPower(c.CombatPower) } : r).ToList();
    }

    public static string GearScore(int value) => value > 0 ? value.ToString(Invariant) : "";

    public static string CombatPower(long value) => value > 0 ? Format.Power(value) : "";

    private static IReadOnlyList<RowView> DamageRows(EncounterSnapshot snapshot, int max, bool relativeToTop)
    {
        var top = Math.Max(1, snapshot.Combatants.Max(c => c.Damage));
        return snapshot.Combatants
            .OrderByDescending(c => c.Damage)
            .Take(max)
            .Select((c, i) => new RowView(
                c.ActorId,
                i + 1,
                c.IsUnknownSummons ? T.OvSummons : c.Name,
                c.Class,
                c.IsSelf,
                relativeToTop ? (double)c.Damage / top : c.Share,
                Compact(c.Dps),
                Compact(c.Damage),
                Percent(c.Share),
                Percent(c.CritRate),
                Level(c.Level)))
            .ToList();
    }

    private static IReadOnlyList<RowView> HealingRows(EncounterSnapshot snapshot, int max, bool relativeToTop)
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
                c.IsUnknownSummons ? T.OvSummons : c.Name,
                c.Class,
                c.IsSelf,
                relativeToTop ? (double)c.Healing / top : (double)c.Healing / total,
                Compact(c.Hps),
                Compact(c.Healing),
                Percent((double)c.Healing / total),
                "",
                Level(c.Level)))
            .ToList();
    }

    public static string? PartyLine(EncounterSnapshot? snapshot) => ChatMenu.PartyLine(snapshot);

    public static string? PlayerLine(EncounterSnapshot? snapshot, uint actorId) => ChatMenu.PlayerLine(snapshot, actorId);

    public static string Rate(double? fraction) => fraction is { } f ? Percent(f) : "-";

    public static DetailView Detail(CombatantDetail detail, int maxSkills)
    {
        var q = detail.Quality;
        var quality = string.Format(Invariant, T.OvQuality, Hits(q.Hits, q.DotTicks), Rate(q.CritRate), Rate(q.BackRate), Rate(q.PerfectRate));
        var max = q.MaxHitSkill is { } skill ? string.Format(Invariant, T.OvMaxHitSkill, Compact(q.MaxHit), skill) : string.Format(Invariant, T.OvMaxHit, Compact(q.MaxHit));
        var extremes = string.Format(Invariant, T.OvTakenSummary, max, Compact(detail.DamageTaken), detail.HitsTaken);
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
        var source = live ? (replay ? ReplayText(status) : StatusText(status)) : string.Format(Invariant, T.OvHistoryPosition, historyIndex + 1, historyCount);
        if (snapshot is null)
            return new HeaderView(live ? T.WaitingTitle : T.OvFightUnavailable, "0:00", "", source, live, recording);

        var title = snapshot.Boss?.Name ?? snapshot.Title;
        if (snapshot.Boss is { } boss && snapshot.IsActive) title += " · " + Percent(boss.HpFraction);
        if (!live) title = snapshot.StartedAt.ToLocalTime().ToString("dd/MM HH:mm ", Invariant) + title;
        var totals = mode == ViewMode.Healing ? HealingTotals(snapshot) : string.Format(Invariant, T.OvDamageTotals, Compact(snapshot.TotalDamage), Compact(snapshot.PartyDps));
        return new HeaderView(title, Duration(snapshot.CombatMs), totals, source, live, recording);
    }

    private static string HealingTotals(EncounterSnapshot snapshot) =>
        string.Format(Invariant, T.OvHealingTotals, Compact(snapshot.Combatants.Sum(c => c.Healing)), Compact(snapshot.Combatants.Sum(c => c.Hps)));

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
                    T.Players(e.Players),
                    inFight ? Compact(e.SelfDps) + "/s" : string.Format(Invariant, T.OvPartyRate, Compact(e.PartyDps)),
                    inFight ? "#" + e.SelfPlace.ToString(Invariant) : "",
                    e.IsBoss,
                    e.BossCode,
                    e.TargetCode);
            })
            .ToList();

    public static string ResultSymbol(EncounterEndReason reason) => reason switch
    {
        EncounterEndReason.Kill => "✔",
        EncounterEndReason.Wipe => "✖",
        _ => "•",
    };

    public static BossBarView? BossBar(EncounterSnapshot? snapshot)
    {
        if (snapshot?.Boss is not { MaxHp: > 0, Hp: >= 0 } boss) return null;
        var partial = boss switch
        {
            { MaxHpKnown: false, Hp: not 0 } => T.HpUnknown,
            { CountedFrom: { } from } => string.Format(Invariant, T.CountedFrom, Format.Percent(from, 0)),
            _ => null,
        };
        var text = boss.Hp == 0 ? T.Defeated : Format.Grouped(boss.Hp) + " HP" + (partial is null ? "" : " · " + partial);
        var percent = boss.MaxHpKnown || boss.Hp == 0 ? Format.Percent(boss.HpFraction) : "?";
        return new BossBarView(boss.HpFraction, text, percent);
    }

    public static LimitView? Limit(InstanceTimerEvent? timer, long nowMs)
    {
        if (timer is null) return null;
        var left = timer.EndMs - nowMs;
        var urgent = left < 60_000 || left < (timer.EndMs - timer.StartMs) / 10;
        var ends = DateTimeOffset.FromUnixTimeMilliseconds(timer.EndMs).ToLocalTime().ToString("HH:mm:ss", Invariant);
        var tip = string.Format(Invariant, timer.Kind == InstanceTimerEvent.BossLimit ? T.BossLimitTip : T.TimeLimitTip, ends);
        return new LimitView(Format.ClockShort(left + 999), urgent, tip);
    }

    public static string ReplayText(CaptureStatus status) =>
        status.State == CaptureState.Stopped ? T.OvReplayDone : T.OvReplay;

    public const string GameClosedMessage = "AION 2 is not running";

    public static string StatusText(CaptureStatus status) => status.State switch
    {
        CaptureState.Capturing => T.Live,
        CaptureState.WaitingForGame when status.Message.StartsWith(GameClosedMessage, StringComparison.Ordinal) => T.OvGameClosed,
        CaptureState.WaitingForGame => T.OvWaitingTraffic,
        CaptureState.Starting => T.OvStarting,
        CaptureState.Error => string.Format(Invariant, T.OvError, status.Message),
        _ => T.OvNoCapture,
    };
}
