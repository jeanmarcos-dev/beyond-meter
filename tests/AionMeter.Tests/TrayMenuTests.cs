using AionMeter.Core.Text;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class TrayMenuTests
{
    private readonly List<OverlayCommand> _applied = [];
    private readonly FakeDesktop _desktop = new();

    private IReadOnlyList<TrayAction> Actions(string language, bool visible = true, bool locked = false) =>
        TrayMenu.Actions(UiText.For(language), visible, locked, _applied.Add, _desktop);

    [Fact]
    public void The_menu_follows_the_interface_language()
    {
        Assert.Equal(["Ocultar el overlay", "Clic a través", "Timers de jefes…", "Probar el aviso de jefe", "Salir"], Actions("es").Select(a => a.Header));
        Assert.Equal(["Hide overlay", "Click-through", "Boss timers…", "Test boss alert", "Exit"], Actions("en").Select(a => a.Header));
    }

    [Fact]
    public void A_hidden_overlay_offers_to_show_it()
    {
        Assert.Equal(UiText.For("en").TrayShow, Actions("en", visible: false)[0].Header);
    }

    [Fact]
    public void Click_through_is_checked_while_the_overlay_is_locked()
    {
        Assert.Equal(true, Actions("en", locked: true)[1].Checked);
        Assert.Equal(false, Actions("en")[1].Checked);
        Assert.All(Actions("en").Where((_, i) => i != 1), a => Assert.Null(a.Checked));
    }

    [Fact]
    public void Each_entry_sends_its_overlay_command()
    {
        var actions = Actions("en");

        foreach (var index in new[] { 0, 1, 2, 4 }) actions[index].Run();

        Assert.Equal([OverlayCommand.Toggle, OverlayCommand.Lock, OverlayCommand.Timers, OverlayCommand.Quit], _applied);
    }

    [Fact]
    public void Testing_the_alert_sends_a_boss_alert()
    {
        Actions("es")[3].Run();

        Assert.Equal([(AppInfo.Name, "Probar el aviso de jefe")], _desktop.Notifications);
        Assert.Empty(_applied);
    }
}
