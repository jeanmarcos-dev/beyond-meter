using AionMeter.Core.Text;
using AionMeter.Overlay;
using AionMeter.Overlay.Platform;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace AionMeter.Tests;

public sealed class NpcapNoticeTests
{
    private readonly FakeDesktop _desktop = new();

    [Fact]
    public void The_notice_shows_only_for_live_capture_without_the_library()
    {
        _desktop.CaptureLibraryMissing = true;
        Assert.True(NpcapNotice.Needed(_desktop, liveCapture: true));
        Assert.False(NpcapNotice.Needed(_desktop, liveCapture: false));

        _desktop.CaptureLibraryMissing = false;
        Assert.False(NpcapNotice.Needed(_desktop, liveCapture: true));
    }

    [Fact]
    public void Windows_asks_npcap_and_linux_never_misses_it()
    {
        using var without = new WindowsDesktop(new NoNative(), _ => new NullBossAlert(), _ => null, npcapInstalled: () => false);
        using var with = new WindowsDesktop(new NoNative(), _ => new NullBossAlert(), _ => null, npcapInstalled: () => true);
        using var linux = new LinuxDesktop(_ => null);

        Assert.True(without.CaptureLibraryMissing);
        Assert.False(with.CaptureLibraryMissing);
        Assert.False(linux.CaptureLibraryMissing);
    }

    [AvaloniaFact]
    public void Yes_opens_the_npcap_download_page_and_no_just_closes()
    {
        var notice = new NpcapNotice(_desktop);
        notice.Show();

        Assert.Contains(notice.GetLogicalDescendants().OfType<TextBlock>(), b => b.Text == UiText.Current.NpcapNeeded);
        notice.NoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Empty(_desktop.Opened);

        var again = new NpcapNotice(_desktop);
        again.Show();
        again.YesButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal([NpcapNotice.DownloadUrl], _desktop.Opened);
    }

    private sealed class NoNative : IWin32
    {
        public long GetExStyle(IntPtr window) => 0;

        public void SetExStyle(IntPtr window, long style)
        {
        }

        public void MakeLayerOpaque(IntPtr window)
        {
        }

        public bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key) => true;

        public void UnregisterHotKey(IntPtr window, int id)
        {
        }

        public (int X, int Y)? WindowOrigin(IntPtr window) => null;

        public void Beep()
        {
        }
    }
}
