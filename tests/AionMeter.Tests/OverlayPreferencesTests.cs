using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Storage;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class OverlayPreferencesTests : IDisposable
{
    private const uint Me = 100, Boss = 500, Mob = 501;

    private readonly string _dir = Directory.CreateTempSubdirectory("overlay-prefs").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Empty_or_corrupt_files_give_the_documented_defaults()
    {
        var defaults = new OverlaySettings();

        Assert.Equal(defaults, OverlaySettings.Parse("{}"));
        Assert.Equal(defaults, OverlaySettings.Parse("{not json"));
        Assert.Equal(defaults, OverlaySettings.Parse("[1,2]"));
        Assert.Equal(TargetMode.BossOnly, defaults.TargetMode);
        Assert.Equal("en", defaults.Language);
        Assert.Equal(10, defaults.IdleTimeoutSec);
        Assert.Equal(30, defaults.BossIdleTimeoutSec);
        Assert.Equal(HistoryMode.BossesAndLong, defaults.HistoryMode);
        Assert.Equal(20, defaults.HistoryMinSeconds);
        Assert.Equal(0.55, defaults.LockedOpacity);
        Assert.Equal(0.85, defaults.Opacity);
        Assert.Equal(2, defaults.BossAlertMinutes);
        Assert.Equal(60, defaults.AutoHideSeconds);
        Assert.Equal(960, defaults.BreakdownWidth);
        Assert.Equal(640, defaults.BreakdownHeight);
    }

    [Theory]
    [InlineData("""{"BreakdownWidth": 1200}""", 1200)]
    [InlineData("""{"BreakdownWidth": 10}""", 480)]
    [InlineData("""{"BreakdownWidth": 99999}""", 3000)]
    [InlineData("""{"BreakdownWidth": "wide"}""", 960)]
    [InlineData("{}", 960)]
    public void Breakdown_width_is_clamped_to_its_range(string json, double expected)
    {
        Assert.Equal(expected, OverlaySettings.Parse(json).BreakdownWidth);
    }

    [Theory]
    [InlineData("""{"BreakdownHeight": 800}""", 800)]
    [InlineData("""{"BreakdownHeight": 10}""", 320)]
    [InlineData("""{"BreakdownHeight": 99999}""", 2000)]
    [InlineData("""{"BreakdownHeight": null}""", 640)]
    [InlineData("{}", 640)]
    public void Breakdown_height_is_clamped_to_its_range(string json, double expected)
    {
        Assert.Equal(expected, OverlaySettings.Parse(json).BreakdownHeight);
    }

    [Theory]
    [InlineData("""{"AutoHideSeconds": 90}""", 90)]
    [InlineData("""{"AutoHideSeconds": 0}""", 0)]
    [InlineData("""{"AutoHideSeconds": 9999}""", 600)]
    [InlineData("""{"AutoHideSeconds": -5}""", 0)]
    [InlineData("""{"AutoHideSeconds": "soon"}""", 60)]
    [InlineData("{}", 60)]
    public void Auto_hide_seconds_are_clamped_to_zero_through_six_hundred(string json, int expected)
    {
        Assert.Equal(expected, OverlaySettings.Parse(json).AutoHideSeconds);
    }

    [Theory]
    [InlineData("""{"LockedOpacity": 5}""", 1.0)]
    [InlineData("""{"LockedOpacity": 0.01}""", 0.15)]
    [InlineData("""{"LockedOpacity": 0.3}""", 0.3)]
    public void Opacity_is_clamped_to_its_range(string json, double expected)
    {
        Assert.Equal(expected, OverlaySettings.Parse(json).LockedOpacity);
    }

    [Theory]
    [InlineData("""{"Opacity": 5}""", 1.0)]
    [InlineData("""{"Opacity": 0.01}""", 0.15)]
    [InlineData("""{"Opacity": 1}""", 1.0)]
    public void Unlocked_opacity_is_clamped_to_its_range(string json, double expected)
    {
        Assert.Equal(expected, OverlaySettings.Parse(json).Opacity);
    }

    [Theory]
    [InlineData("""{"BossAlertMinutes": 5}""", 5)]
    [InlineData("""{"BossAlertMinutes": 0}""", 0)]
    [InlineData("""{"BossAlertMinutes": 99}""", 30)]
    [InlineData("""{"BossAlertMinutes": -4}""", 0)]
    [InlineData("""{"BossAlertMinutes": "soon"}""", 2)]
    [InlineData("{}", 2)]
    public void Boss_alert_lead_is_clamped_to_zero_through_thirty(string json, int expected)
    {
        Assert.Equal(expected, OverlaySettings.Parse(json).BossAlertMinutes);
    }

    [Fact]
    public void Timeouts_and_lengths_are_clamped_to_their_ranges()
    {
        var s = OverlaySettings.Parse("""{"IdleTimeoutSec": 1, "BossIdleTimeoutSec": 9999, "HistoryMinSeconds": 1, "MaxRows": 99}""");

        Assert.Equal(3, s.IdleTimeoutSec);
        Assert.Equal(300, s.BossIdleTimeoutSec);
        Assert.Equal(5, s.HistoryMinSeconds);
        Assert.Equal(20, s.MaxRows);
    }

    [Fact]
    public void A_bad_value_falls_back_alone_and_keeps_the_others()
    {
        var s = OverlaySettings.Parse("""{"HistoryMode": "Sometimes", "X": 3880, "Y": "tall", "TargetMode": "All", "Locked": "yes"}""");

        Assert.Equal(HistoryMode.BossesAndLong, s.HistoryMode);
        Assert.Equal(3880, s.X);
        Assert.Equal(new OverlaySettings().Y, s.Y);
        Assert.Equal(TargetMode.All, s.TargetMode);
        Assert.False(s.Locked);
    }

    [Theory]
    [InlineData("es", "es")]
    [InlineData("zh-Hans", "zh-Hans")]
    [InlineData("../../etc", "en")]
    [InlineData("", "en")]
    public void Language_must_look_like_a_language_code(string value, string expected)
    {
        Assert.Equal(expected, OverlaySettings.Parse($$"""{"Language": "{{value}}"}""").Language);
    }

    [Fact]
    public void Files_written_before_the_new_keys_still_load()
    {
        var s = OverlaySettings.Parse("""{"X": 3880, "Y": 240, "Width": 420, "Locked": true, "MaxRows": 12}""");

        Assert.Equal(new OverlaySettings(X: 3880, Y: 240, Width: 420, Locked: true), s);
    }

    [Fact]
    public void Saved_preferences_round_trip_with_readable_enums()
    {
        var path = Path.Combine(_dir, "overlay.json");
        var saved = new OverlaySettings(TargetMode: TargetMode.All, Language: "es", HistoryMode: HistoryMode.All, LockedOpacity: 0.3);

        saved.Save(path);

        Assert.Contains("\"All\"", File.ReadAllText(path));
        Assert.Equal(saved, OverlaySettings.Load(path));
    }

    [Theory]
    [InlineData(HistoryMode.BossesAndLong, false, 12_000, false)]
    [InlineData(HistoryMode.BossesAndLong, true, 8_000, true)]
    [InlineData(HistoryMode.BossesAndLong, false, 25_000, true)]
    [InlineData(HistoryMode.BossesOnly, false, 600_000, false)]
    [InlineData(HistoryMode.All, false, 12_000, true)]
    [InlineData(HistoryMode.All, true, 4_000, false)]
    public void History_policy_decides_which_fights_are_saved(HistoryMode mode, bool boss, long combatMs, bool expected)
    {
        var fight = new EncounterSnapshot(Guid.NewGuid(), "Fight", null, DateTimeOffset.UnixEpoch, combatMs, combatMs, false,
            EncounterEndReason.Idle, 1, 1, boss ? new BossSnapshot(Boss, 1, "Berk", 0, 1) : null, []);

        Assert.Equal(expected, new HistoryPolicy(mode, 20).ShouldSave(fight));
    }

    [Fact]
    public void Session_saves_according_to_its_current_policy()
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new NpcSeenEvent(0, Mob, 2000002, 50_000));

        MobFight(tracker, start: 1_000);
        Assert.Empty(session.History.List());

        session.Policy = new HistoryPolicy(HistoryMode.All, 20);
        MobFight(tracker, start: 10_000_000);
        Assert.Single(session.History.List());
    }

    [Fact]
    public void Applying_preferences_sets_target_mode_timeouts_policy_and_names()
    {
        var data = DataWithSpanish();
        var meter = new MeterOptions();
        var tracker = new CombatTracker(GameData.Load(data), meter);
        var gameData = GameData.Load(data);
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        var tuning = new MeterTuning(meter, gameData, data, session);

        tuning.Apply(new OverlaySettings(TargetMode: TargetMode.All, Language: "es", IdleTimeoutSec: 15, BossIdleTimeoutSec: 45,
            HistoryMode: HistoryMode.All, HistoryMinSeconds: 60));

        Assert.Equal(TargetMode.All, meter.TargetMode);
        Assert.Equal(15_000, meter.IdleTimeoutMs);
        Assert.Equal(45_000, meter.BossIdleTimeoutMs);
        Assert.Equal(new HistoryPolicy(HistoryMode.All, 60), session.Policy);
        Assert.Equal("Bola de fuego", gameData.SkillName(16040000));
        Assert.Equal("Only English", gameData.SkillName(16050000));
    }

    [Fact]
    public void An_unknown_language_keeps_english_names()
    {
        var data = DataWithSpanish();
        var gameData = GameData.Load(data);
        var tuning = new MeterTuning(new MeterOptions(), gameData, data, null);

        tuning.Apply(new OverlaySettings(Language: "xx"));

        Assert.Equal("Fireball", gameData.SkillName(16040000));
    }

    private string DataWithSpanish()
    {
        var data = Path.Combine(_dir, "data");
        Directory.CreateDirectory(Path.Combine(data, "skills"));
        File.WriteAllText(Path.Combine(data, "skills", "en.json"), """{"16040000": "Fireball", "16050000": "Only English"}""");
        File.WriteAllText(Path.Combine(data, "skills", "es.json"), """{"16040000": "Bola de fuego"}""");
        return data;
    }

    private static void MobFight(CombatTracker t, long start)
    {
        for (var ms = 0L; ms <= 12_000; ms += 2_000)
            t.Process(new DamageEvent(start + ms, Me, Mob, 16040000, 1_000, HitFlags.None));
        t.Tick(start + 12_000 + 3_600_000);
    }
}
