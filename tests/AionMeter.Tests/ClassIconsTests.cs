using AionMeter.Core.Events;
using AionMeter.Overlay;
using Avalonia.Headless.XUnit;

namespace AionMeter.Tests;

public class ClassIconsTests
{
    [AvaloniaTheory]
    [InlineData(GameClass.Gladiator)]
    [InlineData(GameClass.Templar)]
    [InlineData(GameClass.Ranger)]
    [InlineData(GameClass.Assassin)]
    [InlineData(GameClass.Sorcerer)]
    [InlineData(GameClass.Elementalist)]
    [InlineData(GameClass.Cleric)]
    [InlineData(GameClass.Chanter)]
    [InlineData(GameClass.Brawler)]
    public void Every_class_has_an_icon(GameClass cls)
    {
        var icon = ClassIcons.For(cls);

        Assert.NotNull(icon);
        Assert.True(icon.PixelSize.Width > 0);
    }

    [AvaloniaFact]
    public void Unknown_class_has_no_icon()
    {
        Assert.Null(ClassIcons.For(GameClass.Unknown));
    }

    [AvaloniaFact]
    public void The_same_class_reuses_its_bitmap()
    {
        Assert.Same(ClassIcons.For(GameClass.Cleric), ClassIcons.For(GameClass.Cleric));
    }
}
