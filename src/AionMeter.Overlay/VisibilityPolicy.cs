namespace AionMeter.Overlay;

public readonly record struct VisibilityInput(long NowMs, bool? GameRunning, bool FightActive, bool Locked, int AutoHideSeconds);

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
        ObserveGame(input.GameRunning);
        if (_restartIdle || input.FightActive || !input.Locked)
        {
            _lastCombatMs = input.NowMs;
            _restartIdle = false;
        }

        if (input.FightActive && _gameRunning == true && !_manualHidden) Visible = true;
        else if (Visible && IdleExpired(input)) Visible = false;
        return Visible;
    }

    private void ObserveGame(bool? running)
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
            Visible = true;
            _restartIdle = true;
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
