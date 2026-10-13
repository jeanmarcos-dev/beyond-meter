using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Overlay;
using Avalonia;

namespace AionMeter.Tests;

public sealed class BreakdownViewTests
{
    private static readonly HitQuality NoQuality = new(0, 0, null, null, null, null, null, null, 0, null);

    private static SkillRow Skill(int code, string name, int hits = 10, int ticks = 0, double? crit = 0.5, double? back = null) =>
        new(code, name, hits, ticks, 1_000, 100, 0.1, 100, 50, 200, crit, back, null, null, null, null);

    private static CombatantDetail Detail(
        uint id = 100,
        string name = "Ilvane",
        GameClass cls = GameClass.Elementalist,
        bool self = true,
        long combatMs = 60_000,
        long taken = 0,
        int hitsTaken = 0,
        IReadOnlyList<SkillRow>? skills = null,
        IReadOnlyList<long>? perSecond = null,
        IReadOnlyList<CastRecord>? casts = null) =>
        new(id, name, cls, self, 0, 0, 0, combatMs, taken, hitsTaken, NoQuality, skills ?? [], perSecond ?? [], casts ?? []);

    [Fact]
    public void Skills_table_lists_every_skill_without_a_row_limit_and_dashes_unmeasurable_rates()
    {
        var skills = Enumerable.Range(1, 40).Select(i => Skill(i, "S" + i)).ToList();
        skills[3] = Skill(4, "Quemadura", hits: 0, ticks: 12, crit: null);
        skills[4] = Skill(5, "Espadazo", hits: 20, crit: 0.25, back: 0.5);

        var rows = BreakdownView.Skills(Detail(skills: skills));

        Assert.Equal(40, rows.Count);
        Assert.Equal(["Quemadura", "0", "12", "-", "-", "-", "-", "-", "-"], [rows[3].Name, rows[3].Hits, rows[3].Ticks, rows[3].Crit, rows[3].Back, rows[3].Front, rows[3].Perfect, rows[3].Heavy, rows[3].Multi]);
        Assert.Equal(["20", "25%", "50%"], [rows[4].Hits, rows[4].Crit, rows[4].Back]);
        Assert.Equal(["1000", "100", "100", "200", "10%"], [rows[4].Damage, rows[4].Dps, rows[4].Average, rows[4].Max, rows[4].Share]);
    }

    [Fact]
    public void Timeline_smooths_each_second_over_three_seconds()
    {
        var spike = Detail(perSecond: [0, 0, 900, 0, 0], combatMs: 5_000);

        var view = BreakdownView.Timeline(spike, [spike]);

        Assert.Equal([0, 300, 300, 300, 0], view.Series.Single().Values);
    }

    [Fact]
    public void Timeline_drops_during_a_pause_in_the_middle_of_the_fight()
    {
        var damage = Enumerable.Range(0, 40).Select(s => s is >= 20 and <= 29 ? 0L : 900L).ToList();
        var self = Detail(perSecond: damage, combatMs: 40_000);

        var values = BreakdownView.Timeline(self, [self]).Series.Single().Values;

        Assert.Equal(900, values[10]);
        Assert.Equal(0, values[25]);
        Assert.Equal(300, values[20]);
    }

