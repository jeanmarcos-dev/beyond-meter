using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Storage;
using AionMeter.Core.Timers;

namespace AionMeter.Overlay;

public sealed class MeterSession : IDisposable
{
    private const long NamesSaveIntervalMs = 5_000;
    private const long ReplayCloseMs = 3_600_000;

    private IEventSource? _capture;
    private readonly NameCache? _names;
    private readonly BossTimers? _timers;
    private readonly string? _capturesDirectory;
    private int _savedNamesVersion;
    private long _lastNamesSaveMs;
    private (string Path, FightRecord? Record)? _loaded;
    private HistoryFilter _filter = HistoryFilter.None;

    public MeterSession(CombatTracker tracker, HistoryStore history, IEventSource? capture, NameCache? names, HistoryPolicy policy, BossTimers? timers = null, string? capturesDirectory = null, bool replay = false)
    {
        _timers = timers;
        _capturesDirectory = capturesDirectory;
        IsReplay = replay;
        Tracker = tracker;
        History = history;
        _capture = capture;
        _names = names;
        Policy = policy;
        Tracker.EncounterFinished += OnEncounterFinished;
        if (_names?.Load() is { } cached) Tracker.ImportCache(cached);
        _savedNamesVersion = Tracker.CacheVersion;
        if (_timers is null) return;
        Tracker.BossNoticed += _timers.OnNotice;
        Tracker.FieldBossListed += _timers.OnList;
        Tracker.SelfIdentified += OnSelfIdentified;
        _timers.SetCurrentServer(Tracker.SelfServerId);
    }

