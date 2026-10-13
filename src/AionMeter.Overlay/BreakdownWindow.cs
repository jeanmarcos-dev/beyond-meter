using AionMeter.Core.Combat;
using AionMeter.Core.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace AionMeter.Overlay;

public sealed class BreakdownWindow : Window
{
    private const long RefreshIntervalMs = 1_000;
    private const string DotTag = "DoT";
    private const int TimelineTab = 2;
    private const int RotationTab = 3;

    private static readonly SkillColumn?[] Columns =
    [
        SkillColumn.Name, SkillColumn.Hits, null, SkillColumn.Damage, SkillColumn.Dps, SkillColumn.Average, SkillColumn.Max, SkillColumn.Share,
        SkillColumn.Crit, SkillColumn.Back, SkillColumn.Front, SkillColumn.Perfect, SkillColumn.Heavy, SkillColumn.Multi,
    ];

    private static readonly IBrush Gold = new SolidColorBrush(BreakdownPalette.Gold);
    private static readonly IBrush Muted = new SolidColorBrush(BreakdownPalette.Muted);

    private readonly BreakdownSource _source;
    private readonly Func<long> _clock;
    private readonly DispatcherTimer? _timer;
    private readonly TextBlock _title = new() { Foreground = Gold, FontSize = 15, FontWeight = FontWeight.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _playerName = new() { Name = "PlayerName", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 6) };
    private readonly ListBox _players = new() { Name = "Players", Width = 190, Background = Brushes.Transparent };
    private readonly TabControl _tabs = new();
    private readonly StackPanel _skillRows = new() { Name = "SkillRows" };
    private readonly TimelineChart _chart = new() { MinHeight = 240 };
    private readonly RotationStrip _strip = new();
    private readonly ScrollViewer _stripScroll = new() { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    private readonly TextBlock _takenTotal = Stat("TakenTotal");
    private readonly TextBlock _takenPerSecond = Stat("TakenPerSecond");
    private readonly TextBlock _takenHits = Stat("TakenHits");
    private readonly TextBlock _takenTotalLabel = Label();
    private readonly TextBlock _takenPerSecondLabel = Label();
    private readonly TextBlock _takenHitsLabel = Label();
    private readonly TextBlock _dotTicks = Stat("DotTicks");
    private readonly TextBlock _dotTicksLabel = Label();
    private readonly TextBlock _defenseNote = new() { Foreground = Muted, FontSize = 11, TextWrapping = TextWrapping.Wrap };
    private readonly WrapPanel _accuracy = new() { Name = "AccuracyTiles", ItemSpacing = 8, LineSpacing = 8, Margin = new Thickness(0, 10, 0, 0) };
    private readonly WrapPanel _playerTiles = new() { Name = "PlayerTiles", ItemSpacing = 6, LineSpacing = 6, Margin = new Thickness(0, 0, 0, 8) };
    private readonly TextBlock _playerMeta = new() { Name = "PlayerMeta", Foreground = Muted, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, -4, 0, 6) };
    private readonly TextBlock _fightMeta = new() { Name = "FightMeta", Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap };
    private readonly Func<int, string> _serverName;
    private SkillOrder _order;
    private readonly List<TabItem> _tabItems = [];
    private readonly Image _portrait = new() { Name = "FightPortrait", Width = 42, Height = 42, Margin = new Thickness(0, 0, 10, 0), IsVisible = false };
    private readonly ImageStore? _images;

    public const string SkillIconName = "SkillIcon";

    private readonly Button _chat = new() { Name = "ChatButton", FontSize = 12 };
    private readonly Button _image = new() { Name = "CopyImage", Content = "🖼", FontSize = 12, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _note = new() { Name = "CopyNote", FontSize = 12, Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), IsVisible = false };

    public string? LastCopied { get; private set; }

    public Button ChatButton => _chat;

    public PixelSize? LastImage { get; private set; }

