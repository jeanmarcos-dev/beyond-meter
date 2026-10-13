using AionMeter.Core.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class BossPanel : StackPanel
{
    private static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(0xA8, 0xB3, 0xC7));

    private readonly StackPanel _rows = new();
    private readonly TextBlock _empty = new() { Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(6, 4) };
    private readonly List<BossRowControl> _controls = [];
    private readonly TextBlock _alertLabel = new() { Foreground = Muted, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _hint = new() { Foreground = Muted, FontSize = 10.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
    private readonly Func<int> _alertMinutes;
    private readonly Action<int> _setAlertMinutes;
    private bool _actions = true;

    public BossPanel(BossBoard board, Action changed, Func<int>? alertMinutes = null, Action<int>? setAlertMinutes = null)
    {
        Board = board;
        Changed = changed;
        _alertMinutes = alertMinutes ?? (() => 0);
        _setAlertMinutes = setAlertMinutes ?? (_ => { });
        ServerButton.Flyout = Menu(ServerMenu);
        AlertButton.Flyout = Menu(AlertMenu);
        AllButton.Click += (_, _) => Filter(false);
        WatchedButton.Click += (_, _) => Filter(true);
        var filters = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { AllButton, WatchedButton } };
        var alerts = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { _alertLabel, AlertButton } };
        Children.Add(new WrapPanel { ItemSpacing = 8, LineSpacing = 4, Margin = new Thickness(0, 2, 0, 4), Children = { ServerButton, filters, alerts } });
        Children.Add(_hint);
        Children.Add(_rows);
        Children.Add(_empty);
        ApplyTexts();
    }

    public Button ServerButton { get; } = new() { Name = "ServerButton", FontSize = 11, Padding = new Thickness(6, 1) };

    public ToggleButton AllButton { get; } = new() { FontSize = 11, Padding = new Thickness(6, 1), IsChecked = true };

    public ToggleButton WatchedButton { get; } = new() { FontSize = 11, Padding = new Thickness(6, 1) };

    public Button AlertButton { get; } = new() { Name = "AlertButton", FontSize = 11, Padding = new Thickness(6, 1) };

    public void ApplyTexts()
    {
        var t = UiText.Current;
        _empty.Text = t.OvNoBosses;
        _hint.Text = t.TimersHint;
        _alertLabel.Text = t.AlertLabel;
        AllButton.Content = t.FilterAll;
        WatchedButton.Content = t.FilterWatched;
        ToolTip.SetTip(ServerButton, t.TipServer);
        ShowHeader();
        foreach (var control in _controls) control.ApplyTexts();
    }

    public IReadOnlyList<MenuItem> ServerMenu() =>
        Board.Servers().Select(choice => Item(choice.Label, choice.Shown, () => Board.ShowServer(choice.Server))).ToList();

    public IReadOnlyList<MenuItem> AlertMenu()
    {
        var current = _alertMinutes();
        return BossBoard.AlertChoices.Select(minutes => Item(BossBoard.AlertChoice(minutes), minutes == current, () => _setAlertMinutes(minutes))).ToList();
    }

    public IReadOnlyList<MenuItem> IntervalMenu(BossRowControl row) =>
        BossBoard.PresetsFor(row.RespawnMinutes)
            .Select(minutes => Item(BossBoard.IntervalChoice(minutes), minutes == row.RespawnMinutes, () => Board.SetInterval(row.NpcCode, minutes)))
            .ToList();

    private void Filter(bool watchedOnly)
    {
        Board.WatchedOnly = watchedOnly;
        Changed();
    }

    private void ShowHeader()
    {
        ServerButton.Content = Board.ServerLabel(Board.ShownServer) + " ▾";
        AlertButton.Content = BossBoard.AlertChoice(_alertMinutes()) + " ▾";
        AllButton.IsChecked = !Board.WatchedOnly;
        WatchedButton.IsChecked = Board.WatchedOnly;
    }

    private static MenuFlyout Menu(Func<IReadOnlyList<MenuItem>> items)
    {
        var flyout = new MenuFlyout();
        flyout.Opening += (_, _) =>
        {
            flyout.Items.Clear();
            foreach (var item in items()) flyout.Items.Add(item);
        };
        return flyout;
    }

    private MenuItem Item(string header, bool chosen, Action pick)
    {
        var item = new MenuItem { Header = header, ToggleType = MenuItemToggleType.CheckBox, IsChecked = chosen };
        item.Click += (_, _) =>
        {
            pick();
            Changed();
        };
        return item;
    }

    public BossBoard Board { get; }

    public ImageStore? Images { get; set; }

    public Action Changed { get; }

    public IReadOnlyList<BossRowControl> Rows => _controls;

    public void Show(IReadOnlyList<BossRowView> views)
    {
        var codes = views.Select(v => v.NpcCode).ToList();
        if (!codes.SequenceEqual(_controls.Select(c => c.NpcCode)))
        {
            _controls.Clear();
            _rows.Children.Clear();
            foreach (var code in codes)
            {
                var row = Build(code);
                _controls.Add(row);
                _rows.Children.Add(row);
            }
        }
        for (var i = 0; i < views.Count; i++)
        {
            _controls[i].Show(views[i]);
            _controls[i].ShowPortrait(Images?.Portrait(views[i].NpcCode, 0));
        }
        _empty.IsVisible = views.Count == 0;
        ShowHeader();
        ShowActions(_actions);
    }

    public void ShowActions(bool visible)
    {
        _actions = visible;
        foreach (var row in _controls) row.ShowActions(visible);
    }

    private BossRowControl Build(int code)
    {
        var row = new BossRowControl(code);
        Wire(row.WatchButton, () => Board.ToggleWatch(code));
        Wire(row.LessButton, () => Board.StepInterval(code, -1));
        Wire(row.MoreButton, () => Board.StepInterval(code, 1));
        Wire(row.DeadButton, () => Board.MarkDead(code));
        Wire(row.RemoveButton, () => Board.Remove(code));
        row.IntervalButton.Flyout = Menu(() => IntervalMenu(row));
        return row;
    }

    private void Wire(Button button, Action action) =>
        button.Click += (_, _) =>
        {
            action();
            Changed();
        };
}
