using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Storage;
using AionMeter.Overlay;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;

[assembly: AvaloniaTestApplication(typeof(AionMeter.Tests.HeadlessOverlay))]

namespace AionMeter.Tests;

public static class HeadlessOverlay
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<OverlayApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class OverlayWindowTests : IDisposable
{
    private const uint Me = 100, Boss = 500;

    private readonly string _dir = Directory.CreateTempSubdirectory("overlay-window").FullName;

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
        foreach (var world in _worlds) world.Dispose();
    }

    [AvaloniaFact]
    public void Heal_command_and_button_switch_between_damage_and_healing_views()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out var tracker);
        HealingFight(tracker);
        window.Apply(OverlayCommand.Live);
        Assert.Equal(["1. Ilvane", "2. Aurelia"], VisibleRows(window));

        window.Apply(OverlayCommand.Heal);
        Assert.Equal(["1. Aurelia"], VisibleRows(window));
        Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Healing" && t.IsEffectivelyVisible);

        Click(window, ButtonOf(window, "Healing"));
        Assert.Equal(["1. Ilvane", "2. Aurelia"], VisibleRows(window));
        Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Damage" && t.IsEffectivelyVisible);

        Click(window, ButtonOf(window, "Damage"));
        Assert.Equal(["1. Aurelia"], VisibleRows(window));
    }

    [AvaloniaFact]
    public void Healing_view_is_kept_while_browsing_the_history()
    {
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        var record = Fight("Berk", minute: 10);
        var healer = new CombatantSnapshot(9, "Aurelia", GameClass.Cleric, false, 0, 0, 0, 0, 0, 0, 0, Healing: 500, Hps: 50);
        history.Save(record with { Summary = record.Summary with { Combatants = [.. record.Summary.Combatants, healer] } });
        var window = Open(history, out _);

        window.Apply(OverlayCommand.Heal);
        window.Apply(OverlayCommand.Older);

        Assert.Equal(["1. Aurelia"], VisibleRows(window));
        window.Apply(OverlayCommand.Live);
        Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Healing" && t.IsEffectivelyVisible);
    }

    private static void HealingFight(CombatTracker tracker)
    {
        tracker.Process(new PlayerSeenEvent(0, 102, "Aurelia", 1304, GameClass.Cleric));
        LiveFight(tracker);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new HealEvent(now - 500, 102, Me, 20000001, 40_000, HitFlags.None));
    }

    private static List<string?> VisibleRows(Window window) =>
        window.GetLogicalDescendants().OfType<RowControl>().Where(r => r.IsVisible)
            .Select(r => r.GetLogicalDescendants().OfType<TextBlock>().First().Text).ToList();

    [AvaloniaFact]
    public void History_panel_reopens_over_saved_fights()
    {
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        history.Save(Fight("Berk", minute: 10));
        history.Save(Fight("Spider", minute: 12));
        var window = Open(history, out _);
        var button = ButtonOf(window, "History");

        Click(window, button);
        Click(window, button);
        Click(window, button);

        var list = window.GetLogicalDescendants().OfType<ListBox>().Single();
        Assert.True(list.IsEffectivelyVisible);
        Assert.Equal(2, list.ItemCount);
    }

    [AvaloniaFact]
    public void Clicking_a_player_row_opens_and_closes_their_skills()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out var tracker);
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new DamageEvent(now - 2_000, Me, Boss, 16040000, 1_000, HitFlags.Critical));
        tracker.Process(new DamageEvent(now - 1_000, Me, Boss, 16040000, 1_000, HitFlags.None));
        window.Apply(OverlayCommand.Live);
        var row = window.GetLogicalDescendants().OfType<RowControl>().First(r => r.IsVisible);
        var skills = window.GetLogicalDescendants().OfType<SkillPanel>().Single();

        Press(window, row);
        Assert.True(skills.IsVisible);

        Press(window, row);
        Assert.False(skills.IsVisible);
    }

    [AvaloniaFact]
    public void Rows_show_the_class_icon_and_the_self_level()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out var tracker);
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist, 45));
        tracker.Process(new PlayerSeenEvent(0, 101, "Nadie", 1304, GameClass.Unknown));
        tracker.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new DamageEvent(now - 2_000, Me, Boss, 16040000, 2_000, HitFlags.None));
        tracker.Process(new DamageEvent(now - 1_000, 101, Boss, 1, 1_000, HitFlags.None));
        window.Apply(OverlayCommand.Live);

        var rows = window.GetLogicalDescendants().OfType<RowControl>().Where(r => r.IsVisible).ToList();
        var self = rows.Single(r => r.ActorId == Me);
        var unknown = rows.Single(r => r.ActorId == 101);

        Assert.Same(ClassIcons.For(GameClass.Elementalist), self.ClassIcon.Source);
        Assert.Equal("Lv 45", self.LevelText.Text);
        Assert.True(self.LevelText.IsVisible);
        Assert.Null(unknown.ClassIcon.Source);
        Assert.False(unknown.LevelText.IsVisible);
    }

    [AvaloniaFact]
    public void Skill_panel_header_shows_the_class_icon_and_level()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out var tracker);
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist, 45));
        tracker.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new DamageEvent(now - 2_000, Me, Boss, 16040000, 1_000, HitFlags.None));
        window.Apply(OverlayCommand.Live);
        var row = window.GetLogicalDescendants().OfType<RowControl>().First(r => r.IsVisible);
        var skills = window.GetLogicalDescendants().OfType<SkillPanel>().Single();

        Press(window, row);

        Assert.Same(ClassIcons.For(GameClass.Elementalist), skills.ClassIcon.Source);
        Assert.Equal("Lv 45", skills.LevelText.Text);
    }

    [AvaloniaFact]
    public void Close_button_quits_and_hides_while_locked()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out _);
        var quits = 0;
        window.QuitRequested += () => quits++;
        var close = ButtonOf(window, "✕");

        window.Apply(OverlayCommand.Lock);
        Assert.False(close.IsVisible);

        window.Apply(OverlayCommand.Lock);
        Click(window, close);
        Assert.Equal(1, quits);
        Assert.True(File.Exists(Path.Combine(_dir, "overlay.json")));
    }

    [AvaloniaFact]
    public async Task Copy_button_puts_the_fight_on_the_clipboard()
    {
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        history.Save(Fight("Berk", minute: 10));
        var window = Open(history, out _);
        window.Apply(OverlayCommand.Older);
        var copy = ButtonOf(window, "Copy DPS");

        Click(window, copy);
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Berk 0:30 KILL | 1.Ilvane 100/s 100% | party 100/s", await window.Clipboard!.GetTextAsync());
        Assert.Equal("Copied ✓", copy.Content);
    }

    [AvaloniaFact]
    public async Task Copy_without_a_fight_leaves_the_clipboard_alone()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out _);
        await window.Clipboard!.SetTextAsync("previo");

        window.Apply(OverlayCommand.Copy);
        await Settle();

        Assert.Equal("previo", await window.Clipboard.GetTextAsync());
    }

    [AvaloniaFact]
    public async Task Skill_panel_copies_the_line_of_the_player_it_shows()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out var tracker);
        LiveFight(tracker);
        window.Apply(OverlayCommand.Skills);
        var copy = window.GetLogicalDescendants().OfType<SkillPanel>().Single().CopyButton;

        Click(window, copy);
        await Settle();

        Assert.StartsWith("NPC 2300243 0:01 | Ilvane #1 of 1: ", await window.Clipboard!.GetTextAsync());
        Assert.Equal("Copied ✓", copy.Content);
    }

    [AvaloniaFact]
    public async Task CopySelf_works_while_the_overlay_is_locked()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out var tracker);
        LiveFight(tracker);
        window.Apply(OverlayCommand.Lock);

        window.Apply(OverlayCommand.CopySelf);
        await Settle();

        Assert.Contains("Ilvane #1 of 1: ", await window.Clipboard!.GetTextAsync());
    }

    [AvaloniaFact]
    public void Mode_button_switches_to_all_targets_closing_the_running_fight()
    {
        var (window, meter, tracker, _) = OpenTuned();
        LiveFight(tracker);
        var mode = ButtonOf(window, "Bosses");

        Click(window, mode);

        Assert.Equal("All", mode.Content);
        Assert.Equal(TargetMode.All, meter.TargetMode);
        Assert.Equal(EncounterEndReason.Manual, tracker.Segments()[0].Reason);
        Assert.Equal(TargetMode.All, OverlaySettings.Load(SettingsPath).TargetMode);

        window.Apply(OverlayCommand.Mode);
        Assert.Equal("Bosses", mode.Content);
        Assert.Equal(TargetMode.BossOnly, meter.TargetMode);
    }

    [AvaloniaFact]
    public void Reload_applies_the_edited_file_and_keeps_the_running_fight()
    {
        var (window, _, tracker, data) = OpenTuned();
        LiveFight(tracker);
        new OverlaySettings(X: 5, Language: "es", LockedOpacity: 0.3, Locked: true).Save(SettingsPath);

        window.Apply(OverlayCommand.Reload);

        Assert.Equal("Bola de fuego", data.SkillName(16040000));
        Assert.Equal(2_000, tracker.Snapshot(null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())!.TotalDamage);
        window.Apply(OverlayCommand.Lock);
        Assert.Equal((byte)Math.Round(0.3 * 255), FrameAlpha(window));
    }

    [AvaloniaFact]
    public void Locked_background_uses_the_configured_opacity()
    {
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions());
        var session = new MeterSession(tracker, history, null, null, new HistoryPolicy());
        var window = new OverlayWindow(session, new OverlaySettings(Locked: true, LockedOpacity: 0.4), SettingsPath, null);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal((byte)Math.Round(0.4 * 255), FrameAlpha(window));
    }

    [AvaloniaFact]
    public void Unlocked_background_uses_the_configured_opacity()
    {
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions());
        var session = new MeterSession(tracker, history, null, null, new HistoryPolicy());
        var window = new OverlayWindow(session, new OverlaySettings(Opacity: 1), SettingsPath, null);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal((byte)255, FrameAlpha(window));
    }

    [AvaloniaFact]
    public void Footer_shows_the_shortcuts_to_unlock_and_hide()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out _);
        var hint = window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "ShortcutHint");
        Assert.False(hint.IsVisible);

        window.Shortcuts = new Dictionary<OverlayCommand, string> { [OverlayCommand.Lock] = "Super+Shift+F9", [OverlayCommand.Toggle] = "Super+Shift+F11" };
        Assert.True(hint.IsVisible);
        Assert.Equal("Super+Shift+F9 lock · Super+Shift+F11 hide", hint.Text);

        window.Apply(OverlayCommand.Lock);
        Assert.True(hint.IsVisible);
        Assert.Equal("Super+Shift+F9 unlock · Super+Shift+F11 hide", hint.Text);
    }

    [AvaloniaFact]
    public void Quit_stops_the_refresh_timer_before_shutting_down()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out _);
        var quits = 0;
        window.QuitRequested += () => quits++;
        Assert.True(window.Refreshing);

        window.Apply(OverlayCommand.Quit);

        Assert.False(window.Refreshing);
        Assert.Equal(1, quits);
    }

    [AvaloniaFact]
    public void Every_button_shows_the_hand_cursor()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out _);

        var buttons = window.GetLogicalDescendants().OfType<Button>().ToList();

        Assert.NotEmpty(buttons);
        Assert.All(buttons, b => Assert.NotNull(b.Cursor));
    }

    [AvaloniaFact]
    public void Timers_command_opens_and_closes_the_panel_with_a_row_per_boss()
    {
        var (window, _) = OpenWithBoard(Now.AddMinutes(12).AddSeconds(34));
        var panel = window.GetLogicalDescendants().OfType<BossPanel>().Single();
        Assert.False(panel.IsVisible);

        window.Apply(OverlayCommand.Timers);

        Assert.True(panel.IsVisible);
        Assert.Equal(2, panel.Rows.Count);
        var texts = panel.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains("Linx", texts);
        Assert.Contains("12:34", texts);
        Assert.Contains("Alive", texts);
        window.Apply(OverlayCommand.Timers);
        Assert.False(panel.IsVisible);
    }

    [AvaloniaFact]
    public void Panel_actions_change_the_timers_and_persist()
    {
        var (window, world) = OpenWithBoard(Now.AddMinutes(12));
        window.Apply(OverlayCommand.Timers);
        var panel = window.GetLogicalDescendants().OfType<BossPanel>().Single();
        var row = panel.Rows.Single(r => r.NpcCode == BossWorld.Linx);

        Click(window, row.WatchButton);
        Click(window, row.MoreButton);
        Click(window, row.MoreButton);

        var saved = world.Timers().ForServer(BossWorld.Server).Single(t => t.NpcCode == BossWorld.Linx);
        Assert.True(saved.Watch);
        Assert.Equal(10, saved.RespawnMinutes);

        Click(window, row.DeadButton);
        Assert.True(world.Timers().ForServer(BossWorld.Server).Single(t => t.NpcCode == BossWorld.Linx).LastKill is not null);

        Click(window, row.RemoveButton);
        Assert.DoesNotContain(panel.Rows, r => r.NpcCode == BossWorld.Linx);
        Assert.DoesNotContain(world.Timers().ForServer(BossWorld.Server), t => t.NpcCode == BossWorld.Linx);
    }

    [AvaloniaFact]
    public void Panel_actions_are_hidden_while_the_overlay_is_locked()
    {
        var (window, _) = OpenWithBoard(Now.AddMinutes(12));
        window.Apply(OverlayCommand.Timers);
        var row = window.GetLogicalDescendants().OfType<BossPanel>().Single().Rows.First();
        Assert.True(row.WatchButton.IsEffectivelyVisible);

        window.Apply(OverlayCommand.Lock);

        Assert.True(row.IsEffectivelyVisible);
        Assert.False(row.WatchButton.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Summary_line_shows_the_next_watched_boss_with_the_panel_closed_and_hides_without_one()
    {
        var (window, _) = OpenWithBoard(Now.AddMinutes(12).AddSeconds(34));
        window.Apply(OverlayCommand.Lock);
        Assert.DoesNotContain(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.IsEffectivelyVisible && t.Text!.StartsWith("Next"));

        _board!.ToggleWatch(BossWorld.Linx);
        window.Apply(OverlayCommand.Reset);

        Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.IsEffectivelyVisible && t.Text == "Next: Linx 12:34");
    }

    [AvaloniaFact]
    public void Dragging_the_grip_resizes_the_window_and_saves_the_width()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out _);
        var grip = window.GetLogicalDescendants().OfType<Border>().Single(b => b.Name == "WidthGrip");
        var start = grip.TranslatePoint(new Point(4, 4), window)!.Value;
        var before = window.Width;

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(100, 0));
        window.MouseUp(start + new Point(100, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        window.SaveSettings();

        Assert.InRange(window.Width, before + 95, before + 105);
        Assert.Equal(window.Width, OverlaySettings.Load(Path.Combine(_dir, "overlay.json")).Width, 1);
    }

    [AvaloniaFact]
    public void The_grip_is_hidden_while_locked()
    {
        var window = Open(new HistoryStore(Path.Combine(_dir, "fights")), out _);
        var grip = window.GetLogicalDescendants().OfType<Border>().Single(b => b.Name == "WidthGrip");
        Assert.True(grip.IsEffectivelyVisible);

        window.Apply(OverlayCommand.Lock);

        Assert.False(grip.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Game_closing_hides_the_window_and_the_game_opening_shows_it()
    {
        var rig = OpenAuto(new OverlaySettings());

        rig.Source.Set(GameClosed);
        rig.Window.Refresh();
        Assert.False(rig.Window.IsVisible);

        rig.Source.Set(Capturing);
        rig.Window.Refresh();
        Assert.True(rig.Window.IsVisible);
    }

    [AvaloniaFact]
    public void Started_hidden_stays_hidden_while_the_game_is_closed_and_shows_when_it_opens()
    {
        var rig = OpenAuto(new OverlaySettings(), startHidden: true, initial: GameClosed);

        rig.Window.Refresh();
        Assert.False(rig.Window.IsVisible);

        rig.Source.Set(Capturing);
        rig.Window.Refresh();
        Assert.True(rig.Window.IsVisible);
    }

    [AvaloniaFact]
    public void Locked_window_hides_after_the_idle_time_and_shows_again_on_a_fight_without_taking_focus()
    {
        var rig = OpenAuto(new OverlaySettings(Locked: true, AutoHideSeconds: 60));
        rig.Window.Refresh();

        rig.Clock.Advance(59);
        rig.Window.Refresh();
        Assert.True(rig.Window.IsVisible);

        rig.Clock.Advance(1);
        rig.Window.Refresh();
        Assert.False(rig.Window.IsVisible);

        LiveFight(rig.Tracker);
        rig.Window.Refresh();
        Assert.True(rig.Window.IsVisible);
        Assert.False(rig.Window.ShowActivated);
        Assert.False(rig.Window.IsActive);
    }

    [AvaloniaFact]
    public void Unlocked_window_never_auto_hides()
    {
        var rig = OpenAuto(new OverlaySettings(Locked: false, AutoHideSeconds: 60));

        rig.Clock.Advance(3_600);
        rig.Window.Refresh();

        Assert.True(rig.Window.IsVisible);
    }

    [AvaloniaFact]
    public void Toggle_hides_by_hand_and_a_new_fight_does_not_bring_the_window_back()
    {
        var rig = OpenAuto(new OverlaySettings());

        rig.Window.Apply(OverlayCommand.Toggle);
        Assert.False(rig.Window.IsVisible);

        LiveFight(rig.Tracker);
        rig.Window.Refresh();
        Assert.False(rig.Window.IsVisible);

        rig.Window.Apply(OverlayCommand.Show);
        Assert.True(rig.Window.IsVisible);
    }

    [AvaloniaFact]
    public void Game_restart_after_a_manual_hide_shows_the_window()
    {
        var rig = OpenAuto(new OverlaySettings());
        rig.Window.Apply(OverlayCommand.Toggle);

        rig.Source.Set(GameClosed);
        rig.Window.Refresh();
        rig.Source.Set(Capturing);
        rig.Window.Refresh();

        Assert.True(rig.Window.IsVisible);
    }

    [AvaloniaFact]
    public void Fights_keep_being_captured_and_saved_while_the_window_is_hidden_by_hand()
    {
        var rig = OpenAuto(new OverlaySettings());
        rig.Window.Apply(OverlayCommand.Toggle);
        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 80_000;

        rig.Source.Emit(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        rig.Source.Emit(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        for (var i = 0; i < 20; i++)
            rig.Source.Emit(new DamageEvent(start + i * 1_000, Me, Boss, 16040000, 1_000, HitFlags.None));
        rig.Window.Refresh();

        Assert.False(rig.Window.IsVisible);
        Assert.Single(rig.History.List());
        Assert.False(rig.Source.Stopped);
        Assert.False(rig.Source.Disposed);
    }

    [AvaloniaFact]
    public void History_filters_narrow_the_list_and_show_the_count_over_the_total()
    {
        var rig = OpenAuto(new OverlaySettings());
        rig.History.Save(Fight("A", minute: 1, boss: true));
        rig.History.Save(Fight("B", minute: 2, EncounterEndReason.Wipe, boss: true));
        rig.History.Save(Fight("C", minute: 3));
        rig.History.Save(Fight("D", minute: 4));
        Click(rig.Window, ButtonOf(rig.Window, "History"));
        var list = rig.Window.GetLogicalDescendants().OfType<ListBox>().Single();
        var count = rig.Window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "HistoryCount");
        Assert.Equal(4, list.ItemCount);
        Assert.Equal("4 of 4", count.Text);

        Click(rig.Window, ButtonOf(rig.Window, "Bosses only"));
        Assert.Equal(2, list.ItemCount);
        Assert.Equal("2 of 4", count.Text);

        Click(rig.Window, ButtonOf(rig.Window, "Kills only"));
        Assert.Equal(1, list.ItemCount);
        Assert.Equal("1 of 4", count.Text);

        rig.Window.Apply(OverlayCommand.Older);
        Assert.Equal("History 1/1", rig.Window.GetLogicalDescendants().OfType<TextBlock>().First(t => t.Text is { } text && text.StartsWith("History ")).Text);
    }

    [AvaloniaFact]
    public void Delete_asks_inline_and_confirming_removes_the_fight_and_shows_the_next_one()
    {
        var rig = OpenAuto(new OverlaySettings());
        var older = rig.History.Save(Fight("A", minute: 1));
        var newer = rig.History.Save(Fight("B", minute: 2));
        rig.Window.Apply(OverlayCommand.Older);
        var delete = ButtonOf(rig.Window, "Delete");
        Assert.True(delete.IsEffectivelyVisible);
        Assert.False(ButtonOf(rig.Window, "Yes").IsEffectivelyVisible);

        Click(rig.Window, delete);
        Assert.False(delete.IsVisible);
        Assert.True(ButtonOf(rig.Window, "Yes").IsEffectivelyVisible);
        Assert.True(ButtonOf(rig.Window, "No").IsEffectivelyVisible);
        Assert.True(File.Exists(newer));

        Click(rig.Window, ButtonOf(rig.Window, "Yes"));

        Assert.False(File.Exists(newer));
        Assert.True(File.Exists(older));
        Assert.Single(rig.History.List());
        Assert.True(delete.IsEffectivelyVisible);
        Assert.False(ButtonOf(rig.Window, "Yes").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Cancelling_the_delete_keeps_the_fight()
    {
        var rig = OpenAuto(new OverlaySettings());
        var path = rig.History.Save(Fight("A", minute: 1));
        rig.Window.Apply(OverlayCommand.Older);

        Click(rig.Window, ButtonOf(rig.Window, "Delete"));
        Click(rig.Window, ButtonOf(rig.Window, "No"));

        Assert.True(File.Exists(path));
        Assert.True(ButtonOf(rig.Window, "Delete").IsEffectivelyVisible);
        Assert.False(ButtonOf(rig.Window, "Yes").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void The_delete_confirmation_expires_after_five_seconds_without_deleting()
    {
        var rig = OpenAuto(new OverlaySettings());
        var path = rig.History.Save(Fight("A", minute: 1));
        rig.Window.Apply(OverlayCommand.Older);
        Click(rig.Window, ButtonOf(rig.Window, "Delete"));

        rig.Clock.Advance(4);
        rig.Window.Refresh();
        Assert.True(ButtonOf(rig.Window, "Yes").IsEffectivelyVisible);

        rig.Clock.Advance(1);
        rig.Window.Refresh();
        Assert.False(ButtonOf(rig.Window, "Yes").IsEffectivelyVisible);
        Assert.True(ButtonOf(rig.Window, "Delete").IsEffectivelyVisible);
        Assert.True(File.Exists(path));
    }

    [AvaloniaFact]
    public void Delete_is_not_offered_while_live()
    {
        var rig = OpenAuto(new OverlaySettings());
        rig.History.Save(Fight("A", minute: 1));

        Assert.False(ButtonOf(rig.Window, "Delete").IsVisible);

        rig.Window.Apply(OverlayCommand.Older);
        Assert.True(ButtonOf(rig.Window, "Delete").IsVisible);

        rig.Window.Apply(OverlayCommand.Live);
        Assert.False(ButtonOf(rig.Window, "Delete").IsVisible);
    }

    private sealed class FakeClock
    {
        public long Ms { get; private set; } = 1_000_000;

        public void Advance(int seconds) => Ms += seconds * 1_000L;
    }

    private sealed class FakeSource(CaptureStatus initial) : IEventSource
    {
        public event Action<GameEvent>? EventDecoded;
        public event Action<CaptureStatus>? StatusChanged;
        public CaptureStatus Status { get; private set; } = initial;
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }

        public void Set(CaptureStatus status)
        {
            Status = status;
            StatusChanged?.Invoke(status);
        }

        public void Emit(GameEvent e) => EventDecoded?.Invoke(e);

        public void Start()
        {
        }

        public void Stop() => Stopped = true;

        public void Dispose() => Disposed = true;
    }

    private sealed class FakeRecorder(CaptureStatus initial) : IEventSource, IRecordable
    {
        public event Action<GameEvent>? EventDecoded;
        public event Action<CaptureStatus>? StatusChanged;
        public CaptureStatus Status { get; } = initial;
        public bool IsRecording { get; private set; }
        public string? RecordingPath => IsRecording ? "recording.pcap" : null;
        public string? Directory { get; private set; }

        public void Emit(GameEvent e) => EventDecoded?.Invoke(e);

        public void StartRecording(string directory)
        {
            Directory = directory;
            IsRecording = true;
        }

        public void StopRecording() => IsRecording = false;

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }

    private (OverlayWindow Window, FakeRecorder Source, CombatTracker Tracker) OpenRecording(OverlaySettings settings)
    {
        var source = new FakeRecorder(Capturing);
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), source, null, new HistoryPolicy(HistoryMode.All, 20), capturesDirectory: Path.Combine(_dir, "captures"));
        session.Start();
        var window = new OverlayWindow(session, settings, SettingsPath, null);
        window.Show();
        window.Refresh();
        Dispatcher.UIThread.RunJobs();
        return (window, source, tracker);
    }

    private static TextBlock RecBadge(Window window) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "RecordingBadge");

    [AvaloniaFact]
    public void Record_command_toggles_the_recording_and_keeps_the_running_fight()
    {
        var (window, source, tracker) = OpenRecording(new OverlaySettings());
        LiveFight(tracker);
        var before = tracker.Snapshot(null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())!.TotalDamage;

        window.Apply(OverlayCommand.Record);

        Assert.True(source.IsRecording);
        Assert.Equal(Path.Combine(_dir, "captures"), source.Directory);
        Assert.Equal(before, tracker.Snapshot(null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())!.TotalDamage);
        Assert.NotNull(tracker.LiveFight());

        window.Apply(OverlayCommand.Record);

        Assert.False(source.IsRecording);
        Assert.NotNull(tracker.LiveFight());
    }

    [AvaloniaFact]
    public void The_rec_badge_shows_while_recording_locked_or_unlocked()
    {
        var (window, _, _) = OpenRecording(new OverlaySettings());
        Assert.False(RecBadge(window).IsEffectivelyVisible);

        window.Apply(OverlayCommand.Record);
        Assert.True(RecBadge(window).IsEffectivelyVisible);
        Assert.Contains("REC", RecBadge(window).Text);

        window.Apply(OverlayCommand.Lock);
        Assert.True(RecBadge(window).IsEffectivelyVisible);

        window.Apply(OverlayCommand.Record);
        Assert.False(RecBadge(window).IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Record_does_nothing_when_the_source_cannot_record()
    {
        var (window, _, _, _, _) = OpenAuto(new OverlaySettings());

        window.Apply(OverlayCommand.Record);

        Assert.False(RecBadge(window).IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void A_replay_is_marked_in_the_header_and_never_saves_fights()
    {
        var source = new FakeSource(Capturing);
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        var session = new MeterSession(tracker, history, source, null, new HistoryPolicy(HistoryMode.All, 1), replay: true);
        session.Start();
        var window = new OverlayWindow(session, new OverlaySettings(), SettingsPath, null);
        window.Show();

        source.Emit(new SelfIdentifiedEvent(1_000, Me, "Ilvane", 1304, GameClass.Elementalist));
        source.Emit(new DamageEvent(1_100, Me, Boss, 16040000, 9_000, HitFlags.None));
        source.Emit(new DamageEvent(8_000, Me, Boss, 16040000, 9_000, HitFlags.None));
        source.Set(new CaptureStatus(CaptureState.Stopped, "Replay finished"));
        window.Refresh();
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Replay finished");
        Assert.Empty(history.List());
        Assert.NotEmpty(VisibleRows(window));
    }

    private static readonly CaptureStatus Capturing = new(CaptureState.Capturing, "Capturing");
    private static readonly CaptureStatus GameClosed = new(CaptureState.WaitingForGame, "AION 2 is not running");

    private (OverlayWindow Window, FakeSource Source, CombatTracker Tracker, HistoryStore History, FakeClock Clock) OpenAuto(
        OverlaySettings settings, bool startHidden = false, CaptureStatus? initial = null)
    {
        var source = new FakeSource(initial ?? Capturing);
        var clock = new FakeClock();
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        var session = new MeterSession(tracker, history, source, null, new HistoryPolicy(HistoryMode.All, 20));
        session.Start();
        var window = new OverlayWindow(session, settings, SettingsPath, null, startHidden: startHidden, clock: () => clock.Ms);
        if (!startHidden) window.Show();
        window.Refresh();
        Dispatcher.UIThread.RunJobs();
        return (window, source, tracker, history, clock);
    }

    private static readonly DateTimeOffset Now = BossWorld.Now;

    private BossBoard? _board;

    private (OverlayWindow Window, BossWorld World) OpenWithBoard(DateTimeOffset linxBack)
    {
        var world = new BossWorld();
        _worlds.Add(world);
        var timers = world.Timers();
        timers.SetCurrentServer(BossWorld.Server);
        timers.OnList(BossWorld.List(BossWorld.Server, BossWorld.Dead(1, linxBack), BossWorld.Alive(2, Now.AddHours(-1))));
        _board = new BossBoard(timers, world.Data, new FakeBossAlert(), () => Now);
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy(HistoryMode.All, 20));
        var window = new OverlayWindow(session, new OverlaySettings(), Path.Combine(_dir, "overlay.json"), null, board: _board);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, world);
    }

    private readonly List<BossWorld> _worlds = [];

    private string SettingsPath => Path.Combine(_dir, "overlay.json");

    private (OverlayWindow Window, MeterOptions Meter, CombatTracker Tracker, GameData Data) OpenTuned()
    {
        var dataDir = Path.Combine(_dir, "data");
        Directory.CreateDirectory(Path.Combine(dataDir, "skills"));
        File.WriteAllText(Path.Combine(dataDir, "skills", "en.json"), """{"16040000": "Fireball"}""");
        File.WriteAllText(Path.Combine(dataDir, "skills", "es.json"), """{"16040000": "Bola de fuego"}""");
        var data = GameData.Load(dataDir);
        var meter = new MeterOptions();
        var tracker = new CombatTracker(data, meter);
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        var tuning = new MeterTuning(meter, data, dataDir, session);
        var window = new OverlayWindow(session, new OverlaySettings(), SettingsPath, null, tuning.Apply);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, meter, tracker, data);
    }

    private static byte FrameAlpha(OverlayWindow window) =>
        ((ISolidColorBrush)((Border)window.Content!).Background!).Color.A;

    private static void LiveFight(CombatTracker tracker)
    {
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new DamageEvent(now - 2_000, Me, Boss, 16040000, 1_000, HitFlags.Critical));
        tracker.Process(new DamageEvent(now - 1_000, Me, Boss, 16040000, 1_000, HitFlags.None));
    }

    private static async Task Settle()
    {
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();
    }

    private OverlayWindow Open(HistoryStore history, out CombatTracker tracker)
    {
        tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, history, null, null, new HistoryPolicy(HistoryMode.All, 20));
        var window = new OverlayWindow(session, new OverlaySettings(), Path.Combine(_dir, "overlay.json"), null);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Button ButtonOf(Window window, string text) =>
        window.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, text));

    private static void Click(Window window, Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static void Press(Window window, Control target)
    {
        var point = target.TranslatePoint(new Point(10, 10), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static FightRecord Fight(string title, int minute, EncounterEndReason reason = EncounterEndReason.Kill, bool boss = false)
    {
        var self = new CombatantSnapshot(Me, "Ilvane", GameClass.Elementalist, true, 1_000, 100, 1, 10, 0.5, 200, 0);
        var summary = new EncounterSnapshot(
            Guid.NewGuid(), title, "Zone", new DateTimeOffset(2026, 10, 6, 18, minute, 0, TimeSpan.Zero), 30_000, 30_000,
            false, reason, 1_000, 100, boss ? new BossSnapshot(Boss, 2300243, "Jefe", 0, 100) : null, [self]);
        return new FightRecord(1, summary, []);
    }
}
