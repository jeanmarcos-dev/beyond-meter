using AionMeter.Core.Text;
using Avalonia.Controls;
using Avalonia.Platform;

namespace AionMeter.Overlay;

public sealed record TrayAction(string Header, Action Run, bool? Checked = null, IReadOnlyList<TrayAction>? Items = null);

public sealed record TrayState(bool Visible, bool Locked, string Language);

public sealed record TrayHandlers(Action<OverlayCommand> Apply, Action<string> ChangeLanguage, Action PickReplay, IBossAlert Alert);

public static class TrayMenu
{
    public static IReadOnlyList<TrayAction> Actions(UiText text, TrayState state, TrayHandlers on) =>
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
        new(text.TrayExit, () => on.Apply(OverlayCommand.Quit)),
    ];

    public static TrayIcon Create(OverlayWindow window, IBossAlert alert)
    {
        var menu = new NativeMenu();
        var handlers = new TrayHandlers(window.Apply, window.ChangeLanguage, () => _ = window.PickReplay(), alert);
        void Fill()
        {
            var state = new TrayState(window.IsVisible, window.Locked, UiText.Current.Code);
            Populate(menu, Actions(UiText.Current, state, handlers), Fill);
        }
        Fill();
        menu.Opening += (_, _) => Fill();
        window.LanguageChanged += Fill;
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
