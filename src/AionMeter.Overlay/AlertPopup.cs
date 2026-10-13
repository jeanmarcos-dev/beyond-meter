using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace AionMeter.Overlay;

public sealed class AlertPopup : Window
{
    public const double PopupWidth = 320;
    public const double PopupHeight = 84;
    public static readonly TimeSpan ShownFor = TimeSpan.FromSeconds(8);

    private readonly DispatcherTimer _timer = new() { Interval = ShownFor };

    public AlertPopup(string title, string body)
    {
        Width = PopupWidth;
        Height = PopupHeight;
        CanResize = false;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        SystemDecorations = SystemDecorations.None;
        Background = new SolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1C));
        Heading.Text = title;
        Body.Text = body;
        Content = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 8),
            Child = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center, Children = { Heading, Body } },
        };
        PointerPressed += (_, _) => Close();
        _timer.Tick += (_, _) => Close();
        Opened += (_, _) => _timer.Start();
    }

    public TextBlock Heading { get; } = new() { Foreground = Brushes.White, FontWeight = FontWeight.Bold, TextTrimming = TextTrimming.CharacterEllipsis };

    public TextBlock Body { get; } = new() { Foreground = Brushes.White, FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxLines = 2 };

    public int Slot { get; private set; }

    public void PlaceAt(int slot)
    {
        Slot = slot;
        if (Screens.Primary is not { } screen) return;
        var area = screen.WorkingArea;
        var width = (int)Math.Ceiling(PopupWidth * screen.Scaling);
        var height = (int)Math.Ceiling(PopupHeight * screen.Scaling);
        var gap = (int)Math.Ceiling(8 * screen.Scaling);
        Position = new PixelPoint(area.Right - width - gap, area.Bottom - (slot + 1) * (height + gap));
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        base.OnClosed(e);
    }
}

public sealed class PopupAlert(Action? sound = null) : IBossAlert
{
    private readonly List<AlertPopup> _open = [];

    public IReadOnlyList<AlertPopup> Open => _open;

    public void Notify(string title, string body) => Dispatcher.UIThread.Post(() => Show(title, body));

    private void Show(string title, string body)
    {
        var popup = new AlertPopup(title, body);
        popup.PlaceAt(Enumerable.Range(0, _open.Count + 1).First(slot => _open.All(open => open.Slot != slot)));
        popup.Closed += (_, _) => _open.Remove(popup);
        _open.Add(popup);
        popup.Show();
        sound?.Invoke();
    }
}
