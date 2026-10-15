using AionMeter.Core.Capture;
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

public sealed class OverlayLayoutTests : IDisposable
{
    private const uint Me = 100, Boss = 500;

    private readonly string _dir = Directory.CreateTempSubdirectory("overlay-layout").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    private static UiText T => UiText.Current;

    private OverlayWindow Open(UserSettings? settings = null)
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new DamageEvent(now - 2_000, Me, Boss, 16040000, 1_000, HitFlags.Critical));
        tracker.Process(new DamageEvent(now - 1_000, Me, Boss, 16040000, 1_000, HitFlags.None));
        var window = new OverlayWindow(session, settings ?? new UserSettings(), SettingsPath, null)
        {
            SettingsSource = () => new SettingsContext(false, ["en"], [new(null, "Auto")], "", "", true, _ => { }, "", "", ""),
            Shortcuts = new Dictionary<OverlayCommand, string> { [OverlayCommand.Lock] = "Ctrl+L" },
        };
        window.Show();
        window.Apply(OverlayCommand.Live);
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static MenuItem Item(IEnumerable<MenuItem> items, string header) => items.Single(i => Equals(i.Header, header));

    private static IEnumerable<MenuItem> Children(MenuItem item) => item.Items.OfType<MenuItem>();

    private static void Toggle(OverlayWindow window, MenuItem item)
    {
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static bool Visible(OverlayWindow window, string text) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == text && t.IsEffectivelyVisible);

    private static Button? ButtonOf(OverlayWindow window, string text) =>
        window.GetLogicalDescendants().OfType<Button>().SingleOrDefault(b => Equals(b.Content, text));

    [AvaloniaFact]
    public void The_settings_button_sits_in_the_header_and_hides_while_locked()
    {
        var window = Open();

        Assert.Equal("⚙", window.LayoutButton.Content);
        Assert.True(window.LayoutButton.IsEffectivelyVisible);
        Assert.Single(window.GetLogicalDescendants().OfType<Button>(), b => Equals(b.Content, "⚙"));
        Assert.DoesNotContain(window.LayoutButton, window.GetLogicalDescendants().OfType<WrapPanel>().Single(p => p.Name == "Toolbar").Children);

        window.Apply(OverlayCommand.Lock);

        Assert.False(window.LayoutButton.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void The_menu_lists_every_part_checked_and_more_settings_last()
    {
        var window = Open();

        var items = window.LayoutMenu();

        Assert.Equal([T.LayoutPing, T.LayoutClock, T.LayoutTotals, T.LayoutSource, T.LayoutBossBar, T.LayoutColumns, T.LayoutButtons, T.LayoutShortcuts, T.LayoutMore], items.Select(i => i.Header));
        Assert.Equal(["DPS", T.RowDamage, "%", T.RowCrit], Children(Item(items, T.LayoutColumns)).Select(i => i.Header));
        Assert.Equal(
            [T.Live, T.LayoutBrowse, T.OvHistory, T.OvMySkills, T.OvReset, $"{T.RowDamage} / {T.OvHealing}", $"{T.OvAll} / {T.FilterBosses}", T.OvCopyDps, T.OvLock, T.LanguageMenu],
            Children(Item(items, T.LayoutButtons)).Select(i => i.Header));
        var toggles = items.Where(i => i.ToggleType == MenuItemToggleType.CheckBox)
            .Concat(Children(Item(items, T.LayoutColumns)))
            .Concat(Children(Item(items, T.LayoutButtons)))
            .ToList();
        Assert.Equal(Enum.GetValues<OverlayPart>().Length - 2, toggles.Count);
        Assert.All(toggles, i => Assert.True(i.IsChecked));
        Assert.All(toggles, i => Assert.True(i.StaysOpenOnClick));
    }

    [AvaloniaFact]
    public void Clicking_a_part_in_the_open_menu_hides_it_and_keeps_the_menu_open()
    {
        var window = Open();
        var gear = window.LayoutButton.TranslatePoint(new Point(4, 4), window)!.Value;
        window.MouseDown(gear, MouseButton.Left);
        window.MouseUp(gear, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        var flyout = (MenuFlyout)window.LayoutButton.Flyout!;
        Assert.True(flyout.IsOpen);
        var clock = Item(flyout.Items.OfType<MenuItem>(), T.LayoutClock);
        var popup = TopLevel.GetTopLevel(clock)!;
        var point = clock.TranslatePoint(new Point(10, 5), popup)!.Value;

        popup.MouseMove(point);
        popup.MouseDown(point, MouseButton.Left);
        popup.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.Settings.Shows(OverlayPart.Clock));
        Assert.False(clock.IsChecked);
        Assert.True(flyout.IsOpen);

        popup.MouseDown(point, MouseButton.Left);
        popup.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.Settings.Shows(OverlayPart.Clock));
        Assert.True(clock.IsChecked);
    }

    [AvaloniaFact]
    public void The_menu_reflects_the_saved_parts()
    {
        var settings = new UserSettings { ShowPing = false };
        settings.SetShown(OverlayPart.CritColumn, false);
        var window = Open(settings);

        var items = window.LayoutMenu();

        Assert.False(Item(items, T.LayoutPing).IsChecked);
        Assert.True(Item(items, T.LayoutClock).IsChecked);
        Assert.False(Item(Children(Item(items, T.LayoutColumns)), T.RowCrit).IsChecked);
    }

    [AvaloniaFact]
    public void Unchecking_header_parts_hides_them_and_saves_the_choice()
    {
        var window = Open();
        var time = window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "FightClock");
        Assert.True(time.IsEffectivelyVisible);
        var source = window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "Source");
        Assert.True(source.IsEffectivelyVisible);
        Assert.True(window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "ShortcutHint").IsEffectivelyVisible);

        var items = window.LayoutMenu();
        Toggle(window, Item(items, T.LayoutClock));
        Toggle(window, Item(items, T.LayoutTotals));
        Toggle(window, Item(items, T.LayoutSource));
        Toggle(window, Item(items, T.LayoutShortcuts));

        Assert.False(time.IsEffectivelyVisible);
        Assert.False(window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "Totals").IsEffectivelyVisible);
        Assert.False(source.IsEffectivelyVisible);
        Assert.False(window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "ShortcutHint").IsEffectivelyVisible);
        Assert.False(Item(window.LayoutMenu(), T.LayoutClock).IsChecked);
        Assert.Equal([OverlayPart.Clock, OverlayPart.Totals, OverlayPart.Source, OverlayPart.Shortcuts], UserSettings.Load(SettingsPath).HiddenParts);

        Toggle(window, Item(window.LayoutMenu(), T.LayoutClock));

        Assert.True(time.IsEffectivelyVisible);
        Assert.DoesNotContain(OverlayPart.Clock, UserSettings.Load(SettingsPath).HiddenParts);
    }

    [AvaloniaFact]
    public void A_hidden_column_leaves_the_header_and_every_row()
    {
        var window = Open();
        Assert.True(Visible(window, T.RowCrit));
        Assert.True(Visible(window, "50%"));

        Toggle(window, Item(Children(Item(window.LayoutMenu(), T.LayoutColumns)), T.RowCrit));

        Assert.False(Visible(window, T.RowCrit));
        Assert.False(Visible(window, "50%"));
        Assert.True(Visible(window, "100%"));
        Assert.True(Visible(window, "DPS"));
    }

    [AvaloniaFact]
    public void Hidden_buttons_leave_the_toolbar_and_all_of_them_hide_it()
    {
        var window = Open();
        var toolbar = window.GetLogicalDescendants().OfType<WrapPanel>().Single(p => p.Name == "Toolbar");
        var buttons = Children(Item(window.LayoutMenu(), T.LayoutButtons)).ToList();

        Toggle(window, Item(buttons, T.OvReset));

        Assert.False(ButtonOf(window, T.OvReset)!.IsEffectivelyVisible);
        Assert.True(ButtonOf(window, T.Live)!.IsEffectivelyVisible);
        Assert.True(toolbar.IsEffectivelyVisible);

        foreach (var item in buttons.Where(i => i.IsChecked)) Toggle(window, item);

        Assert.False(toolbar.IsEffectivelyVisible);
        Assert.Equal(buttons.Count, UserSettings.Load(SettingsPath).HiddenParts.Count);
    }

    [AvaloniaFact]
    public void The_ping_and_the_boss_bar_follow_their_own_settings()
    {
        var window = Open();
        var items = window.LayoutMenu();

        Toggle(window, Item(items, T.LayoutPing));
        Toggle(window, Item(items, T.LayoutBossBar));

        Assert.False(window.Settings.ShowPing);
        Assert.False(window.Settings.ShowBossPanel);
        Assert.False(window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "Ping").IsVisible);
        window.Apply(OverlayCommand.Settings);
        Dispatcher.UIThread.RunJobs();
        Assert.False(window.SettingsDialog!.ShowPing.IsChecked);
        Assert.False(window.SettingsDialog.ShowBoss.IsChecked);
    }

    [AvaloniaFact]
    public void More_settings_opens_the_settings_window()
    {
        var window = Open();

        Toggle(window, Item(window.LayoutMenu(), T.LayoutMore));

        Assert.NotNull(window.SettingsDialog);
    }

    [AvaloniaFact]
    public void Saving_the_settings_window_keeps_the_hidden_parts()
    {
        var window = Open();
        window.Apply(OverlayCommand.Settings);
        Dispatcher.UIThread.RunJobs();
        var dialog = window.SettingsDialog!;

        Toggle(window, Item(window.LayoutMenu(), T.LayoutClock));
        dialog.Save();
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.Settings.Shows(OverlayPart.Clock));
        Assert.False(window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "FightClock").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Saved_parts_are_hidden_when_the_overlay_opens()
    {
        var settings = new UserSettings();
        settings.SetShown(OverlayPart.Totals, false);
        settings.SetShown(OverlayPart.HistoryButton, false);

        var window = Open(UserSettings.Parse(settings.ToJson()));

        Assert.False(window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Name == "Totals").IsEffectivelyVisible);
        Assert.False(ButtonOf(window, T.OvHistory)!.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Settings_rows_keep_a_count_set_from_the_edge()
    {
        var window = Open(new UserSettings { MaxRows = 7 });
        window.Apply(OverlayCommand.Settings);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(7, window.SettingsDialog!.Collect().MaxRows);
    }

    [AvaloniaFact]
    public void The_width_grip_is_gone()
    {
        var window = Open();

        Assert.DoesNotContain(window.GetLogicalDescendants().OfType<Border>(), b => b.Name == "WidthGrip");
        Assert.DoesNotContain(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "⇔");
    }

    private static Point Middle(OverlayWindow window) => new(window.Bounds.Width / 2, window.Bounds.Height / 2);

    private static void Drag(OverlayWindow window, Point from, Vector by)
    {
        window.MouseMove(from);
        window.MouseDown(from, MouseButton.Left);
        window.MouseMove(from + by);
        window.MouseUp(from + by, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    [AvaloniaFact]
    public void Dragging_the_right_edge_widens_in_place_and_saves()
    {
        var window = Open();
        var before = (window.Width, window.Position);

        Drag(window, new Point(window.Bounds.Width - 2, Middle(window).Y), new Vector(100, 0));

        Assert.Equal(before.Width + 100, window.Width, 1);
        Assert.Equal(before.Position, window.Position);
        Assert.Equal(window.Width, UserSettings.Load(SettingsPath).OverlayWidth, 1);
    }

    [AvaloniaFact]
    public void Dragging_the_left_edge_widens_and_keeps_the_right_edge()
    {
        var window = Open();
        var before = (window.Width, window.Position);

        Drag(window, new Point(2, Middle(window).Y), new Vector(-100, 0));

        Assert.Equal(before.Width + 100, window.Width, 1);
        Assert.Equal(before.Position.X - 100, window.Position.X);
        Assert.Equal(before.Position.Y, window.Position.Y);
    }

    [AvaloniaFact]
    public void Dragging_the_bottom_edge_changes_the_rows_and_saves_them()
    {
        var window = Open();
        var pitch = window.GetLogicalDescendants().OfType<RowControl>().First().Bounds.Height + 2;

        Drag(window, new Point(Middle(window).X, window.Bounds.Height - 2), new Vector(0, 2 * pitch));

        Assert.Equal(12, window.Settings.MaxRows);
        Assert.Equal(12, window.GetLogicalDescendants().OfType<RowControl>().Count());
        Assert.Equal(12, UserSettings.Load(SettingsPath).MaxRows);
        Assert.Single(window.GetLogicalDescendants().OfType<RowControl>(), r => r.IsVisible);
    }

    [AvaloniaFact]
    public void Empty_rows_show_while_the_bottom_edge_is_dragged()
    {
        var window = Open(new UserSettings { MaxRows = 3 });
        var from = new Point(Middle(window).X, window.Bounds.Height - 2);
        var pitch = window.GetLogicalDescendants().OfType<RowControl>().First().Bounds.Height + 2;

        window.MouseMove(from);
        window.MouseDown(from, MouseButton.Left);
        window.MouseMove(from + new Vector(0, pitch));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(4, window.GetLogicalDescendants().OfType<RowControl>().Count(r => r.IsVisible));
        window.MouseUp(from + new Vector(0, pitch), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Single(window.GetLogicalDescendants().OfType<RowControl>(), r => r.IsVisible);
    }

    [AvaloniaFact]
    public void The_edge_under_the_pointer_is_tracked_for_the_cursor()
    {
        var window = Open();

        window.MouseMove(new Point(window.Bounds.Width - 2, Middle(window).Y));
        Assert.Equal(ResizeEdge.Right, window.EdgeUnderPointer);

        window.MouseMove(Middle(window));
        Assert.Equal(ResizeEdge.None, window.EdgeUnderPointer);
    }

    [AvaloniaFact]
    public void A_locked_overlay_does_not_resize()
    {
        var window = Open();
        window.Apply(OverlayCommand.Lock);
        var before = (window.Width, window.Settings.MaxRows);

        Drag(window, new Point(window.Bounds.Width - 2, window.Bounds.Height - 2), new Vector(100, 100));

        Assert.Equal(before, (window.Width, window.Settings.MaxRows));
        Assert.Equal(ResizeEdge.None, window.EdgeUnderPointer);
    }
}
