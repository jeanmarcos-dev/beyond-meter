using System.Globalization;
using System.Net;

namespace AionMeter.Core.Capture;

public static class LinuxProcNet
{
    public const string ProcRoot = "/proc";
    public const string AnyDevice = "any";
    private const string Established = "01";

    public static bool IsGameCommandLine(string cmdline)
    {
        var argv0 = cmdline.Split('\0', 2)[0];
        var name = argv0[(argv0.LastIndexOfAny(['/', '\\']) + 1)..];
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        return GameProcessLocator.ProcessNames.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    public static int[] FindGameProcessIds(string procRoot)
    {
        var ids = new List<int>();
        foreach (var dir in Directory.EnumerateDirectories(procRoot))
        {
            if (!int.TryParse(Path.GetFileName(dir), NumberStyles.None, CultureInfo.InvariantCulture, out var pid)) continue;
            try
            {
                if (IsGameCommandLine(File.ReadAllText(Path.Combine(dir, "cmdline")))) ids.Add(pid);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
        return ids.ToArray();
    }

    public static HashSet<long> SocketInodes(string procRoot, IEnumerable<int> pids)
    {
        var inodes = new HashSet<long>();
        foreach (var pid in pids)
        {
            string[] fds;
            try
            {
                fds = Directory.GetFiles(Path.Combine(procRoot, pid.ToString(CultureInfo.InvariantCulture), "fd"));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }
            foreach (var fd in fds)
            {
                if (SocketInode(fd) is { } inode) inodes.Add(inode);
            }
        }
        return inodes;
    }

    public static List<GameConnection> ParseTcpTable(string table, IReadOnlySet<long> inodes)
    {
        var result = new List<GameConnection>();
        foreach (var line in table.Split('\n').Skip(1))
        {
            var f = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (f.Length < 10 || f[3] != Established) continue;
            if (!long.TryParse(f[9], NumberStyles.None, CultureInfo.InvariantCulture, out var inode) || !inodes.Contains(inode)) continue;
            var (localIp, localPort) = ParseEndpoint(f[1]);
            var (remoteIp, remotePort) = ParseEndpoint(f[2]);
            if (remotePort is 80 or 443) continue;
            result.Add(new GameConnection(localIp, localPort, remoteIp, remotePort));
        }
        return result;
    }

    public static (IPAddress Ip, ushort Port) ParseEndpoint(string hex)
    {
        var colon = hex.IndexOf(':');
        var ip = new IPAddress(uint.Parse(hex.AsSpan(0, colon), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        var port = ushort.Parse(hex.AsSpan(colon + 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (ip, port);
    }

    public static List<GameConnection> FindConnections(string procRoot, IReadOnlyCollection<int> pids)
    {
        var inodes = SocketInodes(procRoot, pids);
        var table = Path.Combine(procRoot, "net", "tcp");
        if (inodes.Count == 0 || !File.Exists(table)) return [];
        return ParseTcpTable(File.ReadAllText(table), inodes);
    }

    private static long? SocketInode(string fd)
    {
        string? target;
        try
        {
            target = new FileInfo(fd).LinkTarget;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
        if (target is null || !target.StartsWith("socket:[", StringComparison.Ordinal) || !target.EndsWith(']')) return null;
        return long.TryParse(target.AsSpan(8, target.Length - 9), NumberStyles.None, CultureInfo.InvariantCulture, out var inode)
            ? inode
            : null;
    }
}
