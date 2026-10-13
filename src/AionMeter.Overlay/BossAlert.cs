using System.ComponentModel;
using System.Diagnostics;

namespace AionMeter.Overlay;

public interface IBossAlert
{
    void Notify(string title, string body);
}

public sealed class NullBossAlert : IBossAlert
{
    public void Notify(string title, string body)
    {
    }
}

public sealed class DesktopBossAlert(Func<ProcessStartInfo, Process?>? start = null) : IBossAlert
{
    private readonly Func<ProcessStartInfo, Process?> _start = start ?? Process.Start;
    private readonly HashSet<string> _missing = [];

    public void Notify(string title, string body)
    {
        Run("notify-send", "--app-name", "AION meter", "--urgency=critical", "--", title, body);
        Run("canberra-gtk-play", "-i", "alarm-clock-elapsed");
    }

    private void Run(string executable, params string[] arguments)
    {
        if (_missing.Contains(executable)) return;
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        try
        {
            if (_start(info) is { } process) Reap(process);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            _missing.Add(executable);
        }
    }

    private static void Reap(Process process)
    {
        process.StandardInput.Close();
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        Task.Run(() =>
        {
            process.WaitForExit();
            process.Dispose();
        });
    }
}
