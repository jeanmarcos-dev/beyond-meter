using AionMeter.Core.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class BossRowControl : StackPanel
{
    private static readonly IBrush Gold = new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42));
    private static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(0xA8, 0xB3, 0xC7));
    private static readonly IBrush Green = new SolidColorBrush(Color.FromRgb(0x6F, 0xD0, 0x8C));

    private readonly TextBlock _name = new() { FontSize = 13, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _status = new() { FontSize = 13, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly TextBlock _detail = new() { FontSize = 11, Foreground = Muted, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _interval = new() { FontSize = 11, Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0), MinWidth = 52, TextAlignment = TextAlignment.Center };
    private readonly WrapPanel _actions = new() { ItemSpacing = 3, LineSpacing = 3 };
    private bool _watch;

    public BossRowControl(int npcCode)
    {
        NpcCode = npcCode;
        Margin = new Thickness(0, 3);
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        Grid.SetColumn(_name, 1);
        Grid.SetColumn(_status, 2);
        top.Children.Add(Portrait);
        top.Children.Add(_name);
        top.Children.Add(_status);
        _actions.Children.Add(WatchButton);
        _actions.Children.Add(LessButton);
        _actions.Children.Add(_interval);
        _actions.Children.Add(MoreButton);
        _actions.Children.Add(DeadButton);
        _actions.Children.Add(RemoveButton);
        Children.Add(top);
        Children.Add(_detail);
        Children.Add(_actions);
        ApplyTexts();
    }

    public int NpcCode { get; }

    public Image Portrait { get; } = new() { Width = 28, Height = 28, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, IsVisible = false };

    public void ShowPortrait(IImage? image)
    {
        Portrait.Source = image;
        Portrait.IsVisible = image is not null;
    }

    public Button WatchButton { get; } = Small("");

    public Button LessButton { get; } = Small("−");

    public Button MoreButton { get; } = Small("+");

    public Button DeadButton { get; } = Small("");

    public Button RemoveButton { get; } = Small("");

    public void ApplyTexts()
    {
        WatchButton.Content = _watch ? UiText.Current.OvWatched : UiText.Current.OvWatch;
        DeadButton.Content = UiText.Current.BtnKilled;
        RemoveButton.Content = UiText.Current.OvRemove;
    }

    public void Show(BossRowView view)
    {
        _name.Text = view.Name;
        _status.Text = view.Status;
        _status.Foreground = view.State switch
        {
            BossState.Countdown => Gold,
            BossState.Unknown => Muted,
            _ => Green,
        };
        _detail.Text = string.IsNullOrEmpty(view.Detail) ? view.Zone : $"{view.Zone} · {view.Detail}";
        _interval.Text = view.Interval;
        _watch = view.Watch;
        ApplyTexts();
    }

    public void ShowActions(bool visible) => _actions.IsVisible = visible;

    private static Button Small(string text) => new() { Content = text, FontSize = 11, Padding = new Thickness(6, 1) };
}
