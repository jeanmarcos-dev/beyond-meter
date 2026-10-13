using System.Net.Sockets;
using System.Text;

namespace AionMeter.Overlay;

public sealed class CommandChannel : IDisposable
{
    private readonly Socket _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;

    private CommandChannel(string path, Action<OverlayCommand> received)
    {
        Path = path;
        if (File.Exists(path)) File.Delete(path);
        _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listener.Bind(new UnixDomainSocketEndPoint(path));
        _listener.Listen(4);
        _loop = Task.Run(() => AcceptLoop(received, _cts.Token));
    }

    public string Path { get; }

    public static string DefaultPath()
    {
        var runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        var dir = string.IsNullOrEmpty(runtime) ? System.IO.Path.GetTempPath() : runtime;
        return System.IO.Path.Combine(dir, "aionmeter-overlay.sock");
    }

    public static CommandChannel Listen(string path, Action<OverlayCommand> received) => new(path, received);

    public static bool IsListening(string path)
    {
        if (!File.Exists(path)) return false;
        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint(path));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public static bool TrySend(string path, OverlayCommand command)
    {
        if (!File.Exists(path)) return false;
        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint(path));
            socket.Send(Encoding.ASCII.GetBytes(OverlayCommands.Name(command) + "\n"));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Dispose();
        try { _loop.Wait(1_000); } catch (AggregateException) { }
        try { File.Delete(Path); } catch (IOException) { }
        _cts.Dispose();
    }

    private async Task AcceptLoop(Action<OverlayCommand> received, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Socket client;
            try
            {
                client = await _listener.AcceptAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }
            using (client)
            {
                if (OverlayCommands.Parse(await ReadLine(client, ct).ConfigureAwait(false)) is { } command) received(command);
            }
        }
    }

    private static async Task<string> ReadLine(Socket client, CancellationToken ct)
    {
        var buffer = new byte[64];
        var text = new StringBuilder();
        try
        {
            int read;
            while (text.Length < buffer.Length && (read = await client.ReceiveAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                text.Append(Encoding.ASCII.GetString(buffer, 0, read));
                if (text.ToString().Contains('\n')) break;
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or SocketException)
        {
        }
        return text.ToString().Split('\n')[0];
    }
}
