using System.Runtime.InteropServices;

namespace AionMeter.Overlay;

public sealed class X11Window : IDisposable
{
    private const int ShapeInput = 2;
    private const int ShapeSet = 0;
    private const int Unsorted = 0;

    private IntPtr _display;

    private X11Window(IntPtr display) => _display = display;

    public static X11Window? Open()
    {
        if (!OperatingSystem.IsLinux()) return null;
        try
        {
            var display = XOpenDisplay(IntPtr.Zero);
            return display == IntPtr.Zero ? null : new X11Window(display);
        }
        catch (DllNotFoundException)
        {
            return null;
        }
    }

    public void SetClickThrough(IntPtr window, bool enabled)
    {
        if (_display == IntPtr.Zero) return;
        if (enabled)
            XShapeCombineRectangles(_display, window, ShapeInput, 0, 0, IntPtr.Zero, 0, ShapeSet, Unsorted);
        else
            XShapeCombineMask(_display, window, ShapeInput, 0, 0, IntPtr.Zero, ShapeSet);
        XFlush(_display);
    }

    public (int X, int Y)? RootPosition(IntPtr window)
    {
        if (_display == IntPtr.Zero) return null;
        var root = XDefaultRootWindow(_display);
        return XTranslateCoordinates(_display, window, root, 0, 0, out var x, out var y, out _) != 0 ? (x, y) : null;
    }

    public void Dispose()
    {
        if (_display == IntPtr.Zero) return;
        XCloseDisplay(_display);
        _display = IntPtr.Zero;
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr name);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XTranslateCoordinates(IntPtr display, IntPtr source, IntPtr destination, int x, int y, out int destX, out int destY, out IntPtr child);

    [DllImport("libXext.so.6")]
    private static extern void XShapeCombineRectangles(IntPtr display, IntPtr window, int kind, int x, int y, IntPtr rectangles, int count, int op, int ordering);

    [DllImport("libXext.so.6")]
    private static extern void XShapeCombineMask(IntPtr display, IntPtr window, int kind, int x, int y, IntPtr mask, int op);
}
