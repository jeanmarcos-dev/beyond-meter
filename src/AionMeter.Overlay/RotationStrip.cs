using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class RotationStrip : Control
{
    public const double StripHeight = 56;

    private const double DefaultViewport = 1_200;

    private static readonly IBrush Cast = new SolidColorBrush(Color.FromRgb(0x3B, 0x7C, 0xE6), 0.85);
    private static readonly IBrush Crit = new SolidColorBrush(BreakdownPalette.Gold);
    private static readonly IPen CritPen = new Pen(Crit, 2);

    private IReadOnlyList<RotationItem> _items = [];
    private double _offset;
    private double _viewport = DefaultViewport;

    public RotationStrip()
    {
        Height = StripHeight;
    }

    public IReadOnlyList<RotationItem> Items => _items;

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
        var middle = Bounds.Height / 2;
        for (var i = start; i < start + count; i++)
        {
            var item = _items[i];
            var center = new Point(item.X + item.Size / 2, middle);
            var radius = item.Size / 2;
            context.DrawEllipse(Cast, item.IsCrit ? CritPen : null, center, radius, radius);
            if (item.IsCrit) context.DrawEllipse(Crit, null, new Point(center.X, middle - radius - 5), 3, 3);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        ToolTip.SetTip(this, TooltipAt(e.GetPosition(this).X));
    }
}
