using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Core.Text;
using AionMeter.Overlay;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class BreakdownWindowTests : IDisposable
{
    private const uint Me = 100, Mate = 101, Third = 102, Boss = 500;

    private readonly string _dir = Directory.CreateTempSubdirectory("breakdown-window").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    [AvaloniaFact]
    public void Ver_detalle_in_the_skill_panel_opens_the_breakdown_of_that_player()
    {
        var (window, tracker) = Open();
        PartyFight(tracker);
        window.Refresh();
        window.UpdateLayout();
        Press(window, window.GetLogicalDescendants().OfType<RowControl>().First(r => r.IsVisible && r.ActorId == Mate));
        var detail = window.GetLogicalDescendants().OfType<SkillPanel>().Single().DetailButton;
        Assert.Null(window.Breakdown);

        Click(window, detail);

        Assert.NotNull(window.Breakdown);
        Assert.True(window.Breakdown!.IsVisible);
        Assert.Equal(Mate, window.Breakdown.ActorId);
        Assert.Equal("Details", detail.Content);
    }

    [AvaloniaFact]
    public void Breakdown_command_opens_the_own_character()
    {
        var (window, tracker) = Open();
        PartyFight(tracker);

        window.Apply(OverlayCommand.Breakdown);

        Assert.Equal(Me, window.Breakdown!.ActorId);
    }

    [AvaloniaFact]
    public void Skill_names_are_visible_at_the_default_size()
    {
        var (window, tracker) = Open();
        PartyFight(tracker);
        window.Apply(OverlayCommand.Breakdown);
        var breakdown = window.Breakdown!;
        breakdown.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        var names = breakdown.GetLogicalDescendants().OfType<Control>()
            .Where(c => c.Parent is Grid && (c is Button { Content: "Skill" } || (c is TextBlock { Text: { } text } && text.StartsWith("Skill 1", StringComparison.Ordinal))))
            .ToList();

        Assert.Equal(2, names.Select(c => c.GetType()).Distinct().Count());
        Assert.All(names, c =>
        {
            var column = ((Grid)c.Parent!).ColumnDefinitions[Grid.GetColumn(c)].ActualWidth;
            Assert.True(column >= 150, $"'{c}' column is {column} wide");
        });
    }

    [AvaloniaFact]
    public void Breakdown_command_without_a_fight_opens_nothing()
    {
        var (window, _) = Open();

        window.Apply(OverlayCommand.Breakdown);

        Assert.Null(window.Breakdown);
    }

    [AvaloniaFact]
    public void The_breakdown_is_a_normal_resizable_window_that_does_not_stay_on_top()
    {
        var (window, tracker) = Open();
        PartyFight(tracker);

        window.Apply(OverlayCommand.Breakdown);
        var breakdown = window.Breakdown!;

        Assert.False(breakdown.Topmost);
        Assert.Equal(SystemDecorations.Full, breakdown.SystemDecorations);
        Assert.True(breakdown.CanResize);
        Assert.Equal(["Skills", "Accuracy", "Timeline", "Rotation", "Defense"], breakdown.GetLogicalDescendants().OfType<TabItem>().Select(t => t.Header));
    }

    [AvaloniaFact]
    public void Choosing_another_player_in_the_list_switches_every_tab()
    {
        var (window, tracker) = Open();
        PartyFight(tracker);
        window.Apply(OverlayCommand.Breakdown);
        var breakdown = window.Breakdown!;
        breakdown.SelectTab(3);
        var list = breakdown.GetLogicalDescendants().OfType<ListBox>().Single(l => l.Name == "Players");
        Assert.Equal(3, list.ItemCount);

        list.SelectedIndex = 2;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Third, breakdown.ActorId);
        Assert.Equal("Mulo", breakdown.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "PlayerName").Text);
        Assert.Equal("600.0K", breakdown.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "TakenTotal").Text);
        Assert.Equal(2, breakdown.GetLogicalDescendants().OfType<RotationStrip>().First().Items.Count);
    }

    [AvaloniaFact]
    public void Each_fight_keeps_its_own_breakdown_window()
    {
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        history.Save(SavedFight("Berk", minute: 10, taken: 777_000));
        history.Save(SavedFight("Spider", minute: 12, taken: 5_000));
        var (window, _) = Open(history);

        window.Apply(OverlayCommand.Older);
        window.Apply(OverlayCommand.Breakdown);
        var spider = window.Breakdown!;
        window.Apply(OverlayCommand.Older);
        window.Apply(OverlayCommand.Breakdown);
        var berk = window.Breakdown!;

        Assert.NotSame(spider, berk);
        Assert.True(spider.IsVisible && berk.IsVisible);

        window.Apply(OverlayCommand.Newer);
        window.Apply(OverlayCommand.Breakdown);
        Assert.Same(spider, window.Breakdown);
    }

    [AvaloniaFact]
    public void A_saved_fight_shows_its_own_figures_and_ignores_later_navigation_in_the_overlay()
    {
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        history.Save(SavedFight("Berk", minute: 10, taken: 777_000));
        history.Save(SavedFight("Spider", minute: 12, taken: 5_000));
        var (window, _) = Open(history);
        window.Apply(OverlayCommand.Older);
        window.Apply(OverlayCommand.Skills);

        Click(window, window.GetLogicalDescendants().OfType<SkillPanel>().Single().DetailButton);
        var breakdown = window.Breakdown!;
        var taken = () => breakdown.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "TakenTotal").Text;
        Assert.Equal("5000", taken());

        window.Apply(OverlayCommand.Older);
        breakdown.Tick();
        breakdown.Tick();

        Assert.Equal("5000", taken());
        Assert.Equal(0, breakdown.RefreshCount);
    }

    [AvaloniaFact]
    public void Live_refresh_runs_at_most_once_per_second_and_asks_only_for_the_selected_player()
    {
        var clock = new FakeClock();
        var detail = TankDetail(Me, "Ilvane", taken: 1_000);
        var calls = new List<uint>();
        var source = new BreakdownSource(true, _ => Summary([detail]), id =>
        {
            calls.Add(id);
            return detail;
        });
        var breakdown = new BreakdownWindow(source, Me, 960, 640, () => clock.Ms);
        breakdown.Show();
        Dispatcher.UIThread.RunJobs();
        var opening = breakdown.RefreshCount;
        calls.Clear();

        breakdown.Tick();
        clock.Ms += 400;
        breakdown.Tick();
        clock.Ms += 400;
        breakdown.Tick();

        Assert.Equal(opening, breakdown.RefreshCount);
        clock.Ms += 200;
        breakdown.Tick();

        Assert.Equal(opening + 1, breakdown.RefreshCount);
        Assert.Equal([Me], calls.Distinct());
    }

    [AvaloniaFact]
    public void The_timeline_asks_for_the_party_only_while_its_tab_is_open()
    {
        var clock = new FakeClock();
        var mine = TankDetail(Me, "Ilvane", taken: 0);
        var other = TankDetail(Mate, "Brea", taken: 0);
        var calls = new List<uint>();
        var source = new BreakdownSource(true, _ => Summary([mine, other]), id =>
        {
            calls.Add(id);
            return id == Me ? mine : other;
        });
        var breakdown = new BreakdownWindow(source, Me, 960, 640, () => clock.Ms);
        breakdown.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(Mate, calls);

        breakdown.SelectTab(2);
        calls.Clear();
        clock.Ms += 1_000;
        breakdown.Tick();

        Assert.Contains(Mate, calls);
    }

    [AvaloniaFact]
    public void The_remembered_size_is_saved_when_the_breakdown_closes()
    {
        var (window, tracker) = Open();
        PartyFight(tracker);
        window.Apply(OverlayCommand.Breakdown);
        var breakdown = window.Breakdown!;

        breakdown.Width = 1_100;
        breakdown.Height = 700;
        breakdown.Close();

        var saved = UserSettings.Load(SettingsPath);
        Assert.Equal((1_100, 700), (saved.BreakdownWidth, saved.BreakdownHeight));
        Assert.Null(window.Breakdown);
    }

    [AvaloniaFact]
    public void A_header_click_sorts_the_skills_and_the_next_breakdown_keeps_the_order()
    {
        var (window, tracker) = Open();
        PartyFight(tracker);
        window.Apply(OverlayCommand.Breakdown);
        var breakdown = window.Breakdown!;

        Click(breakdown.GetLogicalDescendants().OfType<Button>().First(b => b.Name == "SortName"));

        Assert.Equal(new SkillOrder(SkillColumn.Name, false), breakdown.Order);
        Assert.Equal(UiText.Current.OvSkill + " ▴", breakdown.GetLogicalDescendants().OfType<Button>().First(b => b.Name == "SortName").Content);
        breakdown.Close();
        window.Apply(OverlayCommand.Breakdown);
        Assert.Equal(new SkillOrder(SkillColumn.Name, false), window.Breakdown!.Order);
    }

    [AvaloniaFact]
    public void The_breakdown_shows_the_player_tiles_the_accuracy_and_the_fight_line()
    {
        var (window, tracker) = Open();
        PartyFight(tracker);
        window.Apply(OverlayCommand.Breakdown);
        var breakdown = window.Breakdown!;

        Assert.Equal(5, breakdown.GetLogicalDescendants().OfType<WrapPanel>().Single(p => p.Name == "PlayerTiles").Children.Count);
        Assert.Equal(7, breakdown.GetLogicalDescendants().OfType<WrapPanel>().Single(p => p.Name == "AccuracyTiles").Children.Count);
        Assert.Contains(UiText.Current.PartyDpsSuffix, breakdown.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "FightMeta").Text);
        Assert.Contains(UiText.Current.You, breakdown.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "PlayerMeta").Text);
    }

    [AvaloniaFact]
    public async Task The_breakdown_copies_itself_as_an_image()
    {
        var (window, tracker) = Open();
        PartyFight(tracker);
        window.Apply(OverlayCommand.Breakdown);
        var breakdown = window.Breakdown!;
        breakdown.UpdateLayout();

        await breakdown.CopyImage();

        Assert.NotNull(breakdown.LastImage);
        Assert.True(breakdown.LastImage!.Value.Width > 100);
        Assert.Contains(breakdown.GetLogicalDescendants().OfType<TextBlock>(), t => t.Name == "CopyNote" && t.Text == UiText.Current.Copied);
    }

    private static void Click(Button button)
    {
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void The_breakdown_opens_with_the_remembered_size()
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy(HistoryMode.All, 20));
        var window = new OverlayWindow(session, new UserSettings { BreakdownWidth = 1_234, BreakdownHeight = 765 }, SettingsPath, null);
        window.Show();
        PartyFight(tracker);

        window.Apply(OverlayCommand.Breakdown);

        Assert.Equal((1_234, 765), (window.Breakdown!.Width, window.Breakdown.Height));
    }

    [AvaloniaFact]
    public void The_timeline_renders_a_ten_minute_fight_without_errors()
    {
        var perSecond = Enumerable.Range(0, 600).Select(s => 500L + s % 40 * 20).ToList();
        var mine = TankDetail(Me, "Ilvane", taken: 0, perSecond: perSecond, combatMs: 600_000);
        var mate = TankDetail(Mate, "Brea", taken: 0, perSecond: perSecond.Select(v => v / 2).ToList(), combatMs: 600_000);
        var chart = new TimelineChart { Width = 900, Height = 300 };
        chart.Show(BreakdownView.Timeline(mine, [mine, mate]));
        chart.Measure(new Size(900, 300));
        chart.Arrange(new Rect(0, 0, 900, 300));

        var exception = Record.Exception(() => new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(900, 300)).Render(chart));

        Assert.Null(exception);
        Assert.Equal(2, chart.DrawnSeries);
        Assert.Equal(1_200, chart.DrawnPoints);
    }

    [AvaloniaFact]
    public void The_rotation_draws_only_the_visible_casts_and_names_the_cast_under_the_pointer()
    {
        var casts = Enumerable.Range(0, 20_000).Select(i => new CastRecord(i * 100, 1, 1_000, HitFlags.None)).ToArray();
        var detail = TankDetail(Me, "Ilvane", taken: 0, casts: casts);
        var strip = new RotationStrip();
        strip.Show(BreakdownView.Rotation(detail));
        strip.SetViewport(400_000, 900);
        strip.Measure(Size.Infinity);
        strip.Arrange(new Rect(strip.DesiredSize));

        new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(900, 60)).Render(strip);

        Assert.InRange(strip.DrawnCount, 1, 60);
        Assert.Equal(20_000 * 40 - 4, strip.Width, 1);
        Assert.StartsWith("16:40.0  Skill 1\n1,000", strip.TooltipAt(400_010));
    }

    [AvaloniaFact]
    public void The_rotation_asks_for_icons_only_for_the_casts_it_draws()
    {
        var casts = Enumerable.Range(0, 5_000).Select(i => new CastRecord(i * 100, i % 3 + 1, 1_000, HitFlags.None)).ToArray();
        var strip = new RotationStrip();
        var asked = 0;
        strip.Icon = _ => { asked++; return null; };
        strip.Show(BreakdownView.Rotation(TankDetail(Me, "Ilvane", taken: 0, casts: casts)));
        strip.SetViewport(0, 900);
        strip.Measure(Size.Infinity);
        strip.Arrange(new Rect(strip.DesiredSize));

        new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(900, (int)RotationStrip.StripHeight)).Render(strip);

        Assert.Equal(strip.DrawnCount, asked);
        Assert.InRange(asked, 1, 60);
    }

    [Fact]
    public void Each_skill_keeps_its_own_color()
    {
        Assert.Equal(BreakdownPalette.Skill(11020000), BreakdownPalette.Skill(11020000));
        Assert.NotEqual(BreakdownPalette.Skill(11020000), BreakdownPalette.Skill(11020001));
    }

    [AvaloniaFact]
    public void Refreshing_twenty_thousand_casts_stays_under_one_hundred_milliseconds()
    {
        var clock = new FakeClock();
        var casts = Enumerable.Range(0, 20_000).Select(i => new CastRecord(i * 20, i % 7 + 1, 500 + i % 900, i % 5 == 0 ? HitFlags.Critical : HitFlags.None)).ToArray();
        var detail = TankDetail(Me, "Ilvane", taken: 0, casts: casts, combatMs: 400_000);
        var source = new BreakdownSource(true, _ => Summary([detail]), _ => detail);
        var breakdown = new BreakdownWindow(source, Me, 960, 640, () => clock.Ms);
        breakdown.Show();
        breakdown.SelectTab(3);
        Dispatcher.UIThread.RunJobs();
        clock.Ms += 1_000;
        breakdown.Tick();
        clock.Ms += 1_000;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        breakdown.Tick();
        Dispatcher.UIThread.RunJobs();
        watch.Stop();

        Assert.InRange(watch.ElapsedMilliseconds, 0, 100);
        Assert.Equal(20_000, breakdown.GetLogicalDescendants().OfType<RotationStrip>().First().Items.Count);
    }

    private sealed class FakeClock
    {
        public long Ms { get; set; } = 1_000_000;
    }

    private (OverlayWindow Window, CombatTracker Tracker) Open(HistoryStore? history = null)
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, history ?? new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy(HistoryMode.All, 20));
        var window = new OverlayWindow(session, new UserSettings(), SettingsPath, null);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, tracker);
    }

    private static void PartyFight(CombatTracker tracker)
    {
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new PlayerSeenEvent(0, Mate, "Brea", 1304, GameClass.Cleric));
        tracker.Process(new PlayerSeenEvent(0, Third, "Mulo", 1304, GameClass.Templar));
        tracker.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new DamageEvent(now - 3_000, Me, Boss, 16040000, 3_000, HitFlags.Critical));
        tracker.Process(new DamageEvent(now - 2_000, Me, Boss, 16040000, 3_000, HitFlags.None));
        tracker.Process(new DamageEvent(now - 2_000, Mate, Boss, 16040000, 2_000, HitFlags.None));
        tracker.Process(new DamageEvent(now - 1_500, Third, Boss, 16040000, 1_000, HitFlags.None));
        tracker.Process(new DamageEvent(now - 1_000, Third, Boss, 16040000, 1_000, HitFlags.Critical));
        tracker.Process(new DamageEvent(now - 900, Boss, Third, 1, 600_000, HitFlags.None));
    }

    private static CombatantDetail TankDetail(uint id, string name, long taken, IReadOnlyList<long>? perSecond = null, IReadOnlyList<CastRecord>? casts = null, long combatMs = 60_000) =>
        new(id, name, GameClass.Elementalist, id == Me, 0, 0, 0, combatMs, taken, 0,
            new HitQuality(0, 0, null, null, null, null, null, null, 0, null), [], perSecond ?? [], casts ?? []);

    private static EncounterSnapshot Summary(IReadOnlyList<CombatantDetail> details) =>
        new(Guid.NewGuid(), "Jefe", null, DateTimeOffset.UtcNow, 60_000, 60_000, true, EncounterEndReason.None, 0, 0, null,
            details.Select(d => new CombatantSnapshot(d.ActorId, d.Name, d.Class, d.IsSelf, 1_000, 10, 0.5, 5, 0.1, 100, 0)).ToList());

    private static FightRecord SavedFight(string title, int minute, long taken)
    {
        var detail = TankDetail(Me, "Ilvane", taken, perSecond: [100, 200], combatMs: 30_000);
        var summary = Summary([detail]) with
        {
            Title = title,
            StartedAt = new DateTimeOffset(2026, 10, 6, 18, minute, 0, TimeSpan.Zero),
            IsActive = false,
            Reason = EncounterEndReason.Kill,
        };
        return new FightRecord(1, summary, [detail]);
    }

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
}