    public CombatTracker Tracker { get; }
    public HistoryPolicy Policy { get; set; }
    public HistoryStore History { get; }
    public string? SelectedFight { get; private set; }
    public HistoryFilter Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            if (SelectedFight is not null && SelectedIndex() < 0) ShowLive();
        }
    }
    public bool IsLive => SelectedFight is null;
    public bool IsReplay { get; }
    public bool IsRecording => _capture is IRecordable { IsRecording: true };
    public bool DetailSelf { get; private set; }
    public uint? DetailActor { get; private set; }
    public bool? GameRunning => _capture is null || IsReplay ? true : Status.State switch
    {
        CaptureState.Capturing or CaptureState.Error => true,
        CaptureState.WaitingForGame => !Status.Message.StartsWith(MeterView.GameClosedMessage, StringComparison.Ordinal),
        _ => null,
    };
    public CaptureStatus Status => _capture?.Status ?? CaptureStatus.Stopped;

    public event Action<string>? FightSaved;

    public void Start()
    {
        if (_capture is null) return;
        _capture.EventDecoded += Tracker.Process;
        if (IsReplay) _capture.StatusChanged += OnReplayStatus;
        _capture.Start();
    }

    public void ReplaceCapture(IEventSource capture)
    {
        var recording = IsRecording;
        StopCapture();
        _capture = capture;
        Start();
        if (recording) ToggleRecording();
    }

    public bool ToggleRecording()
    {
        if (_capture is not IRecordable recorder || _capturesDirectory is null) return false;
        try
        {
            if (recorder.IsRecording) recorder.StopRecording();
            else recorder.StartRecording(_capturesDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("No se pudo grabar: " + ex.Message);
        }
        return recorder.IsRecording;
    }

    public void Tick(long nowMs)
    {
        if (IsReplay) return;
        Tracker.Tick(nowMs);
        if (_names is null || nowMs - _lastNamesSaveMs < NamesSaveIntervalMs) return;
        _lastNamesSaveMs = nowMs;
        SaveNames();
    }

    public void Reset()
    {
        Tracker.Reset();
        ShowLive();
    }

    public void ShowLive() => SelectedFight = null;

    public void Show(string path) => SelectedFight = path;

    public void Older() => Step(+1);

    public void Newer() => Step(-1);

    public IReadOnlyList<HistoryEntry> Filtered(int max = int.MaxValue) => Filter.Apply(History.List()).Take(max).ToList();

    public bool Delete()
    {
        if (SelectedFight is not { } path) return false;
        var index = SelectedIndex();
        History.Delete(path);
        var list = Filtered();
        _loaded = null;
        if (index < 0 || list.Count == 0) ShowLive();
        else SelectedFight = list[Math.Min(index, list.Count - 1)].Path;
        return true;
    }

    public int SelectedIndex()
    {
        if (SelectedFight is not { } path) return -1;
        var list = Filtered();
        for (var i = 0; i < list.Count; i++)
            if (list[i].Path == path) return i;
        return -1;
    }

    public EncounterSnapshot? Current(long nowMs) =>
        SelectedFight is null ? Tracker.Snapshot(null, nowMs) : LoadedRecord()?.Summary;

    public void ToggleSelfDetail()
    {
        DetailSelf = !DetailSelf;
        DetailActor = null;
    }

    public void ToggleDetail(uint actorId)
    {
        var open = DetailActor == actorId;
        DetailSelf = false;
        DetailActor = open ? null : actorId;
    }

    public CombatantDetail? Detail(EncounterSnapshot? snapshot)
    {
        var actor = DetailSelf ? snapshot?.Combatants.FirstOrDefault(c => c.IsSelf)?.ActorId : DetailActor;
        if (snapshot is null || actor is not { } id) return null;
        return SelectedFight is null
            ? Tracker.Detail(null, id)
            : LoadedRecord()?.Details.FirstOrDefault(d => d.ActorId == id);
    }

    public BreakdownSource? Breakdown(long nowMs)
    {
        if (SelectedFight is not null)
        {
            if (LoadedRecord() is not { } record) return null;
            return new BreakdownSource(false, _ => record.Summary, id => record.Details.FirstOrDefault(d => d.ActorId == id));
        }
        if (Tracker.Snapshot(null, nowMs)?.Id is not { } segment) return null;
        return new BreakdownSource(true, now => Tracker.Snapshot(segment, now), id => Tracker.Detail(segment, id));
    }

    public void Dispose()
    {
        StopCapture();
        Tracker.EncounterFinished -= OnEncounterFinished;
        if (_timers is not null)
        {
            Tracker.BossNoticed -= _timers.OnNotice;
            Tracker.FieldBossListed -= _timers.OnList;
            Tracker.SelfIdentified -= OnSelfIdentified;
        }
        SaveNames();
    }

    private void StopCapture()
    {
        if (_capture is null) return;
        _capture.EventDecoded -= Tracker.Process;
        _capture.StatusChanged -= OnReplayStatus;
        _capture.Dispose();
    }

    private void OnReplayStatus(CaptureStatus status)
    {
        if (status.State == CaptureState.Stopped) Tracker.Tick(Tracker.LastEventMs + ReplayCloseMs);
    }

    private void OnSelfIdentified(string name) => _timers?.SetCurrentServer(Tracker.SelfServerId);

    private FightRecord? LoadedRecord()
    {
        if (SelectedFight is not { } path) return null;
        if (_loaded is not { } loaded || loaded.Path != path) _loaded = loaded = (path, History.Load(path));
        return loaded.Record;
    }

    private void Step(int delta)
    {
        var list = Filtered();
        var next = SelectedIndex() + delta;
        if (next < 0 || list.Count == 0)
        {
            ShowLive();
            return;
        }
        SelectedFight = list[Math.Min(next, list.Count - 1)].Path;
    }

    private void SaveNames()
    {
        if (_names is null || Tracker.CacheVersion == _savedNamesVersion) return;
        _savedNamesVersion = Tracker.CacheVersion;
        _names.Save(Tracker.ExportCache());
    }

    private void OnEncounterFinished(FightRecord record)
    {
        if (IsReplay || !Policy.ShouldSave(record.Summary)) return;
        try
        {
            var path = History.Save(record);
            FightSaved?.Invoke(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("No se pudo guardar la pelea: " + ex.Message);
        }
    }
}
