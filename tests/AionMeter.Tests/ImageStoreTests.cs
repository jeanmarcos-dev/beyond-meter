using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Overlay;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class ImageStoreTests : IDisposable
{
    private const uint Me = 100, Boss = 500;
    private const int Fireball = 16040000, BossCode = 2400419;

    private readonly string _dir = Directory.CreateTempSubdirectory("image-store").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Cache => Path.Combine(_dir, "cache");

    [AvaloniaFact]
    public void Cached_icons_and_portraits_load_and_missing_ones_stay_empty_without_downloading()
    {
        using var store = Cached();

        Assert.NotNull(store.SkillIcon(Fireball));
        Assert.NotNull(store.Portrait(BossCode, 0));
        Assert.NotNull(store.Portrait(0, BossCode));
        Assert.Null(store.SkillIcon(16050000));
        Assert.Null(store.Portrait(1234, 0));
        Assert.False(store.Enabled);
        Assert.Same(store.SkillIcon(Fireball), store.SkillIcon(Fireball));
    }

    [AvaloniaFact]
    public void The_breakdown_shows_skill_icons_and_the_boss_portrait()
    {
        using var store = Cached();
        var (window, tracker) = Open(store);
        Fight(tracker);
        window.Apply(OverlayCommand.Breakdown);
        Dispatcher.UIThread.RunJobs();
        var breakdown = window.Breakdown!;

        Assert.Contains(breakdown.GetLogicalDescendants().OfType<Image>(), i => i.Name == BreakdownWindow.SkillIconName && i.Source is not null);
        Assert.True(breakdown.GetLogicalDescendants().OfType<Image>().Single(i => i.Name == "FightPortrait").IsVisible);
    }

    [AvaloniaFact]
    public void The_history_list_and_the_boss_timers_show_portraits()
    {
        using var store = Cached();
        using var world = new BossWorld();
        var timers = world.Timers();
        timers.SetCurrentServer(BossWorld.Server);
        timers.OnNotice(new BossNotice(BossWorld.Linx, BossWorld.Map, "Altgard", BossNoticeKind.Killed, BossWorld.Now.ToUnixTimeMilliseconds(), ServerId: BossWorld.Server));
        var board = new BossBoard(timers, world.Data, new NullBossAlert(), () => BossWorld.Now);
        var history = new HistoryStore(Path.Combine(_dir, "fights"));
        history.Save(Saved());
        var session = new MeterSession(new CombatTracker(world.Data, new MeterOptions()), history, null, null, new HistoryPolicy());
        var window = new OverlayWindow(session, new UserSettings(), Path.Combine(_dir, "settings.json"), null, board: board) { Images = store };
        window.Show();

        window.Apply(OverlayCommand.History);
        window.Apply(OverlayCommand.Timers);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(window.GetLogicalDescendants().OfType<BossRowControl>(), r => r.Portrait.IsVisible);
        Assert.Contains(window.GetLogicalDescendants().OfType<ListBox>().Single().GetLogicalDescendants().OfType<Image>(), i => i.Source is not null);
    }

    [AvaloniaFact]
    public void Turning_downloads_on_and_off_in_settings_reaches_the_store()
    {
        using var store = Cached();
        var (window, _) = Open(store);

        var on = UserSettings.Parse(window.Settings.ToJson());
        on.DownloadIcons = false;
        window.UseSettings(on);
        Assert.False(store.Enabled);

        var off = UserSettings.Parse(window.Settings.ToJson());
        off.DownloadIcons = true;
        window.UseSettings(off);
        Assert.True(store.Enabled);
        store.Enabled = false;
    }

    private ImageStore Cached()
    {
        Directory.CreateDirectory(Path.Combine(Cache, "icons"));
        Directory.CreateDirectory(Path.Combine(Cache, "portraits"));
        CopyPng(Path.Combine(Cache, "icons", "ICON_EL_SKILL_004.png"));
        CopyPng(Path.Combine(Cache, "portraits", BossCode + ".webp"));
        return ImageStore.For(Path.Combine(_dir, "data"), Cache, enabled: false);
    }

    private static void CopyPng(string path)
    {
        using var source = AssetLoader.Open(new Uri("avares://aionmeter-overlay/Assets/Classes/cleric.png"));
        using var target = File.Create(path);
        source.CopyTo(target);
    }

    private (OverlayWindow Window, CombatTracker Tracker) Open(ImageStore store)
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        var window = new OverlayWindow(session, new UserSettings { DownloadIcons = false }, Path.Combine(_dir, "settings.json"), null) { Images = store };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, tracker);
    }

    private static void Fight(CombatTracker tracker)
    {
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new NpcSeenEvent(0, Boss, BossCode, 120_000_000));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new DamageEvent(now - 2_000, Me, Boss, Fireball, 3_000, HitFlags.None));
    }

    private static FightRecord Saved()
    {
        var summary = new EncounterSnapshot(Guid.NewGuid(), "Linx", "Altgard", new DateTimeOffset(2026, 10, 6, 18, 0, 0, TimeSpan.Zero), 60_000, 60_000, false,
            EncounterEndReason.Kill, 1, 1, new BossSnapshot(Boss, BossCode, "Linx", 0, 1_000), [new CombatantSnapshot(Me, "Ilvane", GameClass.Elementalist, true, 1_000, 10, 1, 5, 0, 300, 0)]);
        return new FightRecord(1, summary, []);
    }
}
