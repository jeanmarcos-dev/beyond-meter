using AionMeter.Overlay;
using Avalonia;
using Avalonia.Input;

namespace AionMeter.Tests;

public sealed class EdgeResizeTests
{
    private static readonly Size Window = new(460, 300);

    [Theory]
    [InlineData(2, 150, ResizeEdge.Left)]
    [InlineData(457, 150, ResizeEdge.Right)]
    [InlineData(200, 297, ResizeEdge.Bottom)]
    [InlineData(1, 299, ResizeEdge.Left | ResizeEdge.Bottom)]
    [InlineData(459, 299, ResizeEdge.Right | ResizeEdge.Bottom)]
    [InlineData(200, 150, ResizeEdge.None)]
    [InlineData(200, 1, ResizeEdge.None)]
    [InlineData(2, 1, ResizeEdge.Left)]
    [InlineData(10, 150, ResizeEdge.None)]
    public void The_edge_under_the_pointer_is_found_in_a_thin_band(double x, double y, ResizeEdge edge)
    {
        Assert.Equal(edge, EdgeResize.At(new Point(x, y), Window));
    }

    [Theory]
    [InlineData(ResizeEdge.Left, StandardCursorType.SizeWestEast)]
    [InlineData(ResizeEdge.Right, StandardCursorType.SizeWestEast)]
    [InlineData(ResizeEdge.Bottom, StandardCursorType.SizeNorthSouth)]
    [InlineData(ResizeEdge.Left | ResizeEdge.Bottom, StandardCursorType.BottomLeftCorner)]
    [InlineData(ResizeEdge.Right | ResizeEdge.Bottom, StandardCursorType.BottomRightCorner)]
    [InlineData(ResizeEdge.None, StandardCursorType.Arrow)]
    public void Each_edge_has_its_cursor(ResizeEdge edge, StandardCursorType cursor)
    {
        Assert.Equal(cursor, EdgeResize.CursorFor(edge));
    }

    [Fact]
    public void The_right_edge_changes_the_width_in_place()
    {
        Assert.Equal(new ResizeResult(560, 0, 10), EdgeResize.Resize(ResizeEdge.Right, 460, 10, 26, new Vector(100, 40)));
    }

    [Fact]
    public void The_left_edge_moves_the_window_so_the_right_edge_stays()
    {
        Assert.Equal(new ResizeResult(560, -100, 10), EdgeResize.Resize(ResizeEdge.Left, 460, 10, 26, new Vector(-100, 0)));
        Assert.Equal(new ResizeResult(400, 60, 10), EdgeResize.Resize(ResizeEdge.Left, 460, 10, 26, new Vector(60, 0)));
    }

    [Fact]
    public void The_width_stays_within_its_limits()
    {
        Assert.Equal(280, EdgeResize.Resize(ResizeEdge.Right, 460, 10, 26, new Vector(-400, 0)).Width);
        Assert.Equal(2000, EdgeResize.Resize(ResizeEdge.Right, 460, 10, 26, new Vector(3000, 0)).Width);
        Assert.Equal(new ResizeResult(280, 180, 10), EdgeResize.Resize(ResizeEdge.Left, 460, 10, 26, new Vector(400, 0)));
    }

    [Theory]
    [InlineData(52, 12)]
    [InlineData(38, 11)]
    [InlineData(12, 10)]
    [InlineData(-26, 9)]
    [InlineData(-1000, 1)]
    [InlineData(1000, 20)]
    public void The_bottom_edge_changes_the_rows_one_row_height_at_a_time(double dy, int rows)
    {
        var result = EdgeResize.Resize(ResizeEdge.Bottom, 460, 10, 26, new Vector(80, dy));

        Assert.Equal(new ResizeResult(460, 0, rows), result);
    }

    [Fact]
    public void A_bottom_corner_changes_width_and_rows()
    {
        Assert.Equal(new ResizeResult(500, -40, 12), EdgeResize.Resize(ResizeEdge.Left | ResizeEdge.Bottom, 460, 10, 26, new Vector(-40, 52)));
        Assert.Equal(new ResizeResult(500, 0, 8), EdgeResize.Resize(ResizeEdge.Right | ResizeEdge.Bottom, 460, 10, 26, new Vector(40, -52)));
    }
}
