using AionMeter.Core.Combat;

namespace AionMeter.Overlay;

public sealed class BreakdownSource(bool isLive, Func<long, EncounterSnapshot?> snapshot, Func<uint, CombatantDetail?> detail)
{
    public bool IsLive { get; } = isLive;

    public EncounterSnapshot? Snapshot(long nowMs) => snapshot(nowMs);

    public CombatantDetail? Detail(uint actorId) => detail(actorId);
}
