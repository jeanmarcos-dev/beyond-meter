using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Logging;
using AionMeter.Core.Storage;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class AppLogTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("app-log").FullName;
    private DateTime _now = new(2026, 10, 13, 9, 30, 15, 250);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private AppLog Log() => new(Path.Combine(_dir, "logs"), () => _now);

    [Fact]
    public void Lines_go_to_a_file_of_the_day_with_time_and_level()
    {
        var log = Log();

        log.Info("started");
        log.Error("capture failed", new InvalidOperationException("no adapter"));

        var file = Path.Combine(_dir, "logs", "aionmeter_20261013.log");
        Assert.Equal(file, log.CurrentFile);
        var lines = File.ReadAllLines(file);
        Assert.Equal("09:30:15.250 [INFO] started", lines[0]);
        Assert.StartsWith("09:30:15.250 [ERROR] capture failed: System.InvalidOperationException: no adapter", lines[1]);
    }

    [Fact]
    public void A_new_day_starts_a_new_file()
    {
        var log = Log();
        log.Info("one");
        _now = _now.AddDays(1);
        log.Info("two");

        Assert.Equal(["aionmeter_20261013.log", "aionmeter_20261014.log"], Directory.GetFiles(Path.Combine(_dir, "logs")).Select(Path.GetFileName).Order());
    }

    [Fact]
    public void Each_boss_list_is_logged_once_and_the_file_stops_growing_past_its_limit()
    {
        var log = Log();
        var list = new FieldBossListEvent(0, 1110, 2, [new FieldBossSlot(111001, true, 0)], [0xAB, 0xCD]);

        log.FieldBossList(list);
        log.FieldBossList(list);

        var line = Assert.Single(File.ReadAllLines(log.FieldBossFile));
        Assert.Equal("2026-10-13 09:30:15 map 1110 slots 1/2 INCOMPLETE ABCD", line);

        File.WriteAllText(log.FieldBossFile, new string('x', (int)AppLog.MaxFieldBossLog + 1));
        log.FieldBossList(list with { Raw = [0x01] });
        Assert.Equal(AppLog.MaxFieldBossLog + 1, new FileInfo(log.FieldBossFile).Length);
    }

    [Fact]
    public void An_unwritable_folder_never_breaks_the_meter()
    {
        var blocked = Path.Combine(_dir, "file");
        File.WriteAllText(blocked, "");
        var log = new AppLog(Path.Combine(blocked, "logs"));

        log.Info("ignored");
    }

    [Fact]
    public void Session_problems_reach_the_log()
    {
        var messages = new List<string>();
        var folder = Path.Combine(_dir, "fights");
        var store = new HistoryStore(folder);
        Directory.Delete(folder, recursive: true);
        File.WriteAllText(folder, "");
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        var session = new MeterSession(tracker, store, null, null, new HistoryPolicy(Core.Settings.HistoryMode.All, 5))
        {
            Log = messages.Add,
        };
        tracker.Process(new SelfIdentifiedEvent(0, 1, "Ilvane", 1, GameClass.Cleric));
        tracker.Process(new NpcSeenEvent(0, 2, 2000002, 50_000));
        for (var t = 0L; t < 20_000; t += 1_000) tracker.Process(new DamageEvent(t, 1, 2, 16040000, 100, HitFlags.None));

        tracker.Tick(600_000);

        Assert.Contains(messages, m => m.StartsWith("Could not save the fight: ", StringComparison.Ordinal));
        session.Dispose();
    }
}
