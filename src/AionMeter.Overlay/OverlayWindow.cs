using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Settings;
using AionMeter.Core.Text;
using AionMeter.Overlay.Platform;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace AionMeter.Overlay;

public sealed class OverlayWindow : Window
{
    private static readonly IBrush Gold = new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42));
    private static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(0xA8, 0xB3, 0xC7));
    private static string CopyLabel => UiText.Current.OvCopyDps;
    private static string CopiedLabel => UiText.Current.OvCopied;

    private static readonly IBrush Win = new SolidColorBrush(Color.FromRgb(0x6B, 0xD1, 0x7F));
    private static readonly IBrush Loss = new SolidColorBrush(Color.FromRgb(0xE5, 0x5B, 0x5B));
    private static readonly IBrush FilterOn = new SolidColorBrush(Color.FromArgb(0x70, 0xF5, 0xC5, 0x42));

    private static readonly IBrush HistoryTint = new SolidColorBrush(Color.FromRgb(0x7D, 0xC4, 0xFF));

    private readonly MeterSession _session;
    private readonly VisibilityPolicy _policy;
    private readonly Func<long> _clock;
    private readonly string _settingsPath;
    private readonly IDesktopPlatform? _desktop;
    private readonly DispatcherTimer _timer;
    private readonly Border _frame = new() { CornerRadius = new CornerRadius(8), Padding = new Thickness(8) };
    private readonly TextBlock _title = new() { Foreground = Gold, FontSize = 15, FontWeight = FontWeight.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _time = new() { Foreground = Gold, FontSize = 15, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly TextBlock _recording = new() { Name = "RecordingBadge", Text = "● REC", Foreground = Brushes.Red, FontSize = 12, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), IsVisible = false };
    private readonly TextBlock _totals = new() { Foreground = Muted, FontSize = 12 };
    private readonly TextBlock _source = new() { Foreground = Muted, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly StackPanel _rows = new();
    private readonly List<RowControl> _rowControls = [];
    private readonly WrapPanel _toolbar = new() { ItemSpacing = 4, LineSpacing = 4, Margin = new Thickness(0, 6, 0, 0) };
    private readonly ListBox _history = new() { MaxHeight = 260, Background = Brushes.Transparent };
    private readonly Border _historyPanel = new() { Margin = new Thickness(0, 6, 0, 0), IsVisible = false };
    private readonly SkillPanel _skills = new() { IsVisible = false };
    private readonly Action<UserSettings> _tune;
    private readonly BossBoard? _board;
    private readonly BossPanel? _bossPanel;
    private readonly TextBlock _nextBoss = new() { Foreground = Gold, FontSize = 12, Margin = new Thickness(6, 2), IsVisible = false, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _shortcutHint = new() { Name = "ShortcutHint", FontSize = 11, Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, IsVisible = false };
    private IReadOnlyDictionary<OverlayCommand, string> _shortcuts = new Dictionary<OverlayCommand, string>();
    private readonly Border _grip = new() { Name = "WidthGrip", Width = 18, Height = 14, Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.SizeWestEast), HorizontalAlignment = HorizontalAlignment.Right, Child = new TextBlock { Text = "⇔", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center } };
    private Control? _columnHeader;
    private bool _bossPanelOpen;
    private bool _resizing;
    private double _resizeStartX;
    private double _resizeStartWidth;
    private readonly Button _mode = new() { FontSize = 12, Padding = new Thickness(8, 2) };
    private readonly Button _view = new() { FontSize = 12, Padding = new Thickness(8, 2) };
    private readonly List<TextBlock> _columnCells = [];
    private ViewMode _viewMode = ViewMode.Damage;
    private UserSettings _settings;
    private readonly Button _close = new() { Content = "✕", FontSize = 12, Padding = new Thickness(8, 2), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top };
    private readonly Button _copy = new() { FontSize = 12, Padding = new Thickness(8, 2) };
    private readonly Button _language = new() { Name = "LanguageButton", FontSize = 12, Padding = new Thickness(8, 2) };
    private readonly List<Action> _texts = [];
    private readonly Button _filterBosses = new() { FontSize = 12, Padding = new Thickness(8, 2) };
    private readonly Button _filterKills = new() { FontSize = 12, Padding = new Thickness(8, 2) };
    private readonly TextBlock _historyCount = new() { Name = "HistoryCount", Foreground = Muted, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0) };
    private readonly Button _delete = new() { FontSize = 12, Padding = new Thickness(8, 2), IsVisible = false };
    private readonly Button _confirmYes = new() { FontSize = 12, Padding = new Thickness(8, 2) };
    private readonly Button _confirmNo = new() { FontSize = 12, Padding = new Thickness(8, 2) };
    private readonly StackPanel _confirmBar = new() { Orientation = Orientation.Horizontal, Spacing = 4, IsVisible = false };
    private readonly DeleteConfirmation _confirmation;
    private bool _historyDirty = true;

    public OverlayWindow(MeterSession session, UserSettings settings, string settingsPath, IDesktopPlatform? desktop, Action<UserSettings>? tune = null, BossBoard? board = null, bool startHidden = false, Func<long>? clock = null)
    {
        _policy = new VisibilityPolicy(startVisible: !startHidden);
        _clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        _confirmation = new DeleteConfirmation(_clock);
        _board = board;
        if (board is not null) _bossPanel = new BossPanel(board, Refresh) { IsVisible = false };
        _tune = tune ?? (_ => { });
        _session = session;
        _settings = settings;
        _settingsPath = settingsPath;
        _desktop = desktop;

        Title = AppInfo.Name;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;
        SizeToContent = SizeToContent.Height;
        Width = settings.OverlayWidth;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = SavedPosition();

        EnsureRows(settings.MaxRows);

        _frame.Child = BuildLayout();
        Content = _frame;
        ApplyTexts();

        _session.History.Changed += () => Dispatcher.UIThread.Post(() => _historyDirty = true);
        _history.SelectionChanged += (_, _) =>
        {
            if (_history.SelectedItem is HistoryItemView item && item.Path != _session.SelectedFight) _session.Show(item.Path);
        };
        Opened += (_, _) => ApplyLock();

        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => Refresh());
        _timer.Start();
        Redraw();
    }

    public event Action? QuitRequested;

    public event Action? LanguageChanged;

    public static readonly TimeSpan DemoLength = TimeSpan.FromSeconds(45);

    public Func<SettingsContext>? SettingsSource { get; set; }

    public Func<string, IEventSource>? ReplaySource { get; set; }

    public bool Replay(string path)
    {
        if (ReplaySource is not { } create || ReplayFile.Problem(path) is not null) return false;
        _session.ReplayFile(create(path));
        Reveal();
        return true;
    }

    public async Task PickReplay()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = UiText.Current.OvReplayTitle,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(UiText.Current.OvCaptureFiles) { Patterns = ReplayFile.Patterns }],
        });
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path) Replay(path);
    }

    private void Reveal()
    {
        _policy.Show();
        SetVisible(true);
    }

    public SettingsWindow? SettingsDialog { get; private set; }

    public event Action<UserSettings, UserSettings>? SettingsChanged;

    public void ChangeLanguage(string language)
    {
        var next = UserSettings.Parse(_settings.ToJson());
        next.Language = UiText.Normalize(language);
        UseSettings(next);
    }

    public void UseSettings(UserSettings settings)
    {
        var previous = _settings;
        settings.OverlayLeft = previous.OverlayLeft;
        settings.OverlayTop = previous.OverlayTop;
        settings.OverlayWidth = previous.OverlayWidth;
        settings.Locked = previous.Locked;
        settings.BreakdownWidth = previous.BreakdownWidth;
        settings.BreakdownHeight = previous.BreakdownHeight;
        _settings = settings;
        EnsureRows(settings.MaxRows);
        _tune(settings);
        SaveSettings();
        if (UiText.Normalize(settings.Language) != UiText.Current.Code)
        {
            UiText.Use(settings.Language);
            ApplyTexts();
            Breakdown?.ApplyTexts();
            LanguageChanged?.Invoke();
        }
        ApplyLock();
        SettingsChanged?.Invoke(previous, settings);
        Refresh();
    }

    private void OpenSettings()
    {
        if (SettingsDialog is { } open)
        {
            open.Activate();
            return;
        }
        if (SettingsSource is not { } source) return;
        var dialog = new SettingsWindow(_settings, source());
        dialog.Saved += UseSettings;
        dialog.Closed += (_, _) => SettingsDialog = null;
        SettingsDialog = dialog;
        dialog.Show();
    }

    private void EnsureRows(int count)
    {
        while (_rowControls.Count < count)
        {
            var row = new RowControl { IsVisible = false };
            row.PointerPressed += (_, _) =>
            {
                if (_settings.Locked) return;
                _session.ToggleDetail(row.ActorId);
                Refresh();
            };
            _rowControls.Add(row);
            _rows.Children.Add(row);
        }
        while (_rowControls.Count > count)
        {
            _rows.Children.Remove(_rowControls[^1]);
            _rowControls.RemoveAt(_rowControls.Count - 1);
        }
    }

    private void ApplyTexts()
    {
        foreach (var apply in _texts) apply();
        _skills.ApplyTexts();
        _bossPanel?.ApplyTexts();
        ShowShortcutHint();
        Refresh();
    }

    private void Text(Action apply) => _texts.Add(apply);

    public BreakdownWindow? Breakdown { get; private set; }

    public void Apply(OverlayCommand command)
    {
        switch (command)
        {
            case OverlayCommand.Show:
                _policy.Show();
                SetVisible(true);
                break;
            case OverlayCommand.Toggle:
                _policy.Toggle();
                SetVisible(_policy.Visible);
                break;
            case OverlayCommand.Lock:
                _settings.Locked = !_settings.Locked;
                ApplyLock();
                SaveSettings();
                break;
            case OverlayCommand.Reset:
                _session.Reset();
                break;
            case OverlayCommand.Live:
                _session.ShowLive();
                break;
            case OverlayCommand.Older:
                _session.Older();
                break;
            case OverlayCommand.Newer:
                _session.Newer();
                break;
            case OverlayCommand.Skills:
                _session.ToggleSelfDetail();
                break;
            case OverlayCommand.Timers:
                if (_bossPanel is not null) _bossPanelOpen = !_bossPanelOpen;
                break;
            case OverlayCommand.Breakdown:
                OpenBreakdown(CurrentSnapshot()?.Combatants.FirstOrDefault(c => c.IsSelf)?.ActorId);
                break;
            case OverlayCommand.Copy:
                _ = Copy(MeterView.PartyLine(CurrentSnapshot()), _copy, CopyLabel);
                break;
            case OverlayCommand.CopySelf:
                _ = Copy(SelfLine(), null, null);
                break;
            case OverlayCommand.Mode:
                _session.Reset();
                _settings.TargetMode = _settings.TargetMode == TargetMode.All ? TargetMode.BossOnly : TargetMode.All;
                _tune(_settings);
                SaveSettings();
                break;
            case OverlayCommand.Heal:
                _viewMode = _viewMode == ViewMode.Damage ? ViewMode.Healing : ViewMode.Damage;
                break;
            case OverlayCommand.Reload:
                _settings = Reloaded();
                _tune(_settings);
                ApplyLock();
                break;
            case OverlayCommand.Settings:
                OpenSettings();
                return;
            case OverlayCommand.History:
                Reveal();
                _historyPanel.IsVisible = true;
                ReloadHistory();
                break;
            case OverlayCommand.Demo:
                _session.StartDemo(DemoLength);
                Reveal();
                break;
            case OverlayCommand.Record:
                _session.ToggleRecording();
                break;
            case OverlayCommand.Quit:
                _timer.Stop();
                SaveSettings();
                QuitRequested?.Invoke();
                return;
        }
        Refresh();
    }

    public bool Locked => _settings.Locked;

    public UserSettings Settings => _settings;

    private PixelPoint SavedPosition() => new((int)Math.Round(_settings.OverlayLeft), (int)Math.Round(_settings.OverlayTop));

    private UserSettings Reloaded()
    {
        var fresh = UserSettings.Load(_settingsPath);
        fresh.OverlayLeft = _settings.OverlayLeft;
        fresh.OverlayTop = _settings.OverlayTop;
        fresh.OverlayWidth = _settings.OverlayWidth;
        fresh.Locked = _settings.Locked;
        fresh.MaxRows = _settings.MaxRows;
        fresh.BreakdownWidth = _settings.BreakdownWidth;
        fresh.BreakdownHeight = _settings.BreakdownHeight;
        return fresh;
    }

    public void SaveSettings()
    {
        if (IsVisible && TryGetPlatformHandle() is { } handle && _desktop?.WindowPosition(handle.Handle) is { } root)
        {
            _settings.OverlayLeft = root.X;
            _settings.OverlayTop = root.Y;
        }
        _settings.Save(_settingsPath);
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        base.OnClosed(e);
    }

    private Control BuildLayout()
    {
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto"), Background = Brushes.Transparent };
        Place(header, _title, 0, 0);
        Place(header, new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Children = { _recording, _time } }, 0, 1);
        Place(header, _totals, 1, 0);
        Place(header, _source, 1, 1);
        Place(header, _close, 0, 2);
        Grid.SetRowSpan(_close, 2);
        _close.Click += (_, _) => Apply(OverlayCommand.Quit);
        header.PointerPressed += (_, e) =>
        {
            if (!_settings.Locked && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
        };

        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("*,64,64,40,40"), Margin = new Thickness(6, 4, 6, 2) };
        _columnHeader = columns;
        var names = MeterView.Columns(ViewMode.Damage);
        for (var i = 0; i < names.Count; i++)
        {
            var cell = new TextBlock { Text = names[i], Foreground = Muted, FontSize = 11, HorizontalAlignment = i == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right };
            Grid.SetColumn(cell, i);
            columns.Children.Add(cell);
            _columnCells.Add(cell);
        }

        _toolbar.Children.Add(ToolButton(t => t.Live, () => Apply(OverlayCommand.Live)));
        _toolbar.Children.Add(ToolButton(_ => "◀", () => Apply(OverlayCommand.Older)));
        _toolbar.Children.Add(ToolButton(_ => "▶", () => Apply(OverlayCommand.Newer)));
        _toolbar.Children.Add(ToolButton(t => t.OvHistory, ToggleHistory));
        var deleteAsk = new TextBlock { Foreground = Gold, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        Text(() => deleteAsk.Text = UiText.Current.OvDeleteAsk);
        _confirmBar.Children.Add(deleteAsk);
        _confirmBar.Children.Add(_confirmYes);
        _confirmBar.Children.Add(_confirmNo);
        _toolbar.Children.Add(_delete);
        _toolbar.Children.Add(_confirmBar);
        _delete.Click += (_, _) => RequestDelete();
        _confirmYes.Click += (_, _) => ConfirmDelete();
        _confirmNo.Click += (_, _) => CancelDelete();
        _toolbar.Children.Add(ToolButton(t => t.OvMySkills, () => Apply(OverlayCommand.Skills)));
        if (_bossPanel is not null) _toolbar.Children.Add(ToolButton(t => t.OvRespawns, () => Apply(OverlayCommand.Timers)));
        _toolbar.Children.Add(ToolButton(t => t.OvReset, () => Apply(OverlayCommand.Reset)));
        _toolbar.Children.Add(_view);
        _view.Click += (_, _) => Apply(OverlayCommand.Heal);
        _toolbar.Children.Add(_mode);
        _mode.Click += (_, _) => Apply(OverlayCommand.Mode);
        _toolbar.Children.Add(_copy);
        _copy.Click += (_, _) => Apply(OverlayCommand.Copy);
        _skills.DetailButton.Click += (_, _) => OpenBreakdown(_session.Detail(CurrentSnapshot())?.ActorId);
        _skills.CopyButton.Click += (_, _) => _ = Copy(DetailLine(), _skills.CopyButton, SkillPanel.CopyLabel);
        _toolbar.Children.Add(ToolButton(t => t.OvLock, () => Apply(OverlayCommand.Lock)));
        _toolbar.Children.Add(_language);
        var settings = new Button { Content = "⚙", FontSize = 12, Padding = new Thickness(8, 2) };
        settings.Click += (_, _) => Apply(OverlayCommand.Settings);
        Text(() => ToolTip.SetTip(settings, UiText.Current.OvSettingsTip));
        _toolbar.Children.Add(settings);
        _language.Flyout = LanguageMenu();
        Text(() => _copy.Content = CopyLabel);
        Text(() => _filterBosses.Content = UiText.Current.OvBossesOnly);
        Text(() => _filterKills.Content = UiText.Current.OvKillsOnly);
        Text(() => _delete.Content = UiText.Current.OvDelete);
        Text(() => _confirmYes.Content = UiText.Current.OvYes);
        Text(() => _confirmNo.Content = UiText.Current.OvNo);
        Text(() =>
        {
            _language.Content = UiText.Current.Code.ToUpperInvariant();
            ToolTip.SetTip(_language, UiText.Current.LanguageMenu);
        });
        _grip.PointerPressed += OnGripPressed;
        _grip.PointerMoved += OnGripMoved;
        _grip.PointerReleased += (_, e) => EndResize(e.Pointer);
        _grip.PointerCaptureLost += (_, _) => EndResize(null);

        _history.ItemTemplate = new FuncDataTemplate<HistoryItemView?>((item, _) => item is null ? new Panel() : HistoryLine(item), supportsRecycling: false);
        var filters = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 0, 0, 4), Children = { _filterBosses, _filterKills, _historyCount } };
        _filterBosses.Click += (_, _) => SetFilter(_session.Filter with { BossesOnly = !_session.Filter.BossesOnly });
        _filterKills.Click += (_, _) => SetFilter(_session.Filter with { KillsOnly = !_session.Filter.KillsOnly });
        _historyPanel.Child = new StackPanel { Children = { filters, _history } };

        var layout = new StackPanel { Children = { header, _nextBoss, columns, _rows, _skills } };
        if (_bossPanel is not null) layout.Children.Add(_bossPanel);
        layout.Children.Add(_toolbar);
        layout.Children.Add(_historyPanel);
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 4, 0, 0) };
        footer.Children.Add(_shortcutHint);
        Grid.SetColumn(_grip, 1);
        footer.Children.Add(_grip);
        layout.Children.Add(footer);
        return layout;
    }

    public bool Refreshing => _timer.IsEnabled;

    public IReadOnlyDictionary<OverlayCommand, string> Shortcuts
    {
        get => _shortcuts;
        set
        {
            _shortcuts = value;
            ShowShortcutHint();
        }
    }

    public void Refresh()
    {
        Redraw();
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        var input = new VisibilityInput(_clock(), _session.GameRunning, _session.Tracker.LiveFight() is not null, _settings.Locked, _settings.AutoHideSeconds);
        var visible = _policy.Update(input);
        if (visible != IsVisible) SetVisible(visible);
    }

    private void Redraw()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _session.Tick(now);
        var snapshot = _session.Current(now);
        var header = MeterView.Header(snapshot, _session.Status, _session.SelectedIndex(), _session.Filtered().Count, _viewMode, _session.IsReplay, _session.IsRecording);
        _recording.IsVisible = header.Recording;
        _title.Text = header.Title;
        _time.Text = header.Time;
        _totals.Text = header.Totals;
        _source.Text = header.Source;
        _source.Foreground = header.IsLive ? Muted : HistoryTint;
        _frame.BorderBrush = header.IsLive ? (_settings.Locked ? null : Gold) : HistoryTint;

        var browsing = !_session.IsLive;
        var asking = browsing && _confirmation.IsPending(_session.SelectedFight);
        _delete.IsVisible = browsing && !asking;
        _confirmBar.IsVisible = asking;
        _filterBosses.Background = _session.Filter.BossesOnly ? FilterOn : null;
        _filterKills.Background = _session.Filter.KillsOnly ? FilterOn : null;

        _mode.Content = _settings.TargetMode == TargetMode.All ? UiText.Current.OvAll : UiText.Current.FilterBosses;

        _view.Content = _viewMode == ViewMode.Healing ? UiText.Current.OvHealing : UiText.Current.RowDamage;
        var columns = MeterView.Columns(_viewMode);
        for (var i = 0; i < _columnCells.Count; i++) _columnCells[i].Text = columns[i];

        var rows = MeterView.Rows(snapshot, _rowControls.Count, _viewMode);
        for (var i = 0; i < _rowControls.Count; i++)
        {
            if (i < rows.Count) _rowControls[i].Show(rows[i]);
            else _rowControls[i].IsVisible = false;
        }

        var detail = _session.Detail(snapshot);
        _skills.IsVisible = detail is not null && !_bossPanelOpen;
        if (detail is not null) _skills.Show(MeterView.Detail(detail, maxSkills: 15));
        RefreshBosses();

        if (_historyPanel.IsVisible && _historyDirty) ReloadHistory();
    }

    private void RefreshBosses()
    {
        if (_board is null || _bossPanel is null) return;
        _board.Poll(_settings.BossAlertMinutes);
        _columnHeader!.IsVisible = !_bossPanelOpen;
        _rows.IsVisible = !_bossPanelOpen;
        _bossPanel.IsVisible = _bossPanelOpen;
        if (_bossPanelOpen) _bossPanel.Show(_board.Rows());
        var summary = _bossPanelOpen ? null : _board.Summary();
        _nextBoss.IsVisible = summary is not null;
        _nextBoss.Text = summary;
    }

    private void OnGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_settings.Locked || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        e.Pointer.Capture(_grip);
        _resizing = true;
        _resizeStartX = this.PointToScreen(e.GetPosition(this)).X;
        _resizeStartWidth = Width;
    }

    private void OnGripMoved(object? sender, PointerEventArgs e)
    {
        if (!_resizing) return;
        var screenX = this.PointToScreen(e.GetPosition(this)).X;
        Width = Math.Clamp(_resizeStartWidth + (screenX - _resizeStartX) / RenderScaling, 280, 2000);
    }

    private void EndResize(IPointer? pointer)
    {
        if (!_resizing) return;
        _resizing = false;
        pointer?.Capture(null);
        _settings.OverlayWidth = Width;
    }

    private void ShowShortcutHint()
    {
        var text = ShortcutHints.Footer(_shortcuts, _settings.Locked);
        _shortcutHint.Text = text;
        _shortcutHint.IsVisible = text is not null;
    }

    private void ApplyLock()
    {
        var locked = _settings.Locked;
        ShowShortcutHint();
        _grip.IsVisible = !locked;
        _bossPanel?.ShowActions(!locked);
        _toolbar.IsVisible = !locked;
        _close.IsVisible = !locked;
        if (locked) _historyPanel.IsVisible = false;
        _frame.Background = new SolidColorBrush(Color.FromArgb((byte)Math.Round((locked ? _settings.LockedOpacity : _settings.BackgroundOpacity) * 255), 0x0A, 0x0E, 0x16));
        _frame.BorderThickness = new Thickness(1);
        if (_desktop is not null && TryGetPlatformHandle() is { } handle) _desktop.SetClickThrough(handle.Handle, locked);
    }

    private void SetVisible(bool visible)
    {
        if (visible)
        {
            Show();
            Position = SavedPosition();
            ApplyLock();
        }
        else
        {
            SaveSettings();
            Hide();
        }
    }

    private void OpenBreakdown(uint? actorId)
    {
        if (actorId is not { } actor || _session.Breakdown(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) is not { } source) return;
        Breakdown?.Close();
        var breakdown = new BreakdownWindow(source, actor, _settings.BreakdownWidth, _settings.BreakdownHeight, _clock);
        if (Screens.ScreenFromWindow(this) is { } screen)
        {
            breakdown.WindowStartupLocation = WindowStartupLocation.Manual;
            breakdown.Position = BreakdownView.Placement(screen.WorkingArea, breakdown.Width, breakdown.Height, screen.Scaling);
        }
        breakdown.Closed += (_, _) => OnBreakdownClosed(breakdown);
        Breakdown = breakdown;
        breakdown.Show();
    }

    private void OnBreakdownClosed(BreakdownWindow breakdown)
    {
        _settings.BreakdownWidth = breakdown.Width;
        _settings.BreakdownHeight = breakdown.Height;
        if (ReferenceEquals(Breakdown, breakdown)) Breakdown = null;
        SaveSettings();
    }

    private EncounterSnapshot? CurrentSnapshot() => _session.Current(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    private string? SelfLine()
    {
        var snapshot = CurrentSnapshot();
        return snapshot?.Combatants.FirstOrDefault(c => c.IsSelf) is { } self ? MeterView.PlayerLine(snapshot, self.ActorId) : null;
    }

    private string? DetailLine()
    {
        var snapshot = CurrentSnapshot();
        return _session.Detail(snapshot) is { } detail ? MeterView.PlayerLine(snapshot, detail.ActorId) : null;
    }

    private async Task Copy(string? text, Button? feedback, string? label)
    {
        if (text is null || Clipboard is not { } clipboard) return;
        await clipboard.SetTextAsync(text);
        if (feedback is null) return;
        feedback.Content = CopiedLabel;
        DispatcherTimer.RunOnce(() => feedback.Content = label, TimeSpan.FromSeconds(1.5));
    }

    private void ToggleHistory()
    {
        _historyPanel.IsVisible = !_historyPanel.IsVisible;
        if (_historyPanel.IsVisible) ReloadHistory();
    }

    private void SetFilter(HistoryFilter filter)
    {
        _session.Filter = filter;
        _confirmation.Cancel();
        ReloadHistory();
        Refresh();
    }

    private void RequestDelete()
    {
        if (_session.SelectedFight is { } path) _confirmation.Request(path);
        Refresh();
    }

    private void ConfirmDelete()
    {
        if (_session.SelectedFight is { } path && _confirmation.Take(path)) _session.Delete();
        Refresh();
    }

    private void CancelDelete()
    {
        _confirmation.Cancel();
        Refresh();
    }

    private void ReloadHistory()
    {
        _historyDirty = false;
        var items = MeterView.History(_session.Filtered(200));
        _historyCount.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, UiText.Current.ShownOf, _session.Filtered().Count, _session.History.List().Count);
        _history.ItemsSource = items;
        _history.SelectedItem = items.FirstOrDefault(i => i.Path == _session.SelectedFight);
    }

    private static Control HistoryLine(HistoryItemView item)
    {
        var line = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto") };
        var resultBrush = item.Result switch { "✔" => Win, "✖" => Loss, _ => Muted };
        Place(line, new TextBlock { Text = item.When, Foreground = Muted, FontSize = 12, Margin = new Thickness(0, 0, 8, 0) }, 0, 0);
        Place(line, new TextBlock { Text = item.Result, Foreground = resultBrush, FontSize = 12, Margin = new Thickness(0, 0, 6, 0) }, 0, 1);
        Place(line, new TextBlock { Text = item.Title, Foreground = item.IsBoss ? Gold : Brushes.White, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis }, 0, 2);
        Place(line, new TextBlock { Text = item.Duration, Foreground = Muted, FontSize = 12, Margin = new Thickness(8, 0, 0, 0) }, 0, 3);
        var detail = string.Join(" · ", new[] { item.Zone, item.Players }.Where(t => t.Length > 0));
        Place(line, new TextBlock { Text = detail, Foreground = Muted, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis }, 1, 1);
        Grid.SetColumnSpan(line.Children[^1], 2);
        Place(line, new TextBlock { Text = (item.Dps + " " + item.Place).TrimEnd(), Foreground = Brushes.White, FontSize = 11, Margin = new Thickness(8, 0, 0, 0) }, 1, 3);
        return line;
    }

    private MenuFlyout LanguageMenu()
    {
        var menu = new MenuFlyout();
        foreach (var code in UiText.Languages)
        {
            var item = new MenuItem { Header = UiText.For(code).LanguageName, Tag = code };
            item.Click += (_, _) => ChangeLanguage(code);
            menu.Items.Add(item);
        }
        return menu;
    }

    private Button ToolButton(Func<UiText, string> text, Action onClick)
    {
        var button = new Button { FontSize = 12, Padding = new Thickness(8, 2) };
        Text(() => button.Content = text(UiText.Current));
        button.Click += (_, _) => onClick();
        return button;
    }

    private static void Place(Grid grid, Control child, int row, int column)
    {
        Grid.SetRow(child, row);
        Grid.SetColumn(child, column);
        grid.Children.Add(child);
    }
}
