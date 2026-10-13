using AionMeter.Core.Text;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class TrayMenuTests
{
    private readonly List<OverlayCommand> _applied = [];
    private readonly List<string> _languages = [];
    private readonly FakeDesktop _desktop = new();
    private int _replays;

    private IReadOnlyList<TrayAction> Actions(string language, bool visible = true, bool locked = false) =>
        TrayMenu.Actions(UiText.For(language), new TrayState(visible, locked, language), new TrayHandlers(_applied.Add, _languages.Add, () => _replays++, _desktop));

    private TrayAction Item(string language, Func<UiText, string> header, bool visible = true, bool locked = false) =>
        Actions(language, visible, locked).Single(a => a.Header == header(UiText.For(language)));

    [Fact]
    public void The_menu_has_every_entry_of_the_windows_app_in_the_interface_language()
    {
        var es = UiText.For("es");

        Assert.Equal(
            [es.TrayHide, es.TrayClickThrough, es.HkReset, es.TrayHistory, es.TrayTimers, es.TraySettings, es.LanguageMenu, es.TrayDemo, es.TrayReplay, es.TrayTestAlert, es.TrayExit],
            Actions("es").Select(a => a.Header));
    }

    [Fact]
    public void Update_entries_come_before_exit_when_updates_are_handled()
    {
        var shown = 0;
        var checks = 0;
        var en = UiText.For("en");
        var actions = TrayMenu.Actions(en, new TrayState(true, false, "en", "Update to 1.1.0…"),
            new TrayHandlers(_applied.Add, _languages.Add, () => _replays++, _desktop, () => shown++, () => checks++));

        Assert.Equal(["Update to 1.1.0…", en.TrayCheckUpdates, en.TrayExit], actions.TakeLast(3).Select(a => a.Header));
        actions[^3].Run();
        actions[^2].Run();
        Assert.Equal((1, 1), (shown, checks));
    }

    [Fact]
    public void Hiding_from_the_tray_turns_the_entry_into_show()
    {
        Item("es", t => t.TrayHide).Run();

        Assert.Equal([OverlayCommand.Toggle], _applied);
        Assert.Equal(UiText.For("es").TrayShow, Actions("es", visible: false)[0].Header);
    }

    [Fact]
    public void Click_through_is_checked_while_the_overlay_is_locked()
    {
        Assert.Equal(true, Item("en", t => t.TrayClickThrough, locked: true).Checked);
        Assert.Equal(false, Item("en", t => t.TrayClickThrough).Checked);
    }

    [Fact]
    public void Each_entry_sends_its_overlay_command()
    {
        foreach (var header in new Func<UiText, string>[] { t => t.TrayClickThrough, t => t.HkReset, t => t.TrayHistory, t => t.TrayTimers, t => t.TraySettings, t => t.TrayDemo, t => t.TrayExit })
            Item("en", header).Run();

        Assert.Equal(
            [OverlayCommand.Lock, OverlayCommand.Reset, OverlayCommand.History, OverlayCommand.Show, OverlayCommand.Timers, OverlayCommand.Settings, OverlayCommand.Demo, OverlayCommand.Quit],
            _applied);
    }

    [Fact]
    public void The_language_submenu_marks_the_current_one_and_switches()
    {
        var submenu = Item("ru", t => t.LanguageMenu).Items!;

        Assert.Equal(["English", "Español", "Русский"], submenu.Select(i => i.Header));
        Assert.Equal([false, false, true], submenu.Select(i => i.Checked));

        submenu[1].Run();
        Assert.Equal(["es"], _languages);
    }

    [Fact]
    public void Replay_asks_for_a_file()
    {
        Item("en", t => t.TrayReplay).Run();

        Assert.Equal(1, _replays);
        Assert.Empty(_applied);
    }

    [Fact]
    public void Testing_the_alert_sends_a_boss_alert()
    {
        Item("es", t => t.TrayTestAlert).Run();

        Assert.Equal([(AppInfo.Name, "Probar el aviso de jefe")], _desktop.Notifications);
    }
}
