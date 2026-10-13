using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Overlay;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class RowAppearanceTests : IDisposable
{
    private const uint Me = 100, Mate = 101, Boss = 500;

    private readonly string _dir = Directory.CreateTempSubdirectory("row-appearance").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    [Fact]
    public void Gear_score_and_combat_power_follow_the_option_and_need_a_value()
    {
        var snapshot = Fight();

        var rows = MeterView.Rows(snapshot, 5);
        Assert.Equal(("3120", "1.25M"), (rows[0].GearScore, rows[0].CombatPower));
        Assert.Equal(("", ""), (rows[1].GearScore, rows[1].CombatPower));

        var hidden = MeterView.Rows(snapshot, 5, showGear: false);
        Assert.All(hidden, r => Assert.Equal(("", ""), (r.GearScore, r.CombatPower)));
    }

    [Fact]
    public void Bars_are_relative_to_the_leader_or_show_the_share()
    {
        var snapshot = Fight();

        Assert.Equal([1.0, 0.5], MeterView.Rows(snapshot, 5).Select(r => r.Bar));
        Assert.Equal([2.0 / 3, 1.0 / 3], MeterView.Rows(snapshot, 5, barsRelativeToTop: false).Select(r => r.Bar), new ToleranceComparer());
    }

    [AvaloniaFact]
    public void Rows_show_gear_after_the_name_and_hide_it_when_turned_off()
    {
        var (window, tracker) = Open(new UserSettings());
        Play(tracker);
        window.Apply(OverlayCommand.Live);
        Dispatcher.UIThread.RunJobs();
        var top = Rows(window)[0];

        Assert.True(top.GearScoreText.IsVisible);
        Assert.True(top.CombatPowerText.IsVisible);
        Assert.False(Rows(window)[1].GearScoreText.IsVisible);

        var noGear = UserSettings.Parse(window.Settings.ToJson());
        noGear.ShowGear = false;
        window.UseSettings(noGear);
        Assert.False(top.GearScoreText.IsVisible);
    }

    [AvaloniaFact]
    public void Row_size_scales_height_and_text_and_is_kept()
    {
        var (window, _) = Open(new UserSettings { RowSize = 50 });

        var row = Rows(window)[0];
        Assert.Equal(12, row.Height);
        Assert.Equal(Math.Round(11 * Math.Sqrt(0.5), 1), row.LevelText.FontSize);
        Assert.Equal(row.Height - 4, row.ClassIcon.Width);

        window.ChangeRowSize(+1);
        Assert.Equal(55, window.Settings.RowSize);
        Assert.Equal(Math.Round(RowControl.BaseHeight * 0.55), row.Height);
        Assert.Equal(55, UserSettings.Load(SettingsPath).RowSize);

        window.ChangeRowSize(+100);
        Assert.Equal(100, window.Settings.RowSize);
    }

    [AvaloniaFact]
    public void Ctrl_and_the_mouse_wheel_over_the_rows_change_their_size()
    {
        var (window, tracker) = Open(new UserSettings());
        Play(tracker);
        window.Apply(OverlayCommand.Live);
        Dispatcher.UIThread.RunJobs();
        var row = Rows(window)[0];
        var point = row.TranslatePoint(new Point(10, 5), window)!.Value;

        window.MouseWheel(point, new Vector(0, -1), RawInputModifiers.Control);
        Assert.Equal(95, window.Settings.RowSize);

        window.MouseWheel(point, new Vector(0, -1));
        Assert.Equal(95, window.Settings.RowSize);
    }

    private (OverlayWindow Window, CombatTracker Tracker) Open(UserSettings settings)
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        var window = new OverlayWindow(session, settings, SettingsPath, null);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, tracker);
    }

    private static IReadOnlyList<RowControl> Rows(OverlayWindow window) => window.GetLogicalDescendants().OfType<RowControl>().ToList();

    private static EncounterSnapshot Fight() =>
        new(Guid.NewGuid(), "Berk", null, DateTimeOffset.UnixEpoch, 10_000, 10_000, true, EncounterEndReason.None, 2, 2, null,
        [
            new CombatantSnapshot(Me, "Ilvane", GameClass.Elementalist, true, 2_000, 200, 2.0 / 3, 10, 0.1, 500, 0, GearScore: 3120, CombatPower: 1_250_000),
            new CombatantSnapshot(Mate, "Brea", GameClass.Cleric, false, 1_000, 100, 1.0 / 3, 5, 0, 300, 0),
        ]);

    private static void Play(CombatTracker tracker)
    {
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new PartyRosterEvent(0, [new PartyMemberInfo("Ilvane", 1304, 45, 3120, 1_250_000, GameClass.Elementalist)]));
        tracker.Process(new PlayerSeenEvent(0, Mate, "Brea", 1304, GameClass.Cleric));
        tracker.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new DamageEvent(now - 2_000, Me, Boss, 16040000, 2_000, HitFlags.None));
        tracker.Process(new DamageEvent(now - 1_000, Mate, Boss, 16040000, 1_000, HitFlags.None));
    }

    private sealed class ToleranceComparer : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) < 1e-9;

        public int GetHashCode(double obj) => 0;
    }
}
