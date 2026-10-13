using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Demo;
using AionMeter.Core.Events;
using AionMeter.Core.Storage;
using AionMeter.Core.Timers;

namespace AionMeter.Overlay;

public sealed record FightRef(string? Path, Guid? Segment, DateTimeOffset StartedAt);

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
    private DateTimeOffset _selectedAt;
    private HistoryFilter _filter = HistoryFilter.None;
    private DemoFightSource? _demo;
    private bool _demoFight;
    private IEventSource? _fileReplay;
    private volatile bool _replayingFile;

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
        Tracker.BossNoticed += OnBossNoticed;
        Tracker.FieldBossListed += OnBossListed;
        Tracker.SelfIdentified += OnSelfIdentified;
        _timers.SetCurrentServer(Tracker.SelfServerId);
    }

    public CombatTracker Tracker { get; }
    public HistoryPolicy Policy { get; set; }
    public Action<string> Log { get; set; } = Console.Error.WriteLine;
    public HistoryStore History { get; }
    public string? SelectedFight { get; private set; }
    public Guid? SelectedSegment { get; private set; }
    public HistoryFilter Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            if (!IsLive && SelectedIndex() < 0) ShowLive();
        }
    }
    public bool IsLive => SelectedFight is null && SelectedSegment is null;
    public bool IsReplay { get; }
    public bool IsRecording => _capture is IRecordable { IsRecording: true };
    public bool DemoRunning => _demo?.IsRunning == true;
    public bool ReplayingFile => _replayingFile;

    public string? CapturesDirectory => _capturesDirectory;
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

    public void StartDemo(TimeSpan duration)
    {
        _demo?.Dispose();
        ShowLive();
        _demoFight = true;
        _demo = new DemoFightSource(Tracker.Process);
        _demo.Start(duration);
    }

    public void ReplayFile(IEventSource replay)
    {
        _fileReplay?.Dispose();
        Tracker.Reset();
        ShowLive();
        _replayingFile = true;
        if (_capture is not null) _capture.EventDecoded -= Tracker.Process;
        replay.EventDecoded += Tracker.Process;
        replay.StatusChanged += status =>
        {
            if (status.State != CaptureState.Stopped || !_replayingFile) return;
            Tracker.Tick(Tracker.LastEventMs + ReplayCloseMs);
            _savedNamesVersion = Tracker.CacheVersion;
            _replayingFile = false;
            if (_capture is not null) _capture.EventDecoded += Tracker.Process;
        };
        _fileReplay = replay;
        replay.Start();
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
            Log("Could not record: " + ex.Message);
        }
        return recorder.IsRecording;
    }

    public void Tick(long nowMs)
    {
        if (IsReplay || _replayingFile) return;
        Tracker.Tick(nowMs);
        if (!IsLive && Tracker.Segments() is [{ IsActive: true } running, ..] && running.StartedAt > _selectedAt) ShowLive();
        if (DemoRunning || _names is null || nowMs - _lastNamesSaveMs < NamesSaveIntervalMs) return;
        _lastNamesSaveMs = nowMs;
        SaveNames();
    }

    public void Reset()
    {
        Tracker.Reset();
        ShowLive();
    }

    public void ShowLive() => Select(null);

    public void Show(string path) => Select(new FightRef(path, null, default));

    public IReadOnlyList<FightRef> Fights()
    {
        var saved = Filtered();
        var savedStarts = saved.Select(e => e.StartedAt.ToUnixTimeSeconds()).ToHashSet();
        var session = _filter.IsActive
            ? []
            : Tracker.Segments().Where(s => !s.IsActive && !savedStarts.Contains(s.StartedAt.ToUnixTimeSeconds())).Select(s => new FightRef(null, s.Id, s.StartedAt));
        return [.. saved.Select(e => new FightRef(e.Path, null, e.StartedAt)).Concat(session).OrderByDescending(f => f.StartedAt)];
    }

    public void Older() => Step(+1);

    public void Newer() => Step(-1);

    public IReadOnlyList<HistoryEntry> Filtered(int max = int.MaxValue) => Filter.Apply(History.List()).Take(max).ToList();

    public bool Delete()
    {
        if (SelectedFight is not { } path) return false;
        var index = SelectedIndex();
        History.Delete(path);
        var list = Fights();
        _loaded = null;
        if (index < 0 || list.Count == 0) ShowLive();
        else Select(list[Math.Min(index, list.Count - 1)]);
        return true;
    }

    public int SelectedIndex()
    {
        if (IsLive) return -1;
        var list = Fights();
        for (var i = 0; i < list.Count; i++)
            if (list[i].Path == SelectedFight && list[i].Segment == SelectedSegment) return i;
        return -1;
    }

    public EncounterSnapshot? Current(long nowMs) =>
        SelectedSegment is { } segment ? Tracker.Snapshot(segment, nowMs)
        : SelectedFight is null ? Tracker.Snapshot(null, nowMs) : LoadedRecord()?.Summary;

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
        if (SelectedSegment is { } segment) return Tracker.Detail(segment, id);
        return SelectedFight is null
            ? Tracker.Detail(null, id)
            : LoadedRecord()?.Details.FirstOrDefault(d => d.ActorId == id);
    }

    public BreakdownSource? Breakdown(long nowMs)
    {
        if (SelectedSegment is { } chosen)
            return new BreakdownSource(false, now => Tracker.Snapshot(chosen, now), id => Tracker.Detail(chosen, id));
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
        _demo?.Dispose();
        _fileReplay?.Dispose();
        Tracker.EncounterFinished -= OnEncounterFinished;
        if (_timers is not null)
        {
            Tracker.BossNoticed -= OnBossNoticed;
            Tracker.FieldBossListed -= OnBossListed;
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

    private void OnSelfIdentified(string name)
    {
        if (!Simulated) _timers?.SetCurrentServer(Tracker.SelfServerId);
    }

    private bool Simulated => _replayingFile || DemoRunning;

    private void OnBossNoticed(BossNotice notice)
    {
        if (!Simulated) _timers?.OnNotice(notice);
    }

    private void OnBossListed(FieldBossListEvent list)
    {
        if (!Simulated) _timers?.OnList(list);
    }

    private FightRecord? LoadedRecord()
    {
        if (SelectedFight is not { } path) return null;
        if (_loaded is not { } loaded || loaded.Path != path) _loaded = loaded = (path, History.Load(path));
        return loaded.Record;
    }

    private void Step(int delta)
    {
        var list = Fights();
        var next = SelectedIndex() + delta;
        if (next < 0 || list.Count == 0)
        {
            ShowLive();
            return;
        }
        Select(list[Math.Min(next, list.Count - 1)]);
    }

    private void Select(FightRef? fight)
    {
        SelectedFight = fight?.Path;
        SelectedSegment = fight?.Segment;
        _selectedAt = DateTimeOffset.UtcNow;
    }

    private void SaveNames()
    {
        if (_names is null || Tracker.CacheVersion == _savedNamesVersion) return;
        _savedNamesVersion = Tracker.CacheVersion;
        _names.Save(Tracker.ExportCache());
    }

    private void OnEncounterFinished(FightRecord record)
    {
        if (_demoFight)
        {
            if (!DemoRunning) _demoFight = false;
            return;
        }
        if (IsReplay || _replayingFile || !Policy.ShouldSave(record.Summary)) return;
        try
        {
            var path = History.Save(record);
            FightSaved?.Invoke(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log("Could not save the fight: " + ex.Message);
        }
    }
}
