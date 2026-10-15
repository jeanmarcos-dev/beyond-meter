using Avalonia;
using Avalonia.Input;

namespace AionMeter.Overlay;

[Flags]
public enum ResizeEdge
{
    None = 0,
    Left = 1,
    Right = 2,
    Bottom = 4,
}

public readonly record struct ResizeResult(double Width, double Shift, int Rows);

public static class EdgeResize
{
    public const double Band = 6;
    public const double MinWidth = 280, MaxWidth = 2000;
    public const int MinRows = 1, MaxRows = 20;

    public static ResizeEdge At(Point point, Size size)
    {
        var edge = ResizeEdge.None;
        if (point.X < Band) edge |= ResizeEdge.Left;
        else if (point.X >= size.Width - Band) edge |= ResizeEdge.Right;
        if (point.Y >= size.Height - Band) edge |= ResizeEdge.Bottom;
        return edge;
    }

    public static StandardCursorType CursorFor(ResizeEdge edge) => edge switch
    {
        ResizeEdge.Left or ResizeEdge.Right => StandardCursorType.SizeWestEast,
        ResizeEdge.Bottom => StandardCursorType.SizeNorthSouth,
        ResizeEdge.Left | ResizeEdge.Bottom => StandardCursorType.BottomLeftCorner,
        ResizeEdge.Right | ResizeEdge.Bottom => StandardCursorType.BottomRightCorner,
        _ => StandardCursorType.Arrow,
    };

    public static ResizeResult Resize(ResizeEdge edge, double startWidth, int startRows, double rowPitch, Vector delta)
    {
        var width = startWidth;
        if (edge.HasFlag(ResizeEdge.Right)) width = Math.Clamp(startWidth + delta.X, MinWidth, MaxWidth);
        if (edge.HasFlag(ResizeEdge.Left)) width = Math.Clamp(startWidth - delta.X, MinWidth, MaxWidth);
        var shift = edge.HasFlag(ResizeEdge.Left) ? startWidth - width : 0;
        var rows = edge.HasFlag(ResizeEdge.Bottom) ? Math.Clamp(startRows + (int)Math.Round(delta.Y / rowPitch), MinRows, MaxRows) : startRows;
        return new ResizeResult(width, shift, rows);
    }
}
