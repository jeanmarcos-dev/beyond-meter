using AionMeter.Core.Events;

namespace AionMeter.Overlay;

public static class ClassColors
{
    private static readonly Dictionary<GameClass, uint> Bars = new()
    {
        [GameClass.Gladiator] = 0x1FA2C8,
        [GameClass.Templar] = 0x3B7CE6,
        [GameClass.Ranger] = 0x1CAE7E,
        [GameClass.Assassin] = 0x46B84E,
        [GameClass.Sorcerer] = 0x864CE0,
        [GameClass.Elementalist] = 0xC23FC4,
        [GameClass.Cleric] = 0xD8A22A,
        [GameClass.Chanter] = 0xEA7D2C,
        [GameClass.Brawler] = 0xD6333E,
        [GameClass.Unknown] = 0x5E6B84,
    };

    public static uint Rgb(GameClass c) => Bars.TryGetValue(c, out var rgb) ? rgb : Bars[GameClass.Unknown];
}
