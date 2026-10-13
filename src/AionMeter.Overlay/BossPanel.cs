using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class BossPanel : StackPanel
{
    private static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(0xA8, 0xB3, 0xC7));

    private readonly StackPanel _rows = new();
    private readonly TextBlock _empty = new() { Text = "Sin jefes para este servidor todavía. Abre el mapa del juego para llenar la lista.", Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(6, 4) };
    private readonly List<BossRowControl> _controls = [];
    private bool _actions = true;

    public BossPanel(BossBoard board, Action changed)
    {
        Board = board;
        Changed = changed;
        Children.Add(_rows);
        Children.Add(_empty);
    }

    public BossBoard Board { get; }

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
        for (var i = 0; i < views.Count; i++) _controls[i].Show(views[i]);
        _empty.IsVisible = views.Count == 0;
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
        return row;
    }

    private void Wire(Button button, Action action) =>
        button.Click += (_, _) =>
        {
            action();
            Changed();
        };
}
