using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Overlay;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class PingOverlayTests : IDisposable
{
    private const long DotnetEpoch = 62_135_596_800_000;

    private readonly string _dir = Directory.CreateTempSubdirectory("ping-overlay").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private sealed class FakeSource : IEventSource
    {
        public event Action<GameEvent>? EventDecoded;
        public event Action<CaptureStatus>? StatusChanged;
        public CaptureStatus Status => new(CaptureState.Capturing, "Capturing");

        public void Emit(GameEvent e) => EventDecoded?.Invoke(e);

        public void Start() => StatusChanged?.Invoke(Status);

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }

    private (OverlayWindow Window, FakeSource Source) Open()
    {
        var source = new FakeSource();
        var session = new MeterSession(new CombatTracker(GameData.Empty, new MeterOptions()), new HistoryStore(Path.Combine(_dir, "fights")), source, null, new HistoryPolicy());
        session.Start();
        var window = new OverlayWindow(session, new UserSettings(), Path.Combine(_dir, "settings.json"), null);
        window.Show();
        return (window, source);
    }

    private static void Ping(OverlayWindow window, FakeSource source, int ms)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        source.Emit(new PingReplyEvent(now, now - ms + DotnetEpoch));
        window.Apply(OverlayCommand.Live);
        Dispatcher.UIThread.RunJobs();
    }

    private static TextBlock Indicator(OverlayWindow window) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Single(c => c.Name == "Ping");

    private static Color Colour(TextBlock block) => ((ISolidColorBrush)block.Foreground!).Color;

    private static readonly Color Gold = Color.FromRgb(0xF5, 0xC5, 0x42);

    private static List<Avalonia.Controls.Shapes.Rectangle> Bars(OverlayWindow window) =>
        window.GetLogicalDescendants().OfType<Panel>().Single(c => c.Name == "PingBars").Children.Cast<Avalonia.Controls.Shapes.Rectangle>().ToList();

    private static void AssertBarsIn(OverlayWindow window, Color colour)
    {
        var bars = Bars(window);
        Assert.Equal(4, bars.Count);
        Assert.True(bars.Zip(bars.Skip(1)).All(p => p.First.Height < p.Second.Height));
        Assert.All(bars, b => Assert.Equal(colour, ((ISolidColorBrush)b.Fill!).Color));
    }

    [AvaloniaFact]
    public void Without_a_reply_the_indicator_waits_in_grey_outside_a_fight()
    {
        var (window, _) = Open();
        window.Apply(OverlayCommand.Live);
        Dispatcher.UIThread.RunJobs();

        var ping = Indicator(window);
        Assert.True(ping.IsVisible);
        Assert.Equal("— ms", ping.Text);
        Assert.Equal(Color.FromRgb(0xA8, 0xB3, 0xC7), Colour(ping));
        AssertBarsIn(window, Colour(ping));
    }

    [AvaloniaFact]
    public void Each_level_has_its_colour()
    {
        var (window, source) = Open();

        Ping(window, source, 66);
        Assert.Equal("66 ms", Indicator(window).Text);
        var good = Colour(Indicator(window));
        AssertBarsIn(window, good);

        Ping(window, source, 140);
        Assert.Equal("140 ms", Indicator(window).Text);
        var fair = Colour(Indicator(window));
        AssertBarsIn(window, fair);

        Ping(window, source, 250);
        Assert.Equal("250 ms", Indicator(window).Text);
        var poor = Colour(Indicator(window));
        AssertBarsIn(window, poor);

        Assert.Equal(Color.FromRgb(0x6B, 0xD1, 0x7F), good);
        Assert.Equal(Color.FromRgb(0xE6, 0xE0, 0x3A), fair);
        Assert.NotEqual(Gold, fair);
        Assert.Equal(Color.FromRgb(0xFF, 0x55, 0x55), poor);
    }

    [AvaloniaFact]
    public void Turning_the_ping_off_hides_the_indicator()
    {
        var (window, source) = Open();
        Ping(window, source, 42);

        var off = UserSettings.Parse(window.Settings.ToJson());
        off.ShowPing = false;
        window.UseSettings(off);
        Dispatcher.UIThread.RunJobs();

        Assert.False(Indicator(window).IsVisible);
        Assert.False(window.GetLogicalDescendants().OfType<Panel>().Single(c => c.Name == "PingBars").IsVisible);
    }
}
