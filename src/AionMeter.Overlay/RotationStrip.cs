using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class RotationStrip : Control
{
    public const double StripHeight = 72;
    private const double Middle = 26;
    private const double LabelSize = 9.5;
    private const double CastGap = BreakdownView.CastGap;

    private const double DefaultViewport = 1_200;

    private static readonly IBrush Crit = new SolidColorBrush(BreakdownPalette.Gold);
    private static readonly IPen CritPen = new Pen(Crit, 2);
    private static readonly IBrush Label = new SolidColorBrush(BreakdownPalette.Muted);
    private static readonly Typeface Face = new(FontFamily.Default);
    private readonly Dictionary<int, IBrush> _brushes = [];

    private IReadOnlyList<RotationItem> _items = [];
    private double _offset;
    private double _viewport = DefaultViewport;

    public RotationStrip()
    {
        Height = StripHeight;
    }

    public IReadOnlyList<RotationItem> Items => _items;

    public Func<int, IImage?>? Icon { get; set; }

    public int DrawnCount { get; private set; }

    public void Show(IReadOnlyList<RotationItem> items)
    {
        _items = items;
        Width = items.Count == 0 ? 0 : items[^1].X + items[^1].Size;
        InvalidateVisual();
    }

    public void SetViewport(double offset, double width)
    {
        _offset = offset;
        _viewport = width > 0 ? width : DefaultViewport;
        InvalidateVisual();
    }

    public string? TooltipAt(double x) => BreakdownView.RotationAt(_items, x)?.Tooltip;

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        var (start, count) = BreakdownView.VisibleRotation(_items, _offset, _offset + _viewport);
        DrawnCount = count;
        for (var i = start; i < start + count; i++)
        {
            var item = _items[i];
            var center = new Point(item.X + item.Size / 2, Middle);
            var radius = item.Size / 2;
            if (Icon?.Invoke(item.SkillCode) is { } icon)
            {
                using (context.PushGeometryClip(new EllipseGeometry(new Rect(item.X, Middle - radius, item.Size, item.Size))))
                    context.DrawImage(icon, new Rect(item.X, Middle - radius, item.Size, item.Size));
                if (item.IsCrit) context.DrawEllipse(null, CritPen, center, radius, radius);
            }
            else
            {
                context.DrawEllipse(SkillBrush(item.SkillCode), item.IsCrit ? CritPen : null, center, radius, radius);
            }
            if (item.IsCrit) context.DrawEllipse(Crit, null, new Point(center.X, Middle - radius - 5), 3, 3);
            var damage = new FormattedText(MeterView.Compact(item.Damage), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, LabelSize, Label);
            if (damage.Width <= item.Size + CastGap) context.DrawText(damage, new Point(center.X - damage.Width / 2, Middle + radius + 2));
        }
    }

    private IBrush SkillBrush(int code)
    {
        if (!_brushes.TryGetValue(code, out var brush)) _brushes[code] = brush = new SolidColorBrush(BreakdownPalette.Skill(code));
        return brush;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        ToolTip.SetTip(this, TooltipAt(e.GetPosition(this).X));
    }
}
