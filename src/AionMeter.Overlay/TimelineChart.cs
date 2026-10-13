using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class TimelineChart : Control
{
    private const double Left = 58;
    private const double Right = 14;
    private const double Top = 10;
    private const double Bottom = 24;
    private const int GridLines = 4;

    private static readonly int[] TimeSteps = [5, 10, 15, 30, 60, 120, 300, 600, 1_200, 1_800, 3_600];
    private static readonly IBrush Muted = new SolidColorBrush(BreakdownPalette.Muted);
    private static readonly IPen GridPen = new Pen(new SolidColorBrush(BreakdownPalette.Muted, 0.2), 1);

    private TimelineView? _view;

    public int DrawnSeries { get; private set; }

    public int DrawnPoints { get; private set; }

    public void Show(TimelineView view)
    {
        _view = view;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        DrawnSeries = 0;
        DrawnPoints = 0;
        var plot = new Rect(Left, Top, Math.Max(1, Bounds.Width - Left - Right), Math.Max(1, Bounds.Height - Top - Bottom));
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        if (_view is not { } view) return;

        DrawGrid(context, plot, view);
        foreach (var series in view.Series) DrawSeries(context, plot, view, series);
    }

    private static void DrawGrid(DrawingContext context, Rect plot, TimelineView view)
    {
        for (var i = 0; i <= GridLines; i++)
        {
            var y = plot.Bottom - plot.Height * i / GridLines;
            context.DrawLine(GridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            Label(context, MeterView.Compact(view.AxisMax * i / GridLines), new Point(4, y - 7));
        }
        var step = TimeSteps.FirstOrDefault(s => view.Seconds / s <= 10, TimeSteps[^1]);
        for (var second = 0; second < view.Seconds; second += step)
        {
            var x = XOf(plot, view, second);
            context.DrawLine(GridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
            Label(context, MeterView.Duration(second * 1_000L), new Point(x - 12, plot.Bottom + 5));
        }
    }

    private void DrawSeries(DrawingContext context, Rect plot, TimelineView view, TimelineSeries series)
    {
        var color = BreakdownPalette.Class(series.Class);
        var pen = new Pen(new SolidColorBrush(color, series.IsSelected ? 1 : 0.6), series.IsSelected ? 2.5 : 1, lineJoin: PenLineJoin.Round);
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            for (var i = 0; i < series.Values.Count; i++)
            {
                var point = new Point(XOf(plot, view, i), plot.Bottom - plot.Height * Math.Min(1, series.Values[i] / view.AxisMax));
                if (i == 0) path.BeginFigure(point, false);
                else path.LineTo(point);
            }
            path.EndFigure(false);
        }
        context.DrawGeometry(null, pen, geometry);
        DrawnSeries++;
        DrawnPoints += series.Values.Count;
    }

    private static double XOf(Rect plot, TimelineView view, int second) =>
        view.Seconds <= 1 ? plot.Left : plot.Left + plot.Width * second / (view.Seconds - 1);

    private static void Label(DrawingContext context, string text, Point at) =>
        context.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 11, Muted), at);
}
