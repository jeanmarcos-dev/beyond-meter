using System.Globalization;
using AionMeter.Core;
using AionMeter.Core.Combat;
using AionMeter.Core.Text;
using Avalonia.Controls;

namespace AionMeter.Overlay;

public sealed record ChatCopyItem(string Header, Func<EncounterSnapshot, string> Text);

public static class ChatMenu
{
    public static IReadOnlyList<ChatCopyItem> Items(UiText t, uint? player, string? playerName)
    {
        var items = new List<ChatCopyItem>();
        if (player is { } id && playerName is not null)
            items.Add(new(string.Format(CultureInfo.InvariantCulture, t.CopyPlayer, playerName), s => ChatLine.Player(Titled(t, s), id, t.Chat, c => Name(t, c))));
        items.Add(new(t.CopyParty, s => ChatLine.Party(Titled(t, s), t.Chat, c => Name(t, c))));
        items.Add(new(t.CopyTable, s => ChatLine.Table(Titled(t, s), t.Chat, c => Name(t, c))));
        return items;
    }

    public static string? PartyLine(EncounterSnapshot? snapshot) =>
        snapshot is not null && snapshot.Combatants.Any(c => !c.IsUnknownSummons) ? Items(UiText.Current, null, null)[0].Text(snapshot) : null;

    public static string? PlayerLine(EncounterSnapshot? snapshot, uint actorId) =>
        snapshot?.Combatants.FirstOrDefault(c => c.ActorId == actorId) is { } c ? Items(UiText.Current, actorId, c.Name)[0].Text(snapshot) : null;

    public static MenuFlyout Flyout(Func<EncounterSnapshot?> fight, Func<(uint? Id, string? Name)> player, Action<string> copy)
    {
        var menu = new MenuFlyout();
        menu.Opening += (_, _) =>
        {
            menu.Items.Clear();
            var (id, name) = player();
            foreach (var entry in Items(UiText.Current, id, name))
            {
                var item = new MenuItem { Header = entry.Header };
                item.Click += (_, _) =>
                {
                    if (fight() is { } snapshot) copy(entry.Text(snapshot));
                };
                menu.Items.Add(item);
            }
        };
        return menu;
    }

    private static EncounterSnapshot Titled(UiText t, EncounterSnapshot s) => s with { Title = t.FightTitle(s) };

    private static string Name(UiText t, CombatantSnapshot c) => t.CombatantLabel(c.ActorId, c.Name, c.Class);
}
