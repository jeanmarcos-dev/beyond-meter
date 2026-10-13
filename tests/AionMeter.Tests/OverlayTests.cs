using AionMeter.Core.Settings;
using AionMeter.Core;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Storage;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class OverlayTests : IDisposable
{
    private const uint Me = 100, Mate = 101, Boss = 500;

    private readonly string _dir = Directory.CreateTempSubdirectory("overlay").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Theory]
    [InlineData("lock", OverlayCommand.Lock)]
    [InlineData(" RESET\n", OverlayCommand.Reset)]
    [InlineData("older", OverlayCommand.Older)]
    [InlineData("toggle", OverlayCommand.Toggle)]
    [InlineData("skills", OverlayCommand.Skills)]
    [InlineData("timers", OverlayCommand.Timers)]
    [InlineData("breakdown", OverlayCommand.Breakdown)]
    [InlineData("copy", OverlayCommand.Copy)]
    [InlineData("copyself", OverlayCommand.CopySelf)]
    [InlineData("mode", OverlayCommand.Mode)]
    [InlineData("heal", OverlayCommand.Heal)]
    [InlineData("reload", OverlayCommand.Reload)]
    [InlineData("record", OverlayCommand.Record)]
    public void Parse_reads_command_words_ignoring_case_and_whitespace(string text, OverlayCommand expected)
    {
        Assert.Equal(expected, OverlayCommands.Parse(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("3")]
    [InlineData("explode")]
    [InlineData("lock;reset")]
    public void Parse_rejects_anything_else(string? text)
    {
        Assert.Null(OverlayCommands.Parse(text));
    }

    [Fact]
    public void Usage_names_every_command()
    {
        var usage = OverlayCommands.Usage();

        Assert.All(Enum.GetValues<OverlayCommand>(), c => Assert.Contains(OverlayCommands.Name(c), usage));
    }

    [Fact]
    public void Usage_mentions_the_startup_flags()
    {
        var usage = OverlayCommands.Usage();

        Assert.Contains("--demo", usage);
        Assert.Contains("--hidden", usage);
        Assert.Contains("--replay", usage);
        Assert.Contains("--fast", usage);
    }

    [Fact]
    public void Header_marks_a_replay_and_a_recording()
    {
        var capturing = new CaptureStatus(CaptureState.Capturing, "Replaying x.pcap");
        var stopped = new CaptureStatus(CaptureState.Stopped, "Replay finished");

        var replay = MeterView.Header(null, capturing, -1, 0, replay: true);
        Assert.Equal("Replay", replay.Source);
        Assert.False(replay.Recording);
        Assert.Equal("Replay finished", MeterView.Header(null, stopped, -1, 0, replay: true).Source);
        Assert.Equal("Live", MeterView.Header(null, capturing, -1, 0, recording: true).Source);
        Assert.True(MeterView.Header(null, capturing, -1, 0, recording: true).Recording);
    }

    [Fact]
    public void Header_says_a_demo_fight_is_running_while_live()
    {
        var capturing = new CaptureStatus(CaptureState.Capturing, "");

        Assert.Equal("Demo fight running", MeterView.Header(null, capturing, -1, 0, demo: true).Source);
        Assert.Equal("History 1/3", MeterView.Header(null, capturing, 0, 3, demo: true).Source);
    }

    [Fact]
    public void Only_show_and_toggle_start_a_new_instance()
    {
        Assert.True(OverlayCommands.StartsInstance(OverlayCommand.Show));
        Assert.True(OverlayCommands.StartsInstance(OverlayCommand.Toggle));
        Assert.False(OverlayCommands.StartsInstance(OverlayCommand.Reset));
        Assert.False(OverlayCommands.StartsInstance(OverlayCommand.Quit));
    }

    [Fact]
    public async Task Channel_delivers_a_command_sent_by_another_process()
    {
        var path = Path.Combine(_dir, "o.sock");
        var received = new TaskCompletionSource<OverlayCommand>();
        using var channel = CommandChannel.Listen(path, c => received.TrySetResult(c));

        Assert.True(CommandChannel.TrySend(path, OverlayCommand.Reset));
        Assert.Equal(OverlayCommand.Reset, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Channel_creates_its_missing_folder()
    {
        var path = Path.Combine(_dir, "bm", "o.sock");
        var received = new TaskCompletionSource<OverlayCommand>();

        using var channel = CommandChannel.Listen(path, c => received.TrySetResult(c));

        Assert.True(CommandChannel.TrySend(path, OverlayCommand.Show));
        Assert.Equal(OverlayCommand.Show, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Channel_reports_no_instance_for_missing_or_stale_sockets()
    {
        var path = Path.Combine(_dir, "o.sock");
        Assert.False(CommandChannel.TrySend(path, OverlayCommand.Lock));

        CommandChannel.Listen(path, _ => { }).Dispose();
        File.WriteAllText(path, "");
        Assert.False(CommandChannel.TrySend(path, OverlayCommand.Lock));
    }

    [Fact]
    public async Task Channel_replaces_a_stale_socket_file_left_by_a_crash()
    {
        var path = Path.Combine(_dir, "o.sock");
        File.WriteAllText(path, "");
        var received = new TaskCompletionSource<OverlayCommand>();

        using var channel = CommandChannel.Listen(path, c => received.TrySetResult(c));

        Assert.True(CommandChannel.TrySend(path, OverlayCommand.Quit));
        Assert.Equal(OverlayCommand.Quit, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Theory]
    [InlineData(950, "950")]
    [InlineData(12_345, "12.3K")]
    [InlineData(1_234_567, "1.23M")]
    [InlineData(2_500_000_000, "2.50B")]
    public void Compact_shortens_large_numbers(double value, string expected)
    {
        Assert.Equal(expected, MeterView.Compact(value));
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(65_400, "1:05")]
    [InlineData(3_725_000, "1:02:05")]
    [InlineData(-10, "0:00")]
    public void Duration_formats_minutes_and_hours(long ms, string expected)
    {
        Assert.Equal(expected, MeterView.Duration(ms));
    }

    [Fact]
    public void Rows_rank_by_damage_scale_bars_to_the_top_dealer_and_respect_the_limit()
    {
        var snapshot = Snapshot(
            Combatant(1, "Low", 250, self: true),
            Combatant(2, "Top", 1_000),
            Combatant(3, "Mid", 500),
            Combatant(AionMeter.Core.Combat.Combatant.UnknownSummonsId, "", 100));

        var rows = MeterView.Rows(snapshot, max: 3);

        Assert.Equal(["Top", "Mid", "Low"], rows.Select(r => r.Name));
        Assert.Equal([1, 2, 3], rows.Select(r => r.Place));
        Assert.Equal([1.0, 0.5, 0.25], rows.Select(r => r.Bar));
        Assert.True(rows[2].IsSelf);
        Assert.Equal("Summons", MeterView.Rows(snapshot, max: 4)[3].Name);
    }

    [Fact]
    public void Healing_rows_rank_by_healing_hide_non_healers_and_share_the_group_total()
    {
        var snapshot = Snapshot(
            Combatant(1, "Fighter", 5_000),
            Healer(2, "Cleric", 600),
            Healer(3, "Chanter", 200, self: true),
            Combatant(4, "Mage", 3_000));

        var rows = MeterView.Rows(snapshot, max: 5, ViewMode.Healing);

        Assert.Equal(["Cleric", "Chanter"], rows.Select(r => r.Name));
        Assert.Equal([1, 2], rows.Select(r => r.Place));
        Assert.Equal([1.0, 1.0 / 3], rows.Select(r => r.Bar));
        Assert.Equal(["75%", "25%"], rows.Select(r => r.Share));
        Assert.Equal(["600", "200"], rows.Select(r => r.Damage));
        Assert.Equal(["60", "20"], rows.Select(r => r.Dps));
        Assert.True(rows[1].IsSelf);
    }

    [Fact]
    public void Rows_show_the_level_only_when_it_is_known()
    {
        var snapshot = Snapshot(
            Combatant(1, "Ilvane", 1_000, self: true) with { Level = 45 },
            Combatant(2, "Borgrim", 500));

        var rows = MeterView.Rows(snapshot, max: 5);

        Assert.Equal(["Lv 45", ""], rows.Select(r => r.Level));
        Assert.Equal(["Lv 45", ""], MeterView.Rows(snapshot with { Combatants = [.. snapshot.Combatants.Select(c => c with { Healing = 10 })] }, max: 5, ViewMode.Healing).Select(r => r.Level));
    }

    [Fact]
    public void Detail_view_carries_the_level()
    {
        var quality = new HitQuality(0, 0, null, null, null, null, null, null, 0, null);

        Assert.Equal("Lv 45", MeterView.Detail(Detail(quality) with { Level = 45 }, maxSkills: 5).Level);
        Assert.Equal("", MeterView.Detail(Detail(quality), maxSkills: 5).Level);
    }

    [Fact]
    public void Healing_rows_are_empty_when_nobody_healed()
    {
        var snapshot = Snapshot(Combatant(1, "Fighter", 5_000));

        Assert.Empty(MeterView.Rows(snapshot, max: 5, ViewMode.Healing));
        Assert.Single(MeterView.Rows(snapshot, max: 5, ViewMode.Damage));
    }

    [Fact]
    public void Damage_rows_still_list_a_pure_healer_last_with_zero()
    {
        var snapshot = Snapshot(Healer(2, "Cleric", 600), Combatant(1, "Fighter", 5_000));

        var rows = MeterView.Rows(snapshot, max: 5);

        Assert.Equal(["Fighter", "Cleric"], rows.Select(r => r.Name));
        Assert.Equal("0", rows[1].Damage);
    }

    [Fact]
    public void Healing_header_shows_party_healing_and_columns_follow_the_view()
    {
        var snapshot = Snapshot(Combatant(1, "Fighter", 5_000), Healer(2, "Cleric", 600), Healer(3, "Chanter", 200));
        var status = new CaptureStatus(CaptureState.Capturing, "");

        Assert.Equal("Total 5000 · Party 100/s", MeterView.Header(snapshot, status, -1, 0).Totals);
        Assert.Equal("Healing 800 · Party 80/s", MeterView.Header(snapshot, status, -1, 0, ViewMode.Healing).Totals);
        Assert.Equal(["Player", "DPS", "Damage", "%", "Crit"], MeterView.Columns(ViewMode.Damage));
        Assert.Equal(["Player", "HPS", "Healing", "%", ""], MeterView.Columns(ViewMode.Healing));
    }

    [Fact]
    public void PartyLine_stays_damage_based_and_leaves_out_pure_healers()
    {
        var snapshot = Snapshot(Combatant(1, "Fighter", 5_000), Healer(2, "Cleric", 600));

        Assert.DoesNotContain("Cleric", MeterView.PartyLine(snapshot));
        Assert.Contains("Fighter", MeterView.PartyLine(snapshot));
    }

    [Fact]
    public void PartyLine_fits_the_game_chat_dropping_whole_players()
    {
        var players = Enumerable.Range(1, 8)
            .Select(i => Combatant((uint)i, $"Extraordinarilylongname{i}", 9_000_000 - i * 100_000))
            .ToArray();

        var line = MeterView.PartyLine(Snapshot(players))!;

        Assert.True(line.Length <= ChatLine.MaxLength, $"{line.Length}: {line}");
        Assert.Matches(@"\| \+\d+ more \| party [\d.]+[KM]?/s$", line);
        Assert.DoesNotContain("Extraordinarilylongname8", line);
    }

    [Fact]
    public void PartyLine_marks_a_kill_and_skips_unowned_summons()
    {
        var snapshot = Snapshot(Combatant(2, "Top", 1_000), Combatant(AionMeter.Core.Combat.Combatant.UnknownSummonsId, "", 900)) with
        {
            Title = "Berk",
            CombatMs = 65_000,
            Reason = EncounterEndReason.Kill,
        };

        Assert.Equal("Berk 1:05 KILL | 1.Top 100/s 25% | party 100/s", MeterView.PartyLine(snapshot));
    }

    [Fact]
    public void PlayerLine_names_rank_dps_share_and_party_dps()
    {
        var snapshot = Snapshot(Combatant(2, "Top", 1_000), Combatant(3, "Mid", 500)) with { Title = "Berk", CombatMs = 65_000 };

        var line = MeterView.PlayerLine(snapshot, 3)!;

        Assert.StartsWith("Berk 1:05 | Mid #2 of 2: 50/s, 500 dmg (25%)", line);
        Assert.EndsWith(" | party 100/s", line);
    }

    [Fact]
    public void Lines_are_null_without_a_fight()
    {
        Assert.Null(MeterView.PartyLine(null));
        Assert.Null(MeterView.PartyLine(Snapshot()));
        Assert.Null(MeterView.PartyLine(Snapshot(Combatant(AionMeter.Core.Combat.Combatant.UnknownSummonsId, "", 900))));
        Assert.Null(MeterView.PlayerLine(null, 1));
        Assert.Null(MeterView.PlayerLine(Snapshot(Combatant(2, "Top", 1_000)), 99));
    }

    [Fact]
    public void Rows_are_empty_without_a_fight()
    {
        Assert.Empty(MeterView.Rows(null, 12));
        Assert.Empty(MeterView.Rows(Snapshot(), 12));
    }

    [Fact]
    public void Header_shows_boss_hp_while_live_and_the_history_position_when_browsing()
    {
        var capturing = new CaptureStatus(CaptureState.Capturing, "Capturing · 1.2.3.4");
        var live = Snapshot(Combatant(1, "A", 1_000)) with
        {
            IsActive = true,
            CombatMs = 65_000,
            Boss = new BossSnapshot(Boss, 2300243, "Berk", 250, 1_000),
        };

        var header = MeterView.Header(live, capturing, historyIndex: -1, historyCount: 4);
        Assert.Equal("Berk · 25%", header.Title);
        Assert.Equal("1:05", header.Time);
        Assert.Equal("Live", header.Source);
        Assert.True(header.IsLive);

        var browsing = MeterView.Header(live with { IsActive = false }, capturing, historyIndex: 1, historyCount: 4);
        Assert.EndsWith(" Berk", browsing.Title);
        Assert.Equal("History 2/4", browsing.Source);
        Assert.False(browsing.IsLive);

        Assert.Equal("Waiting for combat", MeterView.Header(null, capturing, -1, 0).Title);
    }

    [Theory]
    [InlineData(CaptureState.Capturing, "x", "Live")]
    [InlineData(CaptureState.WaitingForGame, "AION 2 is not running", "Game closed")]
    [InlineData(CaptureState.WaitingForGame, "Listening on any…", "Waiting for traffic")]
    [InlineData(CaptureState.Error, "boom", "Error: boom")]
    [InlineData(CaptureState.Stopped, "Capture stopped", "No capture")]
    public void StatusText_summarises_the_capture_state(CaptureState state, string message, string expected)
    {
        Assert.Equal(expected, MeterView.StatusText(new CaptureStatus(state, message)));
    }

    [Fact]
    public void Session_saves_long_fights_and_skips_short_ones()
    {
        var session = Session(out var tracker);
        Setup(tracker);

        Fight(tracker, start: 1_000, seconds: 2);
        Fight(tracker, start: 10_000_000, seconds: 10);

        var saved = Assert.Single(session.History.List());
        Assert.Equal(10_000, saved.CombatMs);
    }

    [Fact]
    public void Session_browses_history_from_newest_to_oldest_and_back_to_live()
    {
        var session = Session(out var tracker);
        Setup(tracker);
        Fight(tracker, start: 1_000, seconds: 10);
        Fight(tracker, start: 10_000_000, seconds: 20);
        var list = session.History.List();

        session.Older();
        Assert.Equal(list[0].Path, session.SelectedFight);
        Assert.Equal(20_000, session.Current(0)!.CombatMs);

        session.Older();
        session.Older();
        Assert.Equal(list[1].Path, session.SelectedFight);
        Assert.Equal(1, session.SelectedIndex());

        session.Newer();
        session.Newer();
        Assert.True(session.IsLive);
        Assert.Equal(-1, session.SelectedIndex());
    }

    [Fact]
    public void Session_reset_closes_the_running_fight_saves_it_and_returns_to_live()
    {
        var session = Session(out var tracker);
        Setup(tracker);
        tracker.Process(new DamageEvent(1_000, Me, Boss, 16040000, 1_000, HitFlags.None));
        tracker.Process(new DamageEvent(9_000, Me, Boss, 16040000, 1_000, HitFlags.None));
        session.Show("elsewhere.json");

        session.Reset();

        Assert.True(session.IsLive);
        Assert.Null(session.Current(10_000));
        Assert.Equal(EncounterEndReason.Manual, Assert.Single(session.History.List()).Reason);
    }

    [Fact]
    public void Session_shows_nothing_for_a_saved_fight_that_cannot_be_read()
    {
        var session = Session(out _);

        session.Show(Path.Combine(_dir, "missing.json"));

        Assert.Null(session.Current(0));
    }

    [Fact]
    public void Session_restores_and_persists_player_names()
    {
        var cache = new NameCache(Path.Combine(_dir, "names.json"));
        var first = Session(out var tracker, cache);
        Setup(tracker);
        first.Dispose();

        Session(out var restored, cache);

        Assert.Equal("Ilvane", restored.SelfName);
    }

    [Fact]
    public void Detail_view_summarises_quality_and_lists_top_skills()
    {
        var detail = Detail(
            new HitQuality(40, 6, 0.25, null, null, 0.1, null, null, 1_234_567, "Flame Burst"),
            Skill("Flame Burst", 30, 0, 900_000, 0.6, 0.5, 0.3),
            Skill("Burn", 4, 6, 500_000, 0.33, null, null),
            Skill("Tap", 6, 0, 100_000, 0.07, 0, 0));

        var view = MeterView.Detail(detail, maxSkills: 2);

        Assert.Equal("Ilvane", view.Title);
        Assert.Equal("Hits 40+6 · Crit 25% · Back - · Perfect 10%", view.Quality);
        Assert.Equal("Max 1.23M (Flame Burst) · Taken 45.0K (7 hits)", view.Extremes);
        Assert.Equal(["Flame Burst", "Burn"], view.Skills.Select(k => k.Name));
        Assert.Equal(new SkillView("Burn", "4+6", "500.0K", "33%", "250.0K", "-", "-"), view.Skills[1]);
    }

    [Fact]
    public void Detail_view_omits_the_skill_name_when_the_max_hit_is_unknown()
    {
        var view = MeterView.Detail(Detail(new HitQuality(0, 0, null, null, null, null, null, null, 0, null)), maxSkills: 5);

        Assert.StartsWith("Max 0 · ", view.Extremes);
        Assert.Empty(view.Skills);
    }

    [Fact]
    public void Session_detail_follows_self_or_the_clicked_player()
    {
        var session = Session(out var tracker);
        Setup(tracker);
        tracker.Process(new DamageEvent(1_000, Me, Boss, 16040000, 1_000, HitFlags.Critical));
        tracker.Process(new DamageEvent(2_000, Mate, Boss, 11020000, 3_000, HitFlags.None));
        var snapshot = session.Current(2_000);

        Assert.Null(session.Detail(snapshot));

        session.ToggleSelfDetail();
        Assert.Equal(Me, session.Detail(snapshot)!.ActorId);

        session.ToggleDetail(Mate);
        Assert.False(session.DetailSelf);
        Assert.Equal(3_000, session.Detail(snapshot)!.Damage);

        session.ToggleDetail(Mate);
        Assert.Null(session.Detail(snapshot));
    }

    [Fact]
    public void Session_detail_reads_skills_from_a_saved_fight()
    {
        var session = Session(out var tracker);
        Setup(tracker);
        Fight(tracker, start: 1_000, seconds: 10);

        session.Older();
        session.ToggleSelfDetail();
        var detail = session.Detail(session.Current(0));

        Assert.NotNull(detail);
        Assert.Equal(Me, detail.ActorId);
        Assert.Equal(1_000, Assert.Single(detail.Skills).Damage);
    }

    [Fact]
    public void Filter_combines_bosses_and_kills_and_counts_against_the_total()
    {
        var entries = Enumerable.Range(0, 10)
            .Select(i => Entry($"f{i}", minute: i, reason: i < 3 ? EncounterEndReason.Kill : i < 4 ? EncounterEndReason.Wipe : EncounterEndReason.Idle, boss: i < 4 || i == 9))
            .ToList();

        var both = new HistoryFilter(BossesOnly: true, KillsOnly: true);

        Assert.Equal(["f0", "f1", "f2"], both.Apply(entries).Select(e => e.FileName));
        Assert.Equal(5, new HistoryFilter(BossesOnly: true).Apply(entries).Count);
        Assert.Equal(3, new HistoryFilter(KillsOnly: true).Apply(entries).Count);
        Assert.Equal(10, HistoryFilter.None.Apply(entries).Count);
        Assert.False(HistoryFilter.None.IsActive);
        Assert.True(both.IsActive);
    }

    [Fact]
    public void History_row_shows_result_zone_duration_players_and_own_dps_with_place()
    {
        var kill = Entry("a", minute: 5, reason: EncounterEndReason.Kill, zone: "Cuevas", players: 4, selfDps: 1_500, selfPlace: 2, partyDps: 9_000, combatMs: 95_000);

        var row = Assert.Single(MeterView.History([kill]));

        Assert.Equal("✔", row.Result);
        Assert.Equal("Cuevas", row.Zone);
        Assert.Equal("1:35", row.Duration);
        Assert.Equal("4 players", row.Players);
        Assert.Equal("1500/s", row.Dps);
        Assert.Equal("#2", row.Place);
    }

    [Fact]
    public void History_row_falls_back_to_the_party_dps_without_place_when_self_did_not_fight()
    {
        var wipe = Entry("a", minute: 5, reason: EncounterEndReason.Wipe, selfDps: 0, selfPlace: 0, partyDps: 12_000, players: 1);

        var row = Assert.Single(MeterView.History([wipe]));

        Assert.Equal("✖", row.Result);
        Assert.Equal("Party 12.0K/s", row.Dps);
        Assert.Equal("", row.Place);
        Assert.Equal("1 player", row.Players);
        Assert.Equal("•", Assert.Single(MeterView.History([Entry("b", minute: 6, reason: EncounterEndReason.Idle)])).Result);
        Assert.Equal("", Assert.Single(MeterView.History([Entry("c", minute: 7, zone: null)])).Zone);
    }

    [Fact]
    public void Session_navigation_with_the_boss_filter_skips_fights_without_a_boss()
    {
        var session = Session(out _);
        var oldBoss = Save(session, "b1", 1, boss: true);
        Save(session, "n1", 2, boss: false);
        var newBoss = Save(session, "b2", 3, boss: true);
        Save(session, "n2", 4, boss: false);
        session.Filter = new HistoryFilter(BossesOnly: true);

        session.Older();
        Assert.Equal(newBoss, session.SelectedFight);
        Assert.Equal(0, session.SelectedIndex());

        session.Older();
        Assert.Equal(oldBoss, session.SelectedFight);
        Assert.Equal(1, session.SelectedIndex());

        session.Older();
        Assert.Equal(oldBoss, session.SelectedFight);

        session.Newer();
        session.Newer();
        Assert.True(session.IsLive);
        Assert.Equal(2, session.Filtered().Count);
        Assert.Equal(4, session.History.List().Count);
    }

    [Fact]
    public void Session_filter_change_returns_to_live_when_the_shown_fight_no_longer_matches()
    {
        var session = Session(out _);
        Save(session, "n1", 1, boss: false);
        session.Older();

        session.Filter = new HistoryFilter(BossesOnly: true);

        Assert.True(session.IsLive);
    }

    [Fact]
    public void Session_delete_removes_the_file_and_moves_to_the_next_older_filtered_fight()
    {
        var session = Session(out _);
        var oldest = Save(session, "n0", 1, boss: true);
        var middle = Save(session, "n1", 2, boss: true);
        Save(session, "n2", 3, boss: true);
        session.Show(middle);

        Assert.True(session.Delete());

        Assert.False(File.Exists(middle));
        Assert.Equal(oldest, session.SelectedFight);
        Assert.Equal(2, session.History.List().Count);
    }

    [Fact]
    public void Session_delete_of_the_oldest_steps_to_the_newer_one_and_of_the_last_one_goes_live()
    {
        var session = Session(out _);
        var oldest = Save(session, "n0", 1, boss: true);
        var newest = Save(session, "n1", 2, boss: true);
        session.Show(oldest);

        session.Delete();
        Assert.Equal(newest, session.SelectedFight);

        session.Delete();
        Assert.True(session.IsLive);
        Assert.Empty(session.History.List());
    }

    [Fact]
    public void Session_delete_follows_the_filter_when_picking_the_next_fight()
    {
        var session = Session(out _);
        var boss1 = Save(session, "b1", 1, boss: true);
        Save(session, "n1", 2, boss: false);
        var boss2 = Save(session, "b2", 3, boss: true);
        session.Filter = new HistoryFilter(BossesOnly: true);
        session.Show(boss2);

        session.Delete();

        Assert.Equal(boss1, session.SelectedFight);
    }

    [Fact]
    public void Session_delete_does_nothing_while_live()
    {
        var session = Session(out _);
        Save(session, "n0", 1, boss: true);

        Assert.False(session.Delete());

        Assert.Single(session.History.List());
        Assert.True(session.IsLive);
    }

    [Fact]
    public void Header_counts_the_position_over_the_filtered_list()
    {
        var snapshot = Snapshot(Combatant(Me, "Ilvane", 1_000, self: true));

        var header = MeterView.Header(snapshot, new CaptureStatus(CaptureState.Capturing, ""), historyIndex: 0, historyCount: 3);

        Assert.Equal("History 1/3", header.Source);
    }

    [Fact]
    public void Delete_confirmation_asks_then_expires_after_five_seconds()
    {
        var clock = 1_000L;
        var confirmation = new DeleteConfirmation(() => clock);

        Assert.False(confirmation.IsPending("a"));

        confirmation.Request("a");
        Assert.True(confirmation.IsPending("a"));
        Assert.False(confirmation.IsPending("b"));

        clock += 4_999;
        Assert.True(confirmation.IsPending("a"));

        clock += 1;
        Assert.False(confirmation.IsPending("a"));
    }

    [Fact]
    public void Delete_confirmation_cancel_and_take_clear_the_request()
    {
        var clock = 0L;
        var confirmation = new DeleteConfirmation(() => clock);

        confirmation.Request("a");
        confirmation.Cancel();
        Assert.False(confirmation.IsPending("a"));

        confirmation.Request("a");
        Assert.True(confirmation.Take("a"));
        Assert.False(confirmation.IsPending("a"));
        Assert.False(confirmation.Take("a"));

        confirmation.Request("a");
        clock += 5_000;
        Assert.False(confirmation.Take("a"));
    }

    private static HistoryEntry Entry(
        string name, int minute, EncounterEndReason reason = EncounterEndReason.Kill, bool boss = false, string? zone = "Zona",
        int players = 3, double selfDps = 100, int selfPlace = 1, double partyDps = 300, long combatMs = 30_000) =>
        new(name, name, new DateTimeOffset(2026, 10, 6, 18, 0, 0, TimeSpan.Zero).AddMinutes(minute), combatMs, reason, partyDps, 1_000,
            zone, boss ? 2300243 : 0, 0, players, "Ilvane", GameClass.Elementalist, selfDps, selfPlace)
        {
            Path = "/fights/" + name + ".json",
        };

    private static string Save(MeterSession session, string title, int minute, bool boss, EncounterEndReason reason = EncounterEndReason.Kill)
    {
        var self = new CombatantSnapshot(Me, "Ilvane", GameClass.Elementalist, true, 1_000, 100, 1, 10, 0.5, 200, 0);
        var summary = new EncounterSnapshot(
            Guid.NewGuid(), title, "Zona", new DateTimeOffset(2026, 10, 6, 18, minute, 0, TimeSpan.Zero), 30_000, 30_000,
            false, reason, 1_000, 100, boss ? new BossSnapshot(Boss, 2300243, "Jefe", 0, 100) : null, [self]);
        return session.History.Save(new FightRecord(1, summary, []));
    }

    private static SkillRow Skill(string name, int hits, int dots, long damage, double share, double? crit, double? back) =>
        new(1, name, hits, dots, damage, damage / 10.0, share, damage / Math.Max(1, hits + dots), 1, damage / 2, crit, back, null, null, null, null);

    private static CombatantDetail Detail(HitQuality quality, params SkillRow[] skills) =>
        new(Me, "Ilvane", GameClass.Elementalist, true, skills.Sum(k => k.Damage), 100, 0.5, 10_000, 45_000, 7, quality, skills, [], []);

    private MeterSession Session(out CombatTracker tracker, NameCache? names = null)
    {
        tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        return new MeterSession(tracker, new HistoryStore(Path.Combine(_dir, "fights")), null, names, new HistoryPolicy(HistoryMode.All, 20));
    }

    private static void Setup(CombatTracker t)
    {
        t.Process(new SelfIdentifiedEvent(0, Me, "Ilvane", 1304, GameClass.Elementalist));
        t.Process(new PlayerSeenEvent(0, Mate, "Borgrim", 1304, GameClass.Gladiator));
        t.Process(new NpcSeenEvent(0, Boss, 2300243, 120_000_000));
    }

    private static void Fight(CombatTracker t, long start, int seconds)
    {
        t.Process(new DamageEvent(start, Me, Boss, 16040000, 1_000, HitFlags.None));
        t.Process(new DamageEvent(start + seconds * 1_000L, Mate, Boss, 11020000, 2_000, HitFlags.None));
        t.Tick(start + seconds * 1_000L + 3_600_000);
    }

    private static CombatantSnapshot Combatant(uint id, string name, long damage, bool self = false) =>
        new(id, name, GameClass.Cleric, self, damage, damage / 10.0, 0.25, 10, 0.5, damage / 2, 0);

    private static CombatantSnapshot Healer(uint id, string name, long healing, bool self = false) =>
        new(id, name, GameClass.Cleric, self, 0, 0, 0, 0, 0, 0, 0, Healing: healing, Hps: healing / 10.0);

    private static EncounterSnapshot Snapshot(params CombatantSnapshot[] combatants) =>
        new(Guid.NewGuid(), "Fight", "Zone", DateTimeOffset.UnixEpoch, 0, 10_000, false, EncounterEndReason.Idle,
            combatants.Sum(c => c.Damage), 100, null, combatants);
}
