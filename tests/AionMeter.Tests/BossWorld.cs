using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Timers;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class BossWorld : IDisposable
{
    public const int Map = 1110, Linx = 2400419, Gartua = 2400800, Server = 1001;

    public static readonly DateTimeOffset Now = new(new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Local));

    public BossWorld()
    {
        Directory = System.IO.Directory.CreateTempSubdirectory("boss-world").FullName;
        Data = GameData.Empty;
        Data.Npcs[Linx] = new NpcDef(Linx, "Linx", IsBoss: true, IsDummy: false);
        Data.Npcs[Gartua] = new NpcDef(Gartua, "Gartua", IsBoss: true, IsDummy: false);
        File.WriteAllText(Path.Combine(Directory, "field_boss_maps.json"), """{"maps":{"1110":{"block":2400,"en":"Altgard"}}}""");
    }

    public string Directory { get; }

    public GameData Data { get; }

    public string TimersFile => Path.Combine(Directory, "boss-timers.json");

    public BossTimers Timers(bool persistent = true) => new(TimersFile, Data, Directory, persistent);

    public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);

    public static FieldBossListEvent List(int server, params FieldBossSlot[] slots) =>
        new(Now.ToUnixTimeMilliseconds(), Map, slots.Length, slots, [(byte)slots.Length, .. slots.Select(s => (byte)s.SlotId)], server);

    public static FieldBossSlot Dead(int place, DateTimeOffset back) => new(Map * 100 + place, false, back.ToUnixTimeMilliseconds());

    public static FieldBossSlot Alive(int place, DateTimeOffset since) => new(Map * 100 + place, true, since.ToUnixTimeMilliseconds());
}

public sealed class FakeBossAlert : IBossAlert
{
    public List<(string Title, string Body)> Sent { get; } = [];

    public void Notify(string title, string body) => Sent.Add((title, body));
}
