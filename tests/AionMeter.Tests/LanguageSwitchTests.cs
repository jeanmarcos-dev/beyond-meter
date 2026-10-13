using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Core.Text;
using AionMeter.Overlay;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace AionMeter.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class InterfaceLanguageCollection
{
    public const string Name = "Interface language";
}

[Collection(InterfaceLanguageCollection.Name)]
public sealed class LanguageSwitchTests : IDisposable
{
    private const uint Me = 100, Boss = 500;

    private readonly string _dir = Directory.CreateTempSubdirectory("language-switch").FullName;
    private readonly BossWorld _world = new();

    public void Dispose()
    {
        UiText.Use("en");
        _world.Dispose();
        Directory.Delete(_dir, recursive: true);
    }

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    [AvaloniaFact]
    public void Choosing_russian_switches_the_overlay_breakdown_timers_and_tray_without_restarting()
    {
        var window = Open();
        var ru = UiText.For("ru");

        Choose(window, "Русский");

        Assert.Equal("RU", LanguageButton(window).Content);
        Assert.Contains(Buttons(window), b => Equals(b.Content, ru.OvHistory));
        Assert.Contains(Buttons(window), b => Equals(b.Content, ru.OvLock));
        Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == ru.OvPlayer);
        Assert.Contains(Buttons(window), b => Equals(b.Content, ru.BtnKilled));
        Assert.Contains(Buttons(window), b => Equals(b.Content, ru.OvRemove));
        var breakdown = window.Breakdown!;
        Assert.Equal($"{AppInfo.Name} · {ru.OvBreakdown}", breakdown.Title);
        Assert.Equal([ru.OvSkills, ru.OvTimeline, ru.TabRotation, ru.TabDefense], breakdown.GetLogicalDescendants().OfType<TabItem>().Select(t => t.Header));
        Assert.Equal(ru.TrayHide, TrayMenu.Actions(UiText.Current, visible: true, locked: false, _ => { }, new NullBossAlert())[0].Header);
        Assert.Equal("ru", UserSettings.Load(SettingsPath).Language);
    }

    [AvaloniaFact]
    public void The_language_menu_offers_the_three_languages_and_back_to_english()
    {
        var window = Open();

        Assert.Equal(["English", "Español", "Русский"], Menu(window).Select(i => i.Header));

        Choose(window, "Español");
        Assert.Contains(Buttons(window), b => Equals(b.Content, "Historial"));

        Choose(window, "English");
        Assert.Contains(Buttons(window), b => Equals(b.Content, "History"));
        Assert.Equal("EN", LanguageButton(window).Content);
    }

    [AvaloniaFact]
    public void Switching_language_raises_the_change_for_the_tray()
    {
        var window = Open();
        var changes = 0;
        window.LanguageChanged += () => changes++;

        Choose(window, "Español");

        Assert.Equal(1, changes);
    }

    [AvaloniaFact]
    public void Saving_russian_in_settings_switches_the_overlay_at_once()
    {
        var window = Open();
        window.SettingsSource = () => new SettingsContext(false, ["en"], [], "", "", false, _ => { }, "", "", "");
        window.Apply(OverlayCommand.Settings);
        var dialog = window.SettingsDialog!;

        dialog.LanguageBox.SelectedIndex = 2;
        dialog.Save();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("ru", UiText.Current.Code);
        Assert.Contains(Buttons(window), b => Equals(b.Content, UiText.For("ru").OvHistory));
        Assert.Equal("ru", UserSettings.Load(SettingsPath).Language);
    }

    private OverlayWindow Open()
    {
        var timers = _world.Timers();
        timers.SetCurrentServer(BossWorld.Server);
        timers.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, BossWorld.Now.AddMinutes(30))));
        var board = new BossBoard(timers, _world.Data, new NullBossAlert(), () => BossWorld.Now);
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy(HistoryMode.All, 20));
        var window = new OverlayWindow(session, new UserSettings(), SettingsPath, null, board: board);
        window.Show();
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        tracker.Process(new DamageEvent(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1_000, Me, Boss, 16040000, 3_000, HitFlags.Critical));
        window.Apply(OverlayCommand.Breakdown);
        window.Apply(OverlayCommand.Timers);
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Button LanguageButton(Window window) => Buttons(window).Single(b => b.Name == "LanguageButton");

    private static IEnumerable<Button> Buttons(Window window) => window.GetLogicalDescendants().OfType<Button>();

    private static IReadOnlyList<MenuItem> Menu(Window window) => ((MenuFlyout)LanguageButton(window).Flyout!).Items.OfType<MenuItem>().ToList();

    private static void Choose(Window window, string language)
    {
        Menu(window).Single(i => Equals(i.Header, language)).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }
}
