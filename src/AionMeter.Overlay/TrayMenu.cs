using AionMeter.Core.Text;
using Avalonia.Controls;
using Avalonia.Platform;

namespace AionMeter.Overlay;

public sealed record TrayAction(string Header, Action Run, bool? Checked = null, IReadOnlyList<TrayAction>? Items = null);

public sealed record TrayState(bool Visible, bool Locked, string Language, string? Update = null);

public sealed record TrayHandlers(Action<OverlayCommand> Apply, Action<string> ChangeLanguage, Action PickReplay, IBossAlert Alert, Action? ShowUpdate = null, Action? CheckUpdates = null);

public static class TrayMenu
{
    public static IReadOnlyList<TrayAction> Actions(UiText text, TrayState state, TrayHandlers on)
    {
        var updates = new List<TrayAction>();
        if (state.Update is { } update && on.ShowUpdate is { } show) updates.Add(new(update, show));
        if (on.CheckUpdates is { } check) updates.Add(new(text.TrayCheckUpdates, check));
        return [.. Entries(text, state, on), .. updates, new(text.TrayExit, () => on.Apply(OverlayCommand.Quit))];
    }

    private static IReadOnlyList<TrayAction> Entries(UiText text, TrayState state, TrayHandlers on) =>
    [
        new(state.Visible ? text.TrayHide : text.TrayShow, () => on.Apply(OverlayCommand.Toggle)),
        new(text.TrayClickThrough, () => on.Apply(OverlayCommand.Lock), state.Locked),
        new(text.HkReset, () => on.Apply(OverlayCommand.Reset)),
        new(text.TrayHistory, () => on.Apply(OverlayCommand.History)),
        new(text.TrayTimers, () =>
        {
            on.Apply(OverlayCommand.Show);
            on.Apply(OverlayCommand.Timers);
        }),
        new(text.TraySettings, () => on.Apply(OverlayCommand.Settings)),
        new(text.LanguageMenu, () => { }, Items: UiText.Languages
            .Select(code => new TrayAction(UiText.For(code).LanguageName, () => on.ChangeLanguage(code), code == state.Language))
            .ToList()),
        new(text.TrayDemo, () => on.Apply(OverlayCommand.Demo)),
        new(text.TrayReplay, on.PickReplay),
        new(text.TrayTestAlert, () => on.Alert.Notify(AppInfo.Name, text.TrayTestAlert)),
    ];

    public static TrayIcon Create(OverlayWindow window, IBossAlert alert, UpdateCoordinator? updates = null)
    {
        var menu = new NativeMenu();
        var handlers = new TrayHandlers(window.Apply, window.ChangeLanguage, () => _ = window.PickReplay(), alert,
            updates is null ? null : () => window.ShowUpdate(updates.Service.Available),
            updates is null ? null : () => _ = updates.CheckFromTrayAsync());
        void Fill()
        {
            var state = new TrayState(window.IsVisible, window.Locked, UiText.Current.Code, updates?.TrayEntry());
            Populate(menu, Actions(UiText.Current, state, handlers), Fill);
        }
        Fill();
        menu.Opening += (_, _) => Fill();
        window.LanguageChanged += Fill;
        if (updates is not null) updates.Changed += () => Avalonia.Threading.Dispatcher.UIThread.Post(Fill);
        var tray = new TrayIcon { Icon = Icon(), ToolTipText = AppInfo.Name, Menu = menu, IsVisible = true };
        tray.Clicked += (_, _) =>
        {
            window.Apply(OverlayCommand.Toggle);
            Fill();
        };
        return tray;
    }

    private static void Populate(NativeMenu menu, IReadOnlyList<TrayAction> actions, Action refresh)
    {
        menu.Items.Clear();
        foreach (var action in actions)
        {
            var item = new NativeMenuItem(action.Header);
            if (action.Checked is { } isChecked)
            {
                item.ToggleType = NativeMenuItemToggleType.CheckBox;
                item.IsChecked = isChecked;
            }
            if (action.Items is { } children)
            {
                var submenu = new NativeMenu();
                Populate(submenu, children, refresh);
                item.Menu = submenu;
            }
            else
            {
                item.Click += (_, _) =>
                {
                    action.Run();
                    refresh();
                };
            }
            menu.Items.Add(item);
        }
    }

    private static WindowIcon Icon()
    {
        using var stream = AssetLoader.Open(new Uri($"avares://{typeof(TrayMenu).Assembly.GetName().Name}/Assets/app.ico"));
        return new WindowIcon(stream);
    }
}
