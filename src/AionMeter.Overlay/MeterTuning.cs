using AionMeter.Core.Combat;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;

namespace AionMeter.Overlay;

public sealed class MeterTuning(MeterOptions meter, GameData data, string dataDirectory, MeterSession? session)
{
    public void Apply(UserSettings settings)
    {
        settings.ApplyTo(meter);
        if (session is not null) session.Policy = new HistoryPolicy(settings.HistoryMode, settings.HistoryMinSeconds, settings.SaveHistory);
        if (data.Language != settings.EffectiveNamesLanguage) data.ReloadNames(dataDirectory, settings.EffectiveNamesLanguage);
    }
}
