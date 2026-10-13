using System.Globalization;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using Avalonia;

namespace AionMeter.Overlay;

public sealed record SkillTableRow(
    string Name,
    string Hits,
    string Ticks,
    string Damage,
    string Dps,
    string Average,
    string Max,
    string Share,
    string Crit,
    string Back,
    string Front,
    string Perfect,
    string Heavy,
    string Multi);

public sealed record TimelineSeries(uint ActorId, string Name, GameClass Class, bool IsSelected, IReadOnlyList<double> Values);

public sealed record TimelineView(int Seconds, double AxisMax, IReadOnlyList<TimelineSeries> Series);

public sealed record RotationItem(int Index, int OffsetMs, int SkillCode, string Name, long Damage, bool IsCrit, double Size, double X)
{
    public string Tooltip
    {
        get
        {
            var crit = IsCrit ? " · crítico" : "";
            return $"{Name} · {MeterView.Compact(Damage)}{crit} · {MeterView.Duration(OffsetMs)}";
        }
    }
}

public sealed record DefenseView(string Taken, string PerSecond, string Hits);

public static class BreakdownView
{
    public const double MinCastSize = 14;
    public const double MaxCastSize = 36;
    public const double CastGap = 4;
    public const int SmoothingSeconds = 3;

    public static readonly IReadOnlyList<string> Tabs = ["Skills", "Línea de tiempo", "Rotación", "Defensa"];

    public static IReadOnlyList<SkillTableRow> Skills(CombatantDetail detail) =>
        detail.Skills
            .Select(s => new SkillTableRow(
                s.Name,
                s.Hits.ToString(CultureInfo.InvariantCulture),
                s.DotTicks.ToString(CultureInfo.InvariantCulture),
                MeterView.Compact(s.Damage),
                MeterView.Compact(s.Dps),
                MeterView.Compact(s.Average),
                MeterView.Compact(s.Max),
                MeterView.Percent(s.Share),
                MeterView.Rate(s.CritRate),
                MeterView.Rate(s.BackRate),
                MeterView.Rate(s.FrontRate),
                MeterView.Rate(s.PerfectRate),
                MeterView.Rate(s.HeavyRate),
                MeterView.Rate(s.MultiRate)))
            .ToList();

    public static TimelineView Timeline(CombatantDetail selected, IReadOnlyList<CombatantDetail> party)
    {
        var members = party.Where(d => d.ActorId != selected.ActorId).Append(selected).ToList();
        var seconds = Math.Max(1, members.Max(d => Math.Max(d.PerSecond.Count, (int)Math.Ceiling(d.CombatMs / 1000.0))));
        var series = members
            .Select(d => new TimelineSeries(d.ActorId, d.Name, d.Class, d.ActorId == selected.ActorId, Smooth(d.PerSecond, seconds)))
            .ToList();
        return new TimelineView(seconds, AxisMax(series.Max(s => s.Values.Max())), series);
    }

    public static double AxisMax(double max)
    {
        if (max <= 0) return 1;
        var unit = Math.Pow(10, Math.Floor(Math.Log10(max)));
        var steps = max / unit;
        var step = steps <= 1 ? 1 : steps <= 2 ? 2 : steps <= 5 ? 5 : 10;
        return step * unit;
    }

    public static IReadOnlyList<RotationItem> Rotation(CombatantDetail detail)
    {
        var names = detail.Skills.ToDictionary(s => s.SkillCode, s => s.Name);
        var casts = detail.Casts.Where(c => (c.Flags & HitFlags.Dot) == 0).OrderBy(c => c.OffsetMs).ToList();
        var top = casts.Count == 0 ? 0 : casts.Max(c => c.Damage);
        var items = new List<RotationItem>(casts.Count);
        var x = 0.0;
        foreach (var cast in casts)
        {
            var size = top <= 0 ? MinCastSize : MinCastSize + (MaxCastSize - MinCastSize) * Math.Sqrt(Math.Max(0, cast.Damage) / (double)top);
            var name = names.TryGetValue(cast.SkillCode, out var known) ? known : "Skill " + cast.SkillCode.ToString(CultureInfo.InvariantCulture);
            items.Add(new RotationItem(items.Count, cast.OffsetMs, cast.SkillCode, name, cast.Damage, (cast.Flags & HitFlags.Critical) != 0, size, x));
            x += size + CastGap;
        }
        return items;
    }

    public static (int Start, int Count) VisibleRotation(IReadOnlyList<RotationItem> items, double fromX, double toX)
    {
        var start = FirstWhere(items, i => i.X >= fromX - MaxCastSize);
        var end = FirstWhere(items, i => i.X > toX);
        return (start, Math.Max(0, end - start));
    }

    public static RotationItem? RotationAt(IReadOnlyList<RotationItem> items, double x)
    {
        var index = FirstWhere(items, i => i.X > x) - 1;
        return index >= 0 && items[index].X + items[index].Size >= x ? items[index] : null;
    }

    public static DefenseView Defense(CombatantDetail detail)
    {
        var seconds = detail.CombatMs / 1000.0;
        return new DefenseView(
            MeterView.Compact(detail.DamageTaken),
            seconds > 0 ? MeterView.Compact(detail.DamageTaken / seconds) : "-",
            detail.HitsTaken.ToString(CultureInfo.InvariantCulture));
    }

    public static PixelPoint Placement(PixelRect workArea, double width, double height, double scaling) =>
        new(
            workArea.X + (int)Math.Max(0, (workArea.Width - width * scaling) / 2),
            workArea.Y + (int)Math.Max(0, (workArea.Height - height * scaling) / 2));

    private static double[] Smooth(IReadOnlyList<long> perSecond, int seconds)
    {
        var values = new double[seconds];
        var half = SmoothingSeconds / 2;
        for (var i = 0; i < seconds; i++)
        {
            var from = Math.Max(0, i - half);
            var to = Math.Min(seconds - 1, i + half);
            var sum = 0.0;
            for (var j = from; j <= to; j++) sum += j < perSecond.Count ? perSecond[j] : 0;
            values[i] = sum / (to - from + 1);
        }
        return values;
    }

    private static int FirstWhere(IReadOnlyList<RotationItem> items, Func<RotationItem, bool> predicate)
    {
        var low = 0;
        var high = items.Count;
        while (low < high)
        {
            var mid = (low + high) / 2;
            if (predicate(items[mid])) high = mid;
            else low = mid + 1;
        }
        return low;
    }
}