    public async Task CopyImage()
    {
        if (Content is not Control root || Clipboard is not { } clipboard)
        {
            ShowNote(false);
            return;
        }
        var scaling = RenderScaling;
        var size = new PixelSize(Math.Max(1, (int)(root.Bounds.Width * scaling)), Math.Max(1, (int)(root.Bounds.Height * scaling)));
        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size, new Vector(96 * scaling, 96 * scaling));
        bitmap.Render(root);
        await clipboard.SetBitmapAsync(bitmap);
        LastImage = size;
        ShowNote(true);
    }

    private void ShowNote(bool copied)
    {
        _note.Text = copied ? UiText.Current.Copied : UiText.Current.CopyFailed;
        _note.IsVisible = true;
        DispatcherTimer.RunOnce(() => _note.IsVisible = false, TimeSpan.FromSeconds(2.5));
    }

    private async Task Copy(string text)
    {
        var copied = Clipboard is { } clipboard;
        if (copied) await Clipboard!.SetTextAsync(text);
        LastCopied = copied ? text : null;
        ShowNote(copied);
    }
    private TextBlock? _rotationHint;
    private long _lastTickMs;
    private bool _syncing;

    public BreakdownWindow(BreakdownSource source, uint actorId, double width, double height, Func<long>? clock = null, ImageStore? images = null, Func<int, string>? serverName = null, SkillOrder? order = null)
    {
        _source = source;
        _order = order ?? SkillOrder.Default;
        _serverName = serverName ?? (id => id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _images = images;
        if (images is not null) images.Changed += Render;
        _clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        ActorId = actorId;
        _lastTickMs = _clock();

        Width = width;
        Height = height;
        MinWidth = 640;
        MinHeight = 400;
        Background = new SolidColorBrush(BreakdownPalette.Background);
        Content = BuildLayout();
        ApplyTexts();

        _players.ItemTemplate = new FuncDataTemplate<RowView?>((row, _) => row is null ? new Panel() : PlayerLine(row), supportsRecycling: false);
        _players.SelectionChanged += (_, _) =>
        {
            if (!_syncing && _players.SelectedItem is RowView row && row.ActorId != ActorId) Select(row.ActorId);
        };
        _tabs.SelectionChanged += (_, e) =>
        {
            if (e.Source == _tabs) Render();
        };
        _stripScroll.ScrollChanged += (_, _) =>
        {
            _strip.SetViewport(_stripScroll.Offset.X, _stripScroll.Viewport.Width);
        };

        Render();
        if (!source.IsLive) return;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(RefreshIntervalMs), DispatcherPriority.Background, (_, _) => Tick());
        _timer.Start();
    }

    public uint ActorId { get; private set; }

    public SkillOrder Order => _order;

    public event Action<SkillOrder>? OrderChanged;

    public void SortBy(SkillColumn column)
    {
        _order = _order.Toggle(column);
        OrderChanged?.Invoke(_order);
        Render();
    }

    public void ApplyTexts()
    {
        var t = UiText.Current;
        Title = $"{AppInfo.Name} · {t.OvBreakdown}";
        _chat.Content = t.TipChat;
        ToolTip.SetTip(_image, t.TipCopyImage);
        var names = BreakdownView.Tabs;
        for (var i = 0; i < _tabItems.Count; i++) _tabItems[i].Header = names[i];
        if (_rotationHint is not null) _rotationHint.Text = t.OvRotationHint;
        _takenTotalLabel.Text = t.OvDamageTaken;
        _takenPerSecondLabel.Text = t.OvDamageTakenPerSecond;
        _takenHitsLabel.Text = t.OvHitsTaken;
        _dotTicksLabel.Text = t.DefDotTicks;
        _defenseNote.Text = t.DefNote;
        Render();
    }

    public int RefreshCount { get; private set; }

    public void Select(uint actorId)
    {
        ActorId = actorId;
        Render();
    }

    public void SelectTab(int index) => _tabs.SelectedIndex = index;

    public void Tick()
    {
        var now = _clock();
        if (!_source.IsLive || now - _lastTickMs < RefreshIntervalMs) return;
        _lastTickMs = now;
        RefreshCount++;
        Render();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_images is not null) _images.Changed -= Render;
        _timer?.Stop();
        base.OnClosed(e);
    }

    private Control BuildLayout()
    {
        var names = BreakdownView.Tabs;
        _tabItems.AddRange(
        [
            new TabItem { Header = names[0], Content = SkillsPage() },
            new TabItem { Header = names[1], Content = new ScrollViewer { Content = _accuracy } },
            new TabItem { Header = names[2], Content = _chart },
            new TabItem { Header = names[3], Content = RotationPage() },
            new TabItem { Header = names[4], Content = DefensePage() },
        ]);
        _tabs.ItemsSource = _tabItems;

        var right = new DockPanel { Margin = new Thickness(12, 0, 0, 0) };
        var player = new StackPanel { Children = { _playerName, _playerMeta, _playerTiles } };
        DockPanel.SetDock(player, Dock.Top);
        right.Children.Add(player);
        right.Children.Add(_tabs);

        var body = new DockPanel { Margin = new Thickness(12) };
        _chat.Flyout = ChatMenu.Flyout(() => _source.Snapshot(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), () => (ActorId, _source.Detail(ActorId)?.Name), text => _ = Copy(text));
        _chat.Margin = new Thickness(12, 0, 0, 0);
        _chat.VerticalAlignment = VerticalAlignment.Center;
        _image.Click += async (_, _) => await CopyImage();
        var titleLine = new StackPanel { Orientation = Orientation.Horizontal, Children = { _title, _chat, _image, _note } };
        var heading = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8), Children = { _portrait, new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { titleLine, _fightMeta } } } };
        _title.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(heading, Dock.Top);
        body.Children.Add(heading);
        DockPanel.SetDock(_players, Dock.Left);
        body.Children.Add(_players);
        body.Children.Add(right);
        return body;
    }

    private Control SkillsPage() => new ScrollViewer { Content = _skillRows, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 6, 0, 0) };

    private Control RotationPage()
    {
        _stripScroll.Content = _strip;
        var hint = new TextBlock { Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 8) };
        _rotationHint = hint;
        return new StackPanel { Children = { hint, _stripScroll } };
    }

    private Control DefensePage() => new StackPanel
    {
        Margin = new Thickness(0, 12, 0, 0),
        Spacing = 14,
        Children =
        {
            Block(_takenTotalLabel, _takenTotal),
            Block(_takenPerSecondLabel, _takenPerSecond),
            Block(_takenHitsLabel, _takenHits),
            Block(_dotTicksLabel, _dotTicks),
            _defenseNote,
        },
    };

    private void Render()
    {
        var snapshot = _source.Snapshot(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        _portrait.Source = snapshot is null ? null : _images?.Portrait(snapshot.Boss?.NpcCode ?? 0, snapshot.TargetCode);
        _portrait.IsVisible = _portrait.Source is not null;
        _title.Text = (snapshot?.Boss?.Name ?? snapshot?.Title ?? UiText.Current.OvFightUnavailable) + " · " + MeterView.Duration(snapshot?.CombatMs ?? 0) + " · " + (_source.IsLive ? UiText.Current.OvLiveFight : UiText.Current.OvSavedFight);
        _fightMeta.Text = snapshot is null ? "" : BreakdownView.FightMeta(snapshot);
        ToolTip.SetTip(_fightMeta, snapshot?.MostDamageUnsent == true ? UiText.Current.DamageUnsentTip : null);
        var rows = MeterView.Rows(snapshot, int.MaxValue);
        _syncing = true;
        _players.ItemsSource = rows;
        _players.SelectedItem = rows.FirstOrDefault(r => r.ActorId == ActorId);
        _syncing = false;

        var detail = _source.Detail(ActorId);
        _playerName.Text = detail?.Name ?? "";
        _playerName.Foreground = new SolidColorBrush(BreakdownPalette.Class(detail?.Class ?? default));
        _playerMeta.Text = detail is null ? "" : BreakdownView.PlayerMeta(detail, _serverName);
        Tiles(_playerTiles, snapshot is null || detail is null ? [] : BreakdownView.PlayerTiles(snapshot, detail), 18);
        Tiles(_accuracy, detail is null ? [] : BreakdownView.Accuracy(detail), 22);
        FillSkills(detail);
        FillDefense(detail);
        if (detail is null) return;
        if (_tabs.SelectedIndex == TimelineTab) FillTimeline(detail, rows);
        _strip.Icon = _images is null ? null : _images.SkillIcon;
        if (_tabs.SelectedIndex == RotationTab) _strip.Show(BreakdownView.Rotation(detail));
    }

    private void FillSkills(CombatantDetail? detail)
    {
        _skillRows.Children.Clear();
        _skillRows.Children.Add(HeaderLine([UiText.Current.OvSkill, UiText.Current.OvHits, UiText.Current.OvTicks, UiText.Current.RowDamage, "DPS", UiText.Current.OvAverage, UiText.Current.OvMax, "%", UiText.Current.RowCrit, UiText.Current.OvBack, UiText.Current.OvFront, UiText.Current.OvPerfect, UiText.Current.OvHeavy, UiText.Current.OvMulti]));
        if (detail is null) return;
        foreach (var row in BreakdownView.Skills(detail, _order))
        {
            var name = row.DotOnly ? row.Name + " · " + DotTag : row.Name;
            var line = SkillLine([name, row.Hits, row.Ticks, row.Damage, row.Dps, row.Average, row.Max, row.Share, row.Crit, row.Back, row.Front, row.Perfect, row.Heavy, row.Multi], Brushes.White, FontWeight.Normal, _images?.SkillIcon(row.SkillCode));
            ToolTip.SetTip(line, row.Tooltip);
            _skillRows.Children.Add(line);
        }
    }

    private Control HeaderLine(string[] labels)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("22,200,56,50,64,64,64,64,48,48,48,48,48,52,52"), Margin = new Thickness(0, 1) };
        for (var i = 0; i < labels.Length; i++)
        {
            var label = labels[i];
            Control cell;
            if (Columns[i] is { } column)
            {
                var sorted = column == _order.Column;
                var arrow = _order.Descending ? "▾" : "▴";
                var head = new Button
                {
                    Name = "Sort" + column,
                    Content = !sorted ? label : i == 0 ? $"{label} {arrow}" : $"{arrow} {label}",
                    Foreground = sorted ? Gold : Muted,
                    Background = Brushes.Transparent,
                    FontSize = 12,
                    Padding = new Thickness(0),
                    HorizontalAlignment = i == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                };
                ToolTip.SetTip(head, UiText.Current.SortTip);
                head.Click += (_, _) => SortBy(column);
                cell = head;
            }
            else
            {
                cell = new TextBlock { Text = label, Foreground = Muted, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right };
            }
            Grid.SetColumn(cell, i + 1);
            grid.Children.Add(cell);
        }
        return grid;
    }

    private static void Tiles(Panel panel, IReadOnlyList<StatTile> tiles, double size)
    {
        panel.Children.Clear();
        foreach (var tile in tiles)
        {
            var stack = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = tile.Label, Foreground = Gold, FontSize = 10.5, FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = tile.Value, Foreground = Brushes.White, FontSize = size, FontWeight = FontWeight.Bold },
                },
            };
            if (tile.Caption.Length > 0) stack.Children.Add(new TextBlock { Text = tile.Caption, Foreground = Muted, FontSize = 10.5, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 160 });
            panel.Children.Add(new Border { Child = stack, Padding = new Thickness(10, 6), MinWidth = 86, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)) });
        }
    }

    private void FillDefense(CombatantDetail? detail)
    {
        var defense = detail is null ? null : BreakdownView.Defense(detail);
        _takenTotal.Text = defense?.Taken ?? "-";
        _takenPerSecond.Text = defense?.PerSecond ?? "-";
        _takenHits.Text = defense?.Hits ?? "-";
        _dotTicks.Text = defense?.DotTicks ?? "-";
    }

    private void FillTimeline(CombatantDetail detail, IReadOnlyList<RowView> rows)
    {
        var party = rows
            .Select(r => r.ActorId == detail.ActorId ? detail : _source.Detail(r.ActorId))
            .OfType<CombatantDetail>()
            .ToList();
        _chart.Show(BreakdownView.Timeline(detail, party));
    }

    private static Control SkillLine(string[] texts, IBrush foreground, FontWeight weight, IImage? icon = null)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("22,200,56,50,64,64,64,64,48,48,48,48,48,52,52"), Margin = new Thickness(0, 1) };
        if (icon is not null) grid.Children.Add(new Image { Name = SkillIconName, Source = icon, Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Center });
        for (var i = 0; i < texts.Length; i++)
        {
            var cell = new TextBlock
            {
                Text = texts[i],
                Foreground = foreground,
                FontSize = 12,
                FontWeight = weight,
                HorizontalAlignment = i == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(cell, i + 1);
            grid.Children.Add(cell);
        }
        return grid;
    }

    private static Control PlayerLine(RowView row)
    {
        var name = new TextBlock
        {
            Text = $"{row.Place}. {row.Name}",
            Foreground = new SolidColorBrush(BreakdownPalette.Class(row.Class)),
            FontWeight = row.IsSelf ? FontWeight.Bold : FontWeight.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var share = new TextBlock { Text = row.Share, Foreground = Muted, FontSize = 11, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(share, 1);
        var text = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { name, share } };
        return new Border
        {
            BorderBrush = row.IsSelf ? Brushes.White : null,
            BorderThickness = new Thickness(row.IsSelf ? 1 : 0),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 2),
            Child = text,
        };
    }

    private static TextBlock Stat(string name) => new() { Name = name, FontSize = 28, FontWeight = FontWeight.Bold, Foreground = Brushes.White };

    private static Control Block(TextBlock label, TextBlock value) => new StackPanel
    {
        Children = { label, value },
    };

    private static TextBlock Label() => new() { Foreground = Muted, FontSize = 12 };
}
