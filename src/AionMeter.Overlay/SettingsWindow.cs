using System.Globalization;
using AionMeter.Core.Combat;
using AionMeter.Core.Settings;
using AionMeter.Core.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed record CaptureDevice(string? Name, string Label);

public sealed record SettingsContext(
    bool IsWindows,
    IReadOnlyList<string> NamesLanguages,
    IReadOnlyList<CaptureDevice> Devices,
    string HistoryInfo,
    string VersionText,
    bool CanAutoInstall,
    Action<string> Open,
    string DataFolder,
    string AppFolder,
    string LogsFolder,
    Func<Task<string>>? CheckNow = null);

public sealed class SettingsWindow : Window
{
    private static readonly int[] RowChoices = [5, 8, 10, 12, 15, 20];
    private static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(0xA8, 0xB3, 0xC7));
    private static readonly IBrush Gold = new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42));

    private readonly UserSettings _draft;
    private readonly SettingsContext _context;
    private readonly StackPanel _body = new() { Spacing = 6, Margin = new Thickness(16) };
    private readonly TextBlock _updateState = new() { Foreground = Muted, FontSize = 12, IsVisible = false };

    public SettingsWindow(UserSettings current, SettingsContext context)
    {
        _draft = UserSettings.Parse(current.ToJson());
        _context = context;
        var t = UiText.Current;
        Title = $"{AppInfo.Name} · {t.SettingsTitle}";
        Width = 560;
        Height = 700;
        MinWidth = 420;
        MinHeight = 360;
        Background = new SolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1C));
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        BuildLanguage(t);
        BuildOverlay(t);
        BuildMeter(t);
        BuildHistory(t);
        BuildCapture(t);
        BuildHotkeys(t);
        BuildUpdates(t);
        BuildData(t);

        SaveButton.Content = t.Save;
        CancelButton.Content = t.Cancel;
        SaveButton.Click += (_, _) => Save();
        CancelButton.Click += (_, _) => Close();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16, 8), Children = { CancelButton, SaveButton } };
        var layout = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        layout.Children.Add(buttons);
        layout.Children.Add(new ScrollViewer { Content = _body });
        Content = layout;
    }

    public event Action<UserSettings>? Saved;

    public ComboBox LanguageBox { get; } = Choices(nameof(LanguageBox));
    public ComboBox NamesBox { get; } = Choices(nameof(NamesBox));
    public CheckBox BarsRelative { get; } = Check(nameof(BarsRelative));
    public CheckBox ShowBoss { get; } = Check(nameof(ShowBoss));
    public CheckBox ShowGear { get; } = Check(nameof(ShowGear));
    public CheckBox AutoShow { get; } = Check(nameof(AutoShow));
    public TextBox AutoHide { get; } = Number(nameof(AutoHide));
    public CheckBox ShowOnStart { get; } = Check(nameof(ShowOnStart));
    public ComboBox MaxRows { get; } = Choices(nameof(MaxRows));
    public Slider BackgroundOpacity { get; } = Fraction(nameof(BackgroundOpacity));
    public Slider LockedOpacity { get; } = Fraction(nameof(LockedOpacity));
    public Slider RowSize { get; } = new() { Name = nameof(RowSize), Minimum = UserSettings.MinRowSize, Maximum = UserSettings.MaxRowSize, Width = 180 };
    public TextBox AlertMinutes { get; } = Number(nameof(AlertMinutes));
    public ComboBox Mode { get; } = Choices(nameof(Mode));
    public TextBox Idle { get; } = Number(nameof(Idle));
    public TextBox BossIdle { get; } = Number(nameof(BossIdle));
    public CheckBox SaveHistory { get; } = Check(nameof(SaveHistory));
    public ComboBox HistoryModeBox { get; } = Choices(nameof(HistoryModeBox));
    public TextBox HistoryMin { get; } = Number(nameof(HistoryMin));
    public ComboBox Device { get; } = Choices(nameof(Device));
    public CheckBox Record { get; } = Check(nameof(Record));
    public TextBox HotkeyToggle { get; } = Hotkey(nameof(HotkeyToggle));
    public TextBox HotkeyReset { get; } = Hotkey(nameof(HotkeyReset));
    public TextBox HotkeyClickThrough { get; } = Hotkey(nameof(HotkeyClickThrough));
    public TextBox HotkeyTimers { get; } = Hotkey(nameof(HotkeyTimers));
    public CheckBox CheckUpdates { get; } = Check(nameof(CheckUpdates));
    public CheckBox AutoInstall { get; } = Check(nameof(AutoInstall));
    public Button CheckNowButton { get; } = new() { Name = nameof(CheckNowButton) };
    public CheckBox Icons { get; } = Check(nameof(Icons));
    public Button SaveButton { get; } = new() { Name = nameof(SaveButton), IsDefault = true, Padding = new Thickness(16, 4) };
    public Button CancelButton { get; } = new() { Name = nameof(CancelButton), IsCancel = true, Padding = new Thickness(16, 4) };

    public void Save()
    {
        Saved?.Invoke(Collect());
        Close();
    }

    public UserSettings Collect()
    {
        var s = _draft;
        s.Language = UiText.Languages[Math.Max(0, LanguageBox.SelectedIndex)];
        s.NamesLanguage = NamesBox.SelectedIndex <= 0 ? null : _context.NamesLanguages[NamesBox.SelectedIndex - 1];
        s.BarsRelativeToTop = BarsRelative.IsChecked == true;
        s.ShowBossPanel = ShowBoss.IsChecked == true;
        s.ShowGear = ShowGear.IsChecked == true;
        s.AutoShow = AutoShow.IsChecked == true;
        s.AutoHideSeconds = Read(AutoHide, s.AutoHideSeconds, 0, 600);
        s.ShowOnStart = ShowOnStart.IsChecked == true;
        s.MaxRows = RowChoices[Math.Max(0, MaxRows.SelectedIndex)];
        s.BackgroundOpacity = Math.Round(BackgroundOpacity.Value, 2);
        s.LockedOpacity = Math.Round(LockedOpacity.Value, 2);
        s.RowSize = (int)Math.Round(RowSize.Value);
        s.BossAlertMinutes = Read(AlertMinutes, s.BossAlertMinutes, 0, 30);
        s.TargetMode = Mode.SelectedIndex == 1 ? TargetMode.All : TargetMode.BossOnly;
        s.IdleTimeoutSec = Read(Idle, s.IdleTimeoutSec, 3, 120);
        s.BossIdleTimeoutSec = Read(BossIdle, s.BossIdleTimeoutSec, 5, 300);
        s.SaveHistory = SaveHistory.IsChecked == true;
        s.HistoryMode = (HistoryMode)Math.Max(0, HistoryModeBox.SelectedIndex);
        s.HistoryMinSeconds = Read(HistoryMin, s.HistoryMinSeconds, 5, 600);
        if (_context.IsWindows) s.CaptureDevice = _context.Devices.ElementAtOrDefault(Math.Max(0, Device.SelectedIndex))?.Name;
        s.RecordPackets = Record.IsChecked == true;
        s.HotkeyToggleOverlay = HotkeyToggle.Text?.Trim() ?? "";
        s.HotkeyReset = HotkeyReset.Text?.Trim() ?? "";
        s.HotkeyClickThrough = HotkeyClickThrough.Text?.Trim() ?? "";
        s.HotkeyTimers = HotkeyTimers.Text?.Trim() ?? "";
        s.CheckUpdates = CheckUpdates.IsChecked == true;
        s.AutoInstallUpdates = AutoInstall.IsChecked == true;
        s.DownloadIcons = Icons.IsChecked == true;
        return s;
    }

    private void BuildLanguage(UiText t)
    {
        foreach (var code in UiText.Languages) LanguageBox.Items.Add(UiText.For(code).LanguageName);
        LanguageBox.SelectedIndex = Array.IndexOf(UiText.Languages, UiText.Normalize(_draft.Language));
        NamesBox.Items.Add(t.OvNamesSame);
        foreach (var code in _context.NamesLanguages) NamesBox.Items.Add(LanguageLabel(code));
        NamesBox.SelectedIndex = _draft.NamesLanguage is { } names ? _context.NamesLanguages.ToList().IndexOf(names) + 1 : 0;
        Section(t.SecLanguage);
        Row(t.LanguageMenu, LanguageBox);
        Row(t.OvNamesLanguage, NamesBox);
        Hint(t.LanguageHint);
    }

    private void BuildOverlay(UiText t)
    {
        Section(t.SecOverlay);
        Toggle(BarsRelative, t.BarsRelative, _draft.BarsRelativeToTop);
        Hint(t.BarsRelativeHint);
        Toggle(ShowBoss, t.ShowBossPanel, _draft.ShowBossPanel);
        Toggle(ShowGear, t.ShowGear, _draft.ShowGear);
        Toggle(AutoShow, t.AutoShow, _draft.AutoShow);
        Row(t.AutoHide, Fill(AutoHide, _draft.AutoHideSeconds));
        Toggle(ShowOnStart, t.ShowOnStart, _draft.ShowOnStart);
        foreach (var rows in RowChoices) MaxRows.Items.Add(rows.ToString(CultureInfo.InvariantCulture));
        MaxRows.SelectedIndex = Array.IndexOf(RowChoices, RowChoices.OrderBy(n => Math.Abs(n - _draft.MaxRows)).First());
        Row(t.RowsShown, MaxRows);
        BackgroundOpacity.Value = _draft.BackgroundOpacity;
        Row(t.BackgroundOpacity, BackgroundOpacity);
        LockedOpacity.Value = _draft.LockedOpacity;
        Row(t.OvLockedOpacity, LockedOpacity);
        RowSize.Value = _draft.RowSize;
        Row(t.RowSize, RowSize);
        Row(t.OvAlertMinutes, Fill(AlertMinutes, _draft.BossAlertMinutes));
    }

    private void BuildMeter(UiText t)
    {
        Section(t.SecMeter);
        Mode.Items.Add(t.MeasureBoss);
        Mode.Items.Add(t.MeasureAll);
        Mode.SelectedIndex = _draft.TargetMode == TargetMode.All ? 1 : 0;
        Row(t.Measure, Mode);
        Row(t.IdleEnd, Fill(Idle, _draft.IdleTimeoutSec));
        Row(t.IdleBoss, Fill(BossIdle, _draft.BossIdleTimeoutSec));
    }

    private void BuildHistory(UiText t)
    {
        Section(t.SecHistory);
        Toggle(SaveHistory, t.SaveAuto, _draft.SaveHistory);
        HistoryModeBox.Items.Add(t.HistBosses);
        HistoryModeBox.Items.Add(t.HistBossesLong);
        HistoryModeBox.Items.Add(t.HistAll);
        HistoryModeBox.SelectedIndex = (int)_draft.HistoryMode;
        Row(t.WhichFights, HistoryModeBox);
        Row(t.LongerThan, Fill(HistoryMin, _draft.HistoryMinSeconds));
        Hint(_context.HistoryInfo);
    }

    private void BuildCapture(UiText t)
    {
        Section(t.SecCapture);
        if (_context.IsWindows)
        {
            foreach (var device in _context.Devices) Device.Items.Add(device.Label);
            Device.SelectedIndex = Math.Max(0, _context.Devices.ToList().FindIndex(d => d.Name == _draft.CaptureDevice));
            Row(t.NetworkAdapter, Device);
            Hint(t.AdapterHint);
        }
        Toggle(Record, t.RecordPackets, _draft.RecordPackets);
        Hint(t.RecordHint);
    }

    private void BuildHotkeys(UiText t)
    {
        Section(t.SecHotkeys);
        HotkeyToggle.Text = _draft.HotkeyToggleOverlay;
        HotkeyReset.Text = _draft.HotkeyReset;
        HotkeyClickThrough.Text = _draft.HotkeyClickThrough;
        HotkeyTimers.Text = _draft.HotkeyTimers;
        if (!_context.IsWindows)
        {
            Hint(t.OvHotkeysDesktop);
            return;
        }
        Row(t.HkToggle, HotkeyToggle);
        Row(t.HkReset, HotkeyReset);
        Row(t.HkClick, HotkeyClickThrough);
        Row(t.HkTimers, HotkeyTimers);
        Hint(t.HotkeysHint);
    }

    private void BuildUpdates(UiText t)
    {
        Section(t.SecUpdates);
        Toggle(CheckUpdates, t.CheckUpdatesAuto, _draft.CheckUpdates);
        Hint(t.UpdatesHint);
        if (_context.IsWindows)
        {
            Toggle(AutoInstall, t.AutoInstallUpdates, _draft.AutoInstallUpdates);
            AutoInstall.IsEnabled = _context.CanAutoInstall;
            Hint(t.AutoInstallHint);
        }
        else
        {
            AutoInstall.IsChecked = _draft.AutoInstallUpdates;
        }
        CheckNowButton.Content = t.CheckNow;
        CheckNowButton.IsVisible = _context.CheckNow is not null;
        CheckNowButton.Click += async (_, _) =>
        {
            CheckNowButton.IsEnabled = false;
            _updateState.IsVisible = true;
            _updateState.Text = t.UpdateChecking;
            _updateState.Text = await _context.CheckNow!();
            CheckNowButton.IsEnabled = true;
        };
        _body.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { new TextBlock { Text = _context.VersionText, Foreground = Muted, FontSize = 12, VerticalAlignment = VerticalAlignment.Center }, CheckNowButton },
        });
        _body.Children.Add(_updateState);
    }

    private void BuildData(UiText t)
    {
        Section(t.SecData);
        Toggle(Icons, t.DownloadIcons, _draft.DownloadIcons);
        Hint(t.IconsHint);
        _body.Children.Add(new WrapPanel
        {
            ItemSpacing = 6,
            LineSpacing = 6,
            Children =
            {
                Link(t.OpenDataFolder, _context.DataFolder),
                Link(t.OpenAppFolder, _context.AppFolder),
                Link(t.Logs, _context.LogsFolder),
            },
        });
        Hint(t.Credit);
        _body.Children.Add(Link(AppInfo.UpstreamUrl, AppInfo.UpstreamUrl));
    }

    private Button Link(string text, string target)
    {
        var button = new Button { Content = text, FontSize = 12 };
        button.Click += (_, _) => _context.Open(target);
        return button;
    }

    private void Section(string title) =>
        _body.Children.Add(new TextBlock { Text = title, Foreground = Gold, FontSize = 13, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 12, 0, 2) });

    private void Hint(string text) =>
        _body.Children.Add(new TextBlock { Text = text, Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap });

    private void Toggle(CheckBox box, string text, bool value)
    {
        box.Content = text;
        box.IsChecked = value;
        _body.Children.Add(box);
    }

    private void Row(string label, Control control)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 12, 0) });
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        _body.Children.Add(grid);
    }

    private static TextBox Fill(TextBox box, int value)
    {
        box.Text = value.ToString(CultureInfo.InvariantCulture);
        return box;
    }

    private static int Read(TextBox box, int fallback, int min, int max) =>
        int.TryParse(box.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? Math.Clamp(value, min, max) : fallback;

    private static string LanguageLabel(string code)
    {
        var known = UiText.Languages.Contains(code) ? UiText.For(code).LanguageName : null;
        if (known is not null) return known;
        try
        {
            return CultureInfo.GetCultureInfo(code).NativeName;
        }
        catch (CultureNotFoundException)
        {
            return code;
        }
    }

    private static ComboBox Choices(string name) => new() { Name = name, MinWidth = 180 };

    private static CheckBox Check(string name) => new() { Name = name };

    private static TextBox Number(string name) => new() { Name = name, Width = 80 };

    private static TextBox Hotkey(string name) => new() { Name = name, Width = 180 };

    private static Slider Fraction(string name) => new() { Name = name, Minimum = 0.15, Maximum = 1, Width = 180 };
}
