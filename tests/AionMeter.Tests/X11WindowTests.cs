using AionMeter.Overlay;

namespace AionMeter.Tests;

public class X11WindowTests
{
    [DisplayFact]
    public void A_closed_display_ignores_late_calls_instead_of_crashing()
    {
        var x11 = X11Window.Open();
        Assert.NotNull(x11);

        x11.Dispose();
        x11.SetClickThrough(IntPtr.Zero, true);

        Assert.Null(x11.RootPosition(IntPtr.Zero));
        x11.Dispose();
    }
}
