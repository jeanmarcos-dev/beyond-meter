using System.Globalization;
using AionMeter.Core.Events;

namespace AionMeter.Core.Logging;

public sealed class AppLog
{
    public const long MaxFieldBossLog = 4_000_000;
    public const int MaxFieldBossLists = 2_000;

    private readonly object _gate = new();
    private readonly Func<DateTime> _clock;
    private readonly HashSet<string> _seenLists = [];

    public AppLog(string directory, Func<DateTime>? clock = null)
    {
        Directory = directory;
        _clock = clock ?? (() => DateTime.Now);
    }

    public string Directory { get; }

    public string CurrentFile => Path.Combine(Directory, $"aionmeter_{_clock().ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log");

    public string FieldBossFile => Path.Combine(Directory, "field-boss-lists.log");

    public void Info(string message) => Write("INFO", message);

    public void Error(string message, Exception? ex = null) => Write("ERROR", ex is null ? message : $"{message}: {ex}");

    public void FieldBossList(FieldBossListEvent list)
    {
        var hex = Convert.ToHexString(list.Raw);
        Append(() =>
        {
            if (_seenLists.Count > MaxFieldBossLists || !_seenLists.Add(hex)) return;
            if (File.Exists(FieldBossFile) && new FileInfo(FieldBossFile).Length > MaxFieldBossLog) return;
            var line = string.Create(CultureInfo.InvariantCulture,
                $"{_clock():yyyy-MM-dd HH:mm:ss} map {list.MapId} slots {list.Slots.Count}/{list.Count}{(list.Complete ? "" : " INCOMPLETE")} {hex}");
            File.AppendAllText(FieldBossFile, line + Environment.NewLine);
        });
    }

    private void Write(string level, string message) =>
        Append(() => File.AppendAllText(CurrentFile, string.Create(CultureInfo.InvariantCulture, $"{_clock():HH:mm:ss.fff} [{level}] {message}") + Environment.NewLine));

    private void Append(Action write)
    {
        try
        {
            lock (_gate)
            {
                System.IO.Directory.CreateDirectory(Directory);
                write();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
