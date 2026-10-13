using AionMeter.Core.Combat;
using AionMeter.Core.Game;

namespace AionMeter.Overlay;

public sealed class MeterTuning(MeterOptions meter, GameData data, string dataDirectory, MeterSession? session)
{
    public void Apply(OverlaySettings settings)
    {
        meter.TargetMode = settings.TargetMode;
        meter.IdleTimeoutMs = settings.IdleTimeoutSec * 1000;
        meter.BossIdleTimeoutMs = settings.BossIdleTimeoutSec * 1000;
        if (session is not null) session.Policy = new HistoryPolicy(settings.HistoryMode, settings.HistoryMinSeconds);
        if (data.Language != settings.Language) data.ReloadNames(dataDirectory, settings.Language);
    }
}
