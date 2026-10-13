using AionMeter.Core.Combat;
using AionMeter.Core.Storage;

namespace AionMeter.Overlay;

public sealed record HistoryFilter(bool BossesOnly = false, bool KillsOnly = false)
{
    public static readonly HistoryFilter None = new();

    public bool IsActive => BossesOnly || KillsOnly;

    public bool Matches(HistoryEntry entry) =>
        (!BossesOnly || entry.IsBoss) && (!KillsOnly || entry.Reason == EncounterEndReason.Kill);

    public IReadOnlyList<HistoryEntry> Apply(IEnumerable<HistoryEntry> entries) => entries.Where(Matches).ToList();
}
