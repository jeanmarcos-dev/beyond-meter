using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Core.Text;
using AionMeter.Overlay;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class BossBarTests : IDisposable
{
    private const uint Me = 100, Boss = 500;

    private readonly string _dir = Directory.CreateTempSubdirectory("boss-bar").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static UiText T => UiText.Current;

    [Fact]
    public void The_bar_shows_hp_left_and_its_share()
    {
        var bar = MeterView.BossBar(With(new BossSnapshot(Boss, 1, "Berk", 135_382_562, 229_800_000)))!;

        Assert.Equal(135_382_562 / 229_800_000.0, bar.Fraction, 6);
        Assert.Equal("135,382,562 HP", bar.Text);
        Assert.Equal("58.9%", bar.Percent);
    }

    [Fact]
    public void A_dead_boss_reads_defeated()
    {
        var bar = MeterView.BossBar(With(new BossSnapshot(Boss, 1, "Berk", 0, 1_000)))!;

        Assert.Equal(T.Defeated, bar.Text);
        Assert.Equal("0.0%", bar.Percent);
    }

    [Fact]
    public void Hp_counted_from_part_of_the_fight_says_so()
    {
        var partial = MeterView.BossBar(With(new BossSnapshot(Boss, 1, "Berk", 500, 1_000, Uncounted: 200)))!;
        var unknown = MeterView.BossBar(With(new BossSnapshot(Boss, 1, "Berk", 500, 1_000, MaxHpKnown: false)))!;

        Assert.Equal("500 HP · " + string.Format(T.CountedFrom, "80%"), partial.Text);
        Assert.Equal("500 HP · " + T.HpUnknown, unknown.Text);
        Assert.Equal("?", unknown.Percent);
    }

    [Fact]
    public void No_boss_or_unknown_max_hp_shows_no_bar()
    {
        Assert.Null(MeterView.BossBar(null));
        Assert.Null(MeterView.BossBar(With(null)));
        Assert.Null(MeterView.BossBar(With(new BossSnapshot(Boss, 1, "Berk", 10, 0))));
    }

    [Fact]
    public void The_time_limit_counts_down_and_turns_urgent_near_the_end()
    {
        var timer = new InstanceTimerEvent(0, 1, 2, 0, 3_600_000);

        var early = MeterView.Limit(timer, 600_000)!;
        Assert.Equal("50:00", early.Text);
        Assert.False(early.Urgent);
        Assert.StartsWith(T.TimeLimitTip[..10], early.Tip);

        Assert.True(MeterView.Limit(timer, 3_300_000)!.Urgent);
        Assert.True(MeterView.Limit(new InstanceTimerEvent(0, 1, 2, 0, 300_000), 250_000)!.Urgent);
        Assert.Null(MeterView.Limit(null, 0));
    }

    [Fact]
    public void A_boss_time_limit_has_its_own_tip()
    {
        var limit = MeterView.Limit(new InstanceTimerEvent(0, 1, InstanceTimerEvent.BossLimit, 0, 300_000), 0)!;

        Assert.StartsWith(T.BossLimitTip[..10], limit.Tip);
    }

    [AvaloniaFact]
    public void The_overlay_shows_the_bar_and_limit_and_hides_the_bar_when_turned_off()
    {
        using var world = new BossWorld();
        var tracker = new CombatTracker(world.Data, new MeterOptions());
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        var window = new OverlayWindow(session, new UserSettings(), Path.Combine(_dir, "settings.json"), null);
        window.Show();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new NpcSeenEvent(0, Boss, BossWorld.Linx, 10_000));
        tracker.Process(new DamageEvent(now - 1_000, Me, Boss, 16040000, 2_500, HitFlags.None));
        tracker.Process(new NpcHpEvent(now - 900, Boss, 7_500, 10_000));
        tracker.Process(new InstanceTimerEvent(now, 1, 2, now, now + 600_000));
        window.Apply(OverlayCommand.Live);
        Dispatcher.UIThread.RunJobs();

        Assert.True(Named<Grid>(window, "BossBar").IsVisible);
        Assert.Equal("75.0%", Named<TextBlock>(window, "BossPercent").Text);
        Assert.True(Named<TextBlock>(window, "TimeLimit").IsVisible);
        Assert.Equal("10:00", Named<TextBlock>(window, "TimeLimit").Text);

        var off = UserSettings.Parse(window.Settings.ToJson());
        off.ShowBossPanel = false;
        window.UseSettings(off);
        Assert.False(Named<Grid>(window, "BossBar").IsVisible);
    }

    private static T Named<T>(Window window, string name) where T : Control =>
        window.GetLogicalDescendants().OfType<T>().Single(c => c.Name == name);

    private static EncounterSnapshot With(BossSnapshot? boss) =>
        new(Guid.NewGuid(), "Berk", null, DateTimeOffset.UnixEpoch, 10_000, 10_000, true, EncounterEndReason.None, 1, 1, boss, []);
}
