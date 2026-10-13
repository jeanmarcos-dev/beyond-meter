using AionMeter.Core.Combat;

namespace AionMeter.Overlay;

public enum HistoryMode
{
    BossesOnly,
    BossesAndLong,
    All,
}

public sealed record HistoryPolicy(HistoryMode Mode = HistoryMode.BossesAndLong, int MinSeconds = 20)
{
    public const long MinFightMs = 5_000;

    public bool ShouldSave(EncounterSnapshot fight)
    {
        if (fight.CombatMs < MinFightMs) return false;
        var boss = fight.Boss is not null;
        return Mode switch
        {
            HistoryMode.BossesOnly => boss,
            HistoryMode.BossesAndLong => boss || fight.CombatMs >= MinSeconds * 1000L,
            _ => true,
        };
    }
}