    [Fact]
    public void Timeline_axis_covers_the_fight_duration_even_after_the_last_hit()
    {
        var self = Detail(perSecond: Enumerable.Repeat(1_000L, 90).ToList(), combatMs: 125_000);

        var view = BreakdownView.Timeline(self, [self]);

        Assert.Equal(125, view.Seconds);
        Assert.Equal(125, view.Series.Single().Values.Count);
        Assert.Equal(0, view.Series.Single().Values[124]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(740, 1_000)]
    [InlineData(1_900, 2_000)]
    [InlineData(2_100, 5_000)]
    [InlineData(5_001, 10_000)]
    public void Axis_maximum_is_the_next_round_step(double max, double expected)
    {
        Assert.Equal(expected, BreakdownView.AxisMax(max));
    }

    [Fact]
    public void Timeline_draws_the_selected_player_last_and_the_party_in_class_colour()
    {
        var self = Detail(100, "Ilvane", GameClass.Elementalist, perSecond: [500, 500]);
        var mate = Detail(101, "Brea", GameClass.Cleric, self: false, perSecond: [100, 100]);

        var view = BreakdownView.Timeline(self, [self, mate]);

        Assert.Equal([101u, 100u], view.Series.Select(s => s.ActorId));
        Assert.Equal([false, true], view.Series.Select(s => s.IsSelected));
        Assert.Equal([GameClass.Cleric, GameClass.Elementalist], view.Series.Select(s => s.Class));
        Assert.Equal(500, view.AxisMax);
    }

    [Fact]
    public void Rotation_leaves_out_damage_over_time_ticks_and_keeps_chronological_order()
    {
        var casts = new CastRecord[]
        {
            new(3_000, 2, 500, HitFlags.None),
            new(1_000, 1, 300, HitFlags.None),
            new(2_000, 9, 80, HitFlags.Dot),
        };

        var rotation = BreakdownView.Rotation(Detail(casts: casts));

        Assert.Equal([1, 2], rotation.Select(r => r.SkillCode));
        Assert.Equal([1_000, 3_000], rotation.Select(r => r.OffsetMs));
    }

    [Fact]
    public void Rotation_sizes_by_damage_and_marks_critical_hits()
    {
        var casts = new CastRecord[]
        {
            new(1_000, 1, 100, HitFlags.None),
            new(2_000, 1, 10_000, HitFlags.Critical),
            new(3_000, 1, 2_500, HitFlags.None),
        };

        var rotation = BreakdownView.Rotation(Detail(casts: casts));

        Assert.Equal([false, true, false], rotation.Select(r => r.IsCrit));
        Assert.Equal(BreakdownView.MaxCastSize, rotation[1].Size);
        Assert.True(rotation[0].Size < rotation[2].Size && rotation[2].Size < rotation[1].Size);
        Assert.True(rotation[0].Size >= BreakdownView.MinCastSize);
        Assert.True(rotation[1].X > rotation[0].X && rotation[2].X > rotation[1].X);
    }

    [Fact]
    public void Rotation_tooltip_names_the_skill_and_its_damage_with_a_fallback_for_unknown_codes()
    {
        var casts = new CastRecord[]
        {
            new(1_000, 7, 12_345, HitFlags.Critical),
            new(2_000, 8, 900, HitFlags.None),
        };

        var rotation = BreakdownView.Rotation(Detail(skills: [Skill(7, "Bola de fuego")], casts: casts));

        Assert.Equal("Bola de fuego · 12.3K · crítico · 0:01", rotation[0].Tooltip);
        Assert.Equal("Skill 8 · 900 · 0:02", rotation[1].Tooltip);
    }

    [Fact]
    public void Rotation_visible_range_covers_only_what_the_viewport_shows()
    {
        var casts = Enumerable.Range(0, 20_000).Select(i => new CastRecord(i * 100, 1, 1_000, HitFlags.None)).ToArray();
        var rotation = BreakdownView.Rotation(Detail(casts: casts));
        var middle = rotation[10_000].X;

        var (start, count) = BreakdownView.VisibleRotation(rotation, middle, middle + 800);

        Assert.InRange(count, 1, 80);
        Assert.InRange(start, 9_900, 10_000);
        Assert.Equal(0, BreakdownView.VisibleRotation([], 0, 800).Count);
    }

    [Fact]
    public void Rotation_item_lookup_finds_the_cast_under_a_position()
    {
        var casts = Enumerable.Range(0, 100).Select(i => new CastRecord(i * 100, i + 1, 1_000, HitFlags.None)).ToArray();
        var rotation = BreakdownView.Rotation(Detail(casts: casts));

        Assert.Equal(51, BreakdownView.RotationAt(rotation, rotation[50].X + 2)!.SkillCode);
        Assert.Null(BreakdownView.RotationAt(rotation, -5));
        Assert.Null(BreakdownView.RotationAt(rotation, rotation[^1].X + 1_000));
    }

    [Fact]
    public void Defense_shows_damage_taken_per_second_of_fight_and_hits()
    {
        var tank = Detail(taken: 600_000, hitsTaken: 30, combatMs: 60_000);

        var defense = BreakdownView.Defense(tank);

        Assert.Equal(("600.0K", "10.0K", "30"), (defense.Taken, defense.PerSecond, defense.Hits));
    }

    [Fact]
    public void Defense_per_second_is_a_dash_without_fight_time()
    {
        Assert.Equal("-", BreakdownView.Defense(Detail(taken: 1_000, hitsTaken: 2, combatMs: 0)).PerSecond);
    }

    [Theory]
    [InlineData(1.0, 2720, 400)]
    [InlineData(2.0, 2240, 80)]
    [InlineData(4.0, 1920, 0)]
    public void Placement_centres_the_window_on_the_monitor_work_area_and_never_leaves_it(double scaling, int x, int y)
    {
        var area = new PixelRect(1920, 0, 2560, 1440);

        Assert.Equal(new PixelPoint(x, y), BreakdownView.Placement(area, 960, 640, scaling));
    }
}
