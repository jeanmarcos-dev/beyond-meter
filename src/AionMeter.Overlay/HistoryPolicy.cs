using AionMeter.Core.Combat;
using AionMeter.Core.Settings;

namespace AionMeter.Overlay;

public sealed record HistoryPolicy(HistoryMode Mode = HistoryMode.BossesAndLong, int MinSeconds = 20, bool Enabled = true)
{
    public const long MinFightMs = 5_000;

    public bool ShouldSave(EncounterSnapshot fight)
    {
        if (!Enabled || fight.CombatMs < MinFightMs) return false;
        var boss = fight.Boss is not null;
        return Mode switch
        {
            HistoryMode.BossesOnly => boss,
            HistoryMode.BossesAndLong => boss || fight.CombatMs >= MinSeconds * 1000L,
            _ => true,
        };
    }
}
