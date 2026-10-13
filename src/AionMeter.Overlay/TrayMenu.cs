using AionMeter.Core.Text;
using Avalonia.Controls;
using Avalonia.Platform;

namespace AionMeter.Overlay;

public sealed record TrayAction(string Header, Action Run, bool? Checked = null);

public static class TrayMenu
{
    public static IReadOnlyList<TrayAction> Actions(UiText text, bool visible, bool locked, Action<OverlayCommand> apply, IBossAlert alert) =>
    [
        new(visible ? text.TrayHide : text.TrayShow, () => apply(OverlayCommand.Toggle)),
        new(text.TrayClickThrough, () => apply(OverlayCommand.Lock), locked),
        new(text.TrayTimers, () => apply(OverlayCommand.Timers)),
        new(text.TrayTestAlert, () => alert.Notify(AppInfo.Name, text.TrayTestAlert)),
        new(text.TrayExit, () => apply(OverlayCommand.Quit)),
    ];

    public static TrayIcon Create(OverlayWindow window, IBossAlert alert)
    {
        var menu = new NativeMenu();
        void Fill()
        {
            menu.Items.Clear();
            foreach (var action in Actions(UiText.Current, window.IsVisible, window.Locked, window.Apply, alert))
            {
                var item = new NativeMenuItem(action.Header);
                if (action.Checked is { } isChecked)
                {
                    item.ToggleType = NativeMenuItemToggleType.CheckBox;
                    item.IsChecked = isChecked;
                }
                item.Click += (_, _) =>
                {
                    action.Run();
                    Fill();
                };
                menu.Items.Add(item);
            }
        }
        Fill();
        menu.Opening += (_, _) => Fill();
        var tray = new TrayIcon { Icon = Icon(), ToolTipText = AppInfo.Name, Menu = menu, IsVisible = true };
        tray.Clicked += (_, _) =>
        {
            window.Apply(OverlayCommand.Toggle);
            Fill();
        };
        return tray;
    }

    private static WindowIcon Icon()
    {
        using var stream = AssetLoader.Open(new Uri($"avares://{typeof(TrayMenu).Assembly.GetName().Name}/Assets/app.ico"));
        return new WindowIcon(stream);
    }
}
