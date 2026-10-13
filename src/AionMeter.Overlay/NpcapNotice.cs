using AionMeter.Core.Text;
using AionMeter.Overlay.Platform;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class NpcapNotice : Window
{
    public const string DownloadUrl = "https://npcap.com/#download";

    public NpcapNotice(IDesktopPlatform desktop)
    {
        var t = UiText.Current;
        Title = AppInfo.Name;
        Width = 460;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1C));
        YesButton.Content = t.OvYes;
        NoButton.Content = t.OvNo;
        YesButton.Click += (_, _) =>
        {
            desktop.Open(DownloadUrl);
            Close();
        };
        NoButton.Click += (_, _) => Close();
        Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 14,
            Children =
            {
                new TextBlock { Text = t.NpcapNeeded, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { NoButton, YesButton } },
            },
        };
    }

    public Button YesButton { get; } = new() { Name = nameof(YesButton), IsDefault = true, Padding = new Thickness(16, 4) };

    public Button NoButton { get; } = new() { Name = nameof(NoButton), IsCancel = true, Padding = new Thickness(16, 4) };

    public static bool Needed(IDesktopPlatform desktop, bool liveCapture) => liveCapture && desktop.CaptureLibraryMissing;
}
