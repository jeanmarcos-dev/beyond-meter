using AionMeter.Overlay;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class AlertPopupTests
{
    [AvaloniaFact]
    public void The_popup_shows_the_alert_and_plays_the_sound()
    {
        var sounds = 0;
        var alert = new PopupAlert(() => sounds++);

        alert.Notify("Kromede", "Templo · 21:30");
        Dispatcher.UIThread.RunJobs();

        var popup = Assert.Single(alert.Open);
        Assert.Equal("Kromede", popup.Heading.Text);
        Assert.Equal("Templo · 21:30", popup.Body.Text);
        Assert.True(popup.IsVisible);
        Assert.True(popup.Topmost);
        Assert.False(popup.ShowInTaskbar);
        Assert.Equal(1, sounds);
    }

    [AvaloniaFact]
    public void Popups_stack_and_a_closed_one_frees_its_place()
    {
        var alert = new PopupAlert();
        alert.Notify("a", "1");
        alert.Notify("b", "2");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal([0, 1], alert.Open.Select(p => p.Slot));

        alert.Open[0].Close();
        alert.Notify("c", "3");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([1, 0], alert.Open.Select(p => p.Slot));
    }

    [AvaloniaFact]
    public void A_click_closes_the_popup()
    {
        var alert = new PopupAlert();
        alert.Notify("a", "1");
        Dispatcher.UIThread.RunJobs();
        var popup = alert.Open[0];

        popup.MouseDown(new Avalonia.Point(10, 10), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(alert.Open);
        Assert.False(popup.IsVisible);
    }
}
