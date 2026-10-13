using System.Diagnostics;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class DisplayFactAttribute : FactAttribute
{
    public DisplayFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))) Skip = "DISPLAY is not set";
    }
}

public sealed class OverlayProcessTests : IDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    private readonly string _root = Directory.CreateTempSubdirectory("overlay-process").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Runtime => Path.Combine(_root, "runtime");

    private string Socket => Path.Combine(Runtime, "aionmeter-overlay.sock");

    [DisplayFact]
    public async Task Quit_command_ends_the_process_with_code_zero()
    {
        using var process = Launch(display: null);
        await WaitForSocket(process);

        Assert.True(CommandChannel.TrySend(Socket, OverlayCommand.Quit));

        Assert.Equal(0, await Exit(process));
    }

    [Fact]
    public async Task An_unhandled_startup_exception_ends_the_process_with_a_non_zero_code()
    {
        using var process = Launch(display: ":9999");

        Assert.NotEqual(0, await Exit(process));
    }

    private Process Launch(string? display)
    {
        Directory.CreateDirectory(Runtime);
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "aionmeter-overlay"))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
        };
        start.ArgumentList.Add("show");
        start.ArgumentList.Add("--demo");
        start.ArgumentList.Add("--hidden");
        start.Environment["XDG_RUNTIME_DIR"] = Runtime;
        start.Environment["XDG_CONFIG_HOME"] = Path.Combine(_root, "config");
        start.Environment["XDG_DATA_HOME"] = Path.Combine(_root, "data");
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
