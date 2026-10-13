namespace AionMeter.Overlay;

public readonly record struct VisibilityInput(long NowMs, bool? GameRunning, bool FightActive, bool Locked, int AutoHideSeconds, bool AutoShow = true, bool ShowOnStart = true);

public sealed class VisibilityPolicy
{
    private bool? _gameRunning;
    private bool _manualHidden;
    private bool _restartIdle = true;
    private long _lastCombatMs;

    public VisibilityPolicy(bool startVisible) => Visible = startVisible;

    public bool Visible { get; private set; }

    public void Show()
    {
        Visible = true;
        _manualHidden = false;
        _restartIdle = true;
    }

    public void Hide()
    {
        Visible = false;
        _manualHidden = true;
    }

    public void Toggle()
    {
        if (Visible) Hide();
        else Show();
    }

    public bool Update(VisibilityInput input)
    {
        ObserveGame(input.GameRunning, input.ShowOnStart);
        if (_restartIdle || input.FightActive || !input.Locked)
        {
            _lastCombatMs = input.NowMs;
            _restartIdle = false;
        }

        if (!input.AutoShow) return Visible;
        if (input.FightActive && _gameRunning == true && !_manualHidden) Visible = true;
        else if (Visible && IdleExpired(input)) Visible = false;
        return Visible;
    }

    private void ObserveGame(bool? running, bool showOnStart)
    {
        if (running is not { } now) return;
        var first = _gameRunning is null;
        var changed = _gameRunning != now;
        _gameRunning = now;
        if (!changed) return;
        if (now)
        {
            if (first && _manualHidden) return;
            _manualHidden = false;
            _restartIdle = true;
            if (showOnStart) Visible = true;
        }
        else if (!first)
        {
            _manualHidden = false;
            Visible = false;
        }
    }

    private bool IdleExpired(VisibilityInput input) =>
        input.Locked && input.AutoHideSeconds > 0 && input.NowMs - _lastCombatMs >= input.AutoHideSeconds * 1000L;
}
