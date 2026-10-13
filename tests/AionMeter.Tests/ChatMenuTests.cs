using AionMeter.Core;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Settings;
using AionMeter.Core.Storage;
using AionMeter.Core.Text;
using AionMeter.Overlay;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace AionMeter.Tests;

public sealed class ChatMenuTests : IDisposable
{
    private const uint Me = 100, Mate = 101;

    private readonly string _dir = Directory.CreateTempSubdirectory("chat-menu").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void A_player_gets_their_line_the_party_ranking_and_the_discord_table()
    {
        var t = UiText.For("en");
        var fight = Fight();

        var items = ChatMenu.Items(t, Mate, "Brea");

        Assert.Equal([string.Format(t.CopyPlayer, "Brea"), t.CopyParty, t.CopyTable], items.Select(i => i.Header));
        Assert.Equal(ChatLine.Player(fight with { Title = t.FightTitle(fight) }, Mate, t.Chat), items[0].Text(fight));
        Assert.Equal(ChatLine.Party(fight with { Title = t.FightTitle(fight) }, t.Chat), items[1].Text(fight));
        Assert.Equal(ChatLine.Table(fight with { Title = t.FightTitle(fight) }, t.Chat), items[2].Text(fight));
    }

    [Fact]
    public void Without_a_player_only_the_party_entries_remain()
    {
        Assert.Equal(2, ChatMenu.Items(UiText.For("en"), null, null).Count);
    }

    [Fact]
    public void The_lines_use_the_words_of_the_interface_language()
    {
        var es = UiText.For("es");
        var fight = Fight();

        Assert.Equal(ChatLine.Table(fight with { Title = es.FightTitle(fight) }, es.Chat), ChatMenu.Items(es, null, null)[1].Text(fight));
        Assert.NotEqual(ChatMenu.Items(UiText.For("en"), null, null)[1].Text(fight), ChatMenu.Items(es, null, null)[1].Text(fight));
    }

    [AvaloniaFact]
    public async Task Right_clicking_a_row_copies_that_player_or_the_discord_table()
    {
        var (window, tracker) = Open();
        Play(tracker);
        window.Apply(OverlayCommand.Live);
        Dispatcher.UIThread.RunJobs();
        var row = window.GetLogicalDescendants().OfType<RowControl>().First(r => r.ActorId == Mate);
        var menu = (MenuFlyout)row.ContextFlyout!;

        var items = Open(menu, row);
        Assert.Equal(string.Format(UiText.Current.CopyPlayer, "Brea"), items[0].Header);

        items[2].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains("```", window.LastCopied);
        Assert.Contains("Brea", window.LastCopied);
        Assert.Equal(window.LastCopied, await window.Clipboard!.GetTextAsync());
    }

    [AvaloniaFact]
    public async Task The_breakdown_copies_the_player_it_shows()
    {
        var (window, tracker) = Open();
        Play(tracker);
        window.Apply(OverlayCommand.Breakdown);
        Dispatcher.UIThread.RunJobs();
        var breakdown = window.Breakdown!;
        var items = Open((MenuFlyout)breakdown.ChatButton.Flyout!, breakdown.ChatButton);

        Assert.Equal(string.Format(UiText.Current.CopyPlayer, "Ilvane"), items[0].Header);
        items[0].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains("Ilvane", breakdown.LastCopied);
        Assert.Contains(breakdown.GetLogicalDescendants().OfType<TextBlock>(), b => b.Name == "CopyNote" && b.IsVisible && b.Text == UiText.Current.Copied);
    }

    private static IReadOnlyList<MenuItem> Open(MenuFlyout menu, Control target)
    {
        menu.ShowAt(target);
        Dispatcher.UIThread.RunJobs();
        var items = menu.Items.OfType<MenuItem>().ToList();
        menu.Hide();
        return items;
    }

    private (OverlayWindow Window, CombatTracker Tracker) Open()
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, null, new HistoryPolicy());
        var window = new OverlayWindow(session, new UserSettings(), Path.Combine(_dir, "settings.json"), null);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, tracker);
    }

    private static void Play(CombatTracker tracker)
    {
        tracker.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        tracker.Process(new PlayerSeenEvent(0, Mate, "Brea", 1304, GameClass.Cleric));
        tracker.Process(new NpcSeenEvent(0, 500, 2300243, 120_000_000));
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        tracker.Process(new DamageEvent(now - 2_000, Me, 500, 16040000, 2_000, HitFlags.None));
        tracker.Process(new DamageEvent(now - 1_000, Mate, 500, 16040000, 1_000, HitFlags.None));
    }

    private static EncounterSnapshot Fight() =>
        new(Guid.NewGuid(), "Berk", "Altgard", DateTimeOffset.UnixEpoch, 30_000, 30_000, false, EncounterEndReason.Kill, 2, 2, null,
        [
            new CombatantSnapshot(Me, "Ilvane", GameClass.Elementalist, true, 2_000, 66, 2.0 / 3, 10, 0.1, 500, 0),
            new CombatantSnapshot(Mate, "Brea", GameClass.Cleric, false, 1_000, 33, 1.0 / 3, 5, 0, 300, 0),
        ]);
}
