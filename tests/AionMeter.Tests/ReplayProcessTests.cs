using System.Diagnostics;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class ReplayProcessTests : IDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    private readonly string _root = Directory.CreateTempSubdirectory("replay-process").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Runtime => Path.Combine(_root, "runtime");

    private string Socket => Path.Combine(Runtime, "aionmeter-overlay.sock");

    private string Data => Path.Combine(_root, "data");

    [Fact]
    public async Task A_missing_file_prints_the_error_and_exits_non_zero_before_any_window_opens()
    {
        var missing = Path.Combine(_root, "no-existe.pcap");
        using var process = Launch(display: ":9999", "--replay", missing);

        var error = await process.StandardError.ReadToEndAsync();

        Assert.NotEqual(0, await Exit(process));
        Assert.Contains(missing, error);
        Assert.False(File.Exists(Socket));
    }

    [Fact]
    public async Task A_file_that_is_not_a_capture_exits_non_zero()
    {
        var garbage = Path.Combine(_root, "garbage.pcap");
        File.WriteAllText(garbage, "esto no es un pcap");
        using var process = Launch(display: ":9999", "--replay", garbage);

        Assert.NotEqual(0, await Exit(process));
    }

    [Fact]
    public async Task Replay_without_a_file_exits_with_the_usage_error()
    {
        using var process = Launch(display: ":9999", "--replay");

        Assert.Equal(2, await Exit(process));
    }

    [DisplayFact]
    public async Task A_fast_replay_opens_and_quits_without_writing_history_names_timers_or_captures()
    {
        var pcap = GamePcap.Write(Path.Combine(_root, "game.pcap"));
        using var process = Launch(display: null, "--replay", pcap, "--fast", "--hidden");
        await WaitForSocket(process);

        Assert.True(CommandChannel.TrySend(Socket, OverlayCommand.Record));
        Assert.True(CommandChannel.TrySend(Socket, OverlayCommand.Quit));

        Assert.Equal(0, await Exit(process));
        var home = Path.Combine(Data, "aionmeter");
        Assert.False(Directory.Exists(Path.Combine(home, "fights")));
        Assert.False(File.Exists(Path.Combine(home, "names-cache.json")));
        Assert.False(File.Exists(Path.Combine(home, "boss-timers.json")));
        Assert.False(Directory.Exists(Path.Combine(home, "captures")));
    }

    [DisplayFact]
    public async Task A_replay_is_refused_while_another_overlay_is_running()
    {
        using var running = Launch(display: null, "show", "--demo", "--hidden");
        await WaitForSocket(running);
        var pcap = GamePcap.Write(Path.Combine(_root, "game.pcap"));
        using var replay = Launch(display: null, "--replay", pcap, "--fast", "--hidden");

        var error = await replay.StandardError.ReadToEndAsync();

        Assert.Equal(1, await Exit(replay));
        Assert.Contains("overlay abierto", error);
        Assert.True(CommandChannel.TrySend(Socket, OverlayCommand.Quit));
        Assert.Equal(0, await Exit(running));
    }

    private Process Launch(string? display, params string[] args)
    {
        Directory.CreateDirectory(Runtime);
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "aionmeter-overlay"))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
        };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        start.Environment["XDG_RUNTIME_DIR"] = Runtime;
        start.Environment["XDG_CONFIG_HOME"] = Path.Combine(_root, "config");
        start.Environment["XDG_DATA_HOME"] = Data;
        if (display is not null) start.Environment["DISPLAY"] = display;
        return Process.Start(start)!;
    }

    private async Task WaitForSocket(Process process)
    {
        var deadline = DateTime.UtcNow + Limit;
        while (!File.Exists(Socket))
        {
            Assert.False(process.HasExited, "the overlay exited before opening its socket");
            Assert.True(DateTime.UtcNow < deadline, "the overlay never opened its socket");
            await Task.Delay(100);
        }
    }

    private static async Task<int> Exit(Process process)
    {
        using var cts = new CancellationTokenSource(Limit);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("the overlay did not exit in time");
        }
        return process.ExitCode;
    }
}
