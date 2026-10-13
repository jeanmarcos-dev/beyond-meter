namespace AionMeter.Overlay;

public sealed class DeleteConfirmation(Func<long> clock, long timeoutMs = 5_000)
{
    private string? _path;
    private long _deadline;

    public void Request(string path)
    {
        _path = path;
        _deadline = clock() + timeoutMs;
    }

    public bool IsPending(string? path) => path is not null && _path == path && clock() < _deadline;

    public bool Take(string path)
    {
        var pending = IsPending(path);
        _path = null;
        return pending;
    }

    public void Cancel() => _path = null;
}
