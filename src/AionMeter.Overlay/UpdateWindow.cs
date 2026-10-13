using System.Globalization;
using AionMeter.Core.Text;
using AionMeter.Core.Updates;
using AionMeter.Overlay.Platform;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class UpdateWindow : Window
{
    private static readonly IBrush Gold = new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42));
    private static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(0xA8, 0xB3, 0xC7));

    private readonly UpdateCoordinator _updates;
    private readonly IDesktopPlatform _desktop;
    private bool _canInstall;
    private CancellationTokenSource? _download;

    public UpdateWindow(UpdateCoordinator updates, ReleaseInfo release, IDesktopPlatform desktop)
    {
        _updates = updates;
        _desktop = desktop;
        Release = release;
        var t = UiText.Current;
        Title = AppInfo.Name;
        Width = 520;
        Height = 560;
        MinWidth = 380;
        MinHeight = 300;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1C));

        _canInstall = updates.CanInstall && release.Installer is not null;
        var current = updates.Service.Current.ToString(3);
        Headline.Text = string.Format(CultureInfo.InvariantCulture, t.UpdateHeadline, release.Version.ToString(3));
        SubLine.Text = release.PublishedAt is { } at
            ? string.Format(CultureInfo.InvariantCulture, t.UpdateReleased, current, at.LocalDateTime.ToString(t.DateFormat, t.Culture))
            : string.Format(CultureInfo.InvariantCulture, t.UpdateYouHave, current);
        foreach (var (text, heading) in ReleaseNotes.Lines(release.Notes, t.Code))
        {
            if (Notes.Inlines!.Count > 0) Notes.Inlines.Add(new LineBreak());
            Notes.Inlines.Add(heading ? new Run(text) { FontWeight = FontWeight.SemiBold, Foreground = Gold } : new Run(text));
        }
        Hint.Text = _canInstall ? t.UpdateInstalledHint : OperatingSystem.IsWindows() ? t.UpdatePortableHint : t.OvLinuxUpdateHint;
        UpdateButton.Content = _canInstall ? t.UpdateNow : t.UpdateOpenPage;
        LaterButton.Content = t.UpdateLater;
        SkipButton.Content = t.UpdateSkip;
        UpdateButton.Click += async (_, _) => await Update();
        LaterButton.Click += (_, _) => Close();
        SkipButton.Click += (_, _) =>
        {
            _updates.Service.Skip(Release);
            Close();
        };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { SkipButton, LaterButton, UpdateButton } };
        var top = new StackPanel
        {
            Spacing = 6,
            Children = { Headline, SubLine, new TextBlock { Text = t.UpdateWhatsNew, Foreground = Gold, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 8, 0, 0) } },
        };
        var bottom = new StackPanel { Spacing = 8, Children = { Hint, Progress, State, buttons } };
        var layout = new DockPanel { Margin = new Thickness(18) };
        DockPanel.SetDock(top, Dock.Top);
        DockPanel.SetDock(bottom, Dock.Bottom);
        layout.Children.Add(top);
        layout.Children.Add(bottom);
        layout.Children.Add(new ScrollViewer { Content = Notes, Margin = new Thickness(0, 6) });
        Content = layout;
    }

    public ReleaseInfo Release { get; }

    public TextBlock Headline { get; } = new() { FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };

    public TextBlock SubLine { get; } = new() { Foreground = Muted, TextWrapping = TextWrapping.Wrap };

    public TextBlock Notes { get; } = new() { Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, Inlines = [] };

    public TextBlock Hint { get; } = new() { Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap };

    public ProgressBar Progress { get; } = new() { Minimum = 0, Maximum = 1, IsVisible = false };

    public TextBlock State { get; } = new() { Foreground = Muted, FontSize = 12, IsVisible = false, TextWrapping = TextWrapping.Wrap };

    public Button UpdateButton { get; } = new() { IsDefault = true, Padding = new Thickness(16, 4) };

    public Button LaterButton { get; } = new() { IsCancel = true, Padding = new Thickness(16, 4) };

    public Button SkipButton { get; } = new() { Padding = new Thickness(16, 4) };

    public async Task Update()
    {
        if (!_canInstall)
        {
            _desktop.Open(Release.PageUrl);
            Close();
            return;
        }
        var t = UiText.Current;
        UpdateButton.IsEnabled = SkipButton.IsEnabled = false;
        Progress.IsVisible = State.IsVisible = true;
        State.Text = string.Format(CultureInfo.InvariantCulture, t.UpdateDownloading, 0);
        _download = new CancellationTokenSource();
        var progress = new Progress<double>(f =>
        {
            Progress.Value = f;
            State.Text = string.Format(CultureInfo.InvariantCulture, t.UpdateDownloading, f * 100);
        });
        try
        {
            var installer = await _updates.Service.DownloadInstallerAsync(Release, progress, _download.Token);
            State.Text = t.UpdateStarting;
            _desktop.LaunchInstaller(installer, quiet: false);
            Installing?.Invoke();
        }
        catch (OperationCanceledException) when (_download.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or InvalidOperationException
                                       or OperationCanceledException or NotSupportedException or System.ComponentModel.Win32Exception)
        {
            State.Text = string.Format(CultureInfo.InvariantCulture, t.UpdateFailed, ex.Message);
            Progress.IsVisible = false;
            _canInstall = false;
            UpdateButton.Content = t.UpdateOpenPage;
            UpdateButton.IsEnabled = SkipButton.IsEnabled = true;
        }
    }

    public event Action? Installing;

    protected override void OnClosed(EventArgs e)
    {
        _download?.Cancel();
        base.OnClosed(e);
    }
}
