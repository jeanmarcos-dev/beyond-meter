using System.Net;
using AionMeter.Core.Capture;

namespace AionMeter.Tests;

public class LinuxProcNetTests : IDisposable
{
    private const string TcpHeader =
        "  sl  local_address rem_address   st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode";

    private readonly string _proc = Directory.CreateTempSubdirectory("procnet").FullName;

    public void Dispose() => Directory.Delete(_proc, recursive: true);

    [Theory]
    [InlineData("0100007F:1F90", "127.0.0.1", 8080)]
    [InlineData("2632A8C0:C350", "192.168.50.38", 50000)]
    [InlineData("0370CAC1:3410", "193.202.112.3", 13328)]
    public void ParseEndpoint_reads_little_endian_address_and_big_endian_port(string hex, string ip, int port)
    {
        var (addr, p) = LinuxProcNet.ParseEndpoint(hex);

        Assert.Equal(IPAddress.Parse(ip), addr);
        Assert.Equal(port, p);
    }

    [Theory]
    [InlineData("S:\\steamapps\\common\\AION2\\Aion2\\Binaries\\Win64\\AION2.exe\0-steam\0", true)]
    [InlineData("Z:\\games\\Aion2-Win64-Shipping.exe\0", true)]
    [InlineData("/opt/aion2\0", true)]
    [InlineData("c:\\windows\\system32\\steam.exe\0/mnt/AION2/AION2.exe\0", false)]
    [InlineData("python3\0proton\0waitforexitandrun\0S:\\AION2.exe\0", false)]
    [InlineData("S:/steamapps/common/AION2/Engine/Binaries/Win64/EpicWebHelper.exe\0", false)]
    public void IsGameCommandLine_matches_only_the_game_as_first_argument(string cmdline, bool expected)
    {
        Assert.Equal(expected, LinuxProcNet.IsGameCommandLine(cmdline));
    }

    [Fact]
    public void ParseTcpTable_keeps_established_game_sockets_and_drops_web_ports()
    {
        var table = string.Join('\n',
            TcpHeader,
            "   0: 2632A8C0:C350 0370CAC1:3410 01 00000000:00000000 00:00000000 00000000  1000        0 111 1",
            "   1: 2632A8C0:C351 5421F122:01BB 01 00000000:00000000 00:00000000 00000000  1000        0 222 1",
            "   2: 2632A8C0:C352 0370CAC1:3410 06 00000000:00000000 00:00000000 00000000  1000        0 333 1",
            "   3: 2632A8C0:C353 0370CAC1:3410 01 00000000:00000000 00:00000000 00000000  1000        0 444 1",
            "");

        var conns = LinuxProcNet.ParseTcpTable(table, new HashSet<long> { 111, 222, 333 });

        var c = Assert.Single(conns);
        Assert.Equal(IPAddress.Parse("192.168.50.38"), c.LocalIp);
        Assert.Equal(50000, c.LocalPort);
        Assert.Equal(IPAddress.Parse("193.202.112.3"), c.RemoteIp);
        Assert.Equal(PacketPipeline.GamePort, c.RemotePort);
    }

    [Fact]
    public void FindGameProcessIds_scans_numeric_proc_entries_by_command_line()
    {
        Proc(100, "S:\\AION2\\Binaries\\Win64\\AION2.exe\0-steam\0");
        Proc(200, "/usr/bin/zsh\0");
        Directory.CreateDirectory(Path.Combine(_proc, "self"));

        Assert.Equal([100], LinuxProcNet.FindGameProcessIds(_proc));
    }

    [Fact]
    public void FindConnections_joins_process_sockets_with_the_tcp_table()
    {
        var fd = Proc(100, "AION2.exe\0");
        File.CreateSymbolicLink(Path.Combine(fd, "5"), "socket:[111]");
        File.CreateSymbolicLink(Path.Combine(fd, "6"), "pipe:[999]");
        File.CreateSymbolicLink(Path.Combine(fd, "7"), "/dev/null");
        Directory.CreateDirectory(Path.Combine(_proc, "net"));
        File.WriteAllText(Path.Combine(_proc, "net", "tcp"), string.Join('\n',
            TcpHeader,
            "   0: 2632A8C0:C350 0370CAC1:3410 01 00000000:00000000 00:00000000 00000000  1000        0 111 1",
            "   1: 2632A8C0:C351 0470CAC1:3410 01 00000000:00000000 00:00000000 00000000  1000        0 999 1"));

        var c = Assert.Single(LinuxProcNet.FindConnections(_proc, [100]));

        Assert.Equal(IPAddress.Parse("193.202.112.3"), c.RemoteIp);
    }

    [Fact]
    public void FindConnections_returns_nothing_for_a_process_without_sockets()
    {
        Proc(100, "AION2.exe\0");

        Assert.Empty(LinuxProcNet.FindConnections(_proc, [100]));
    }

    private string Proc(int pid, string cmdline)
    {
        var dir = Path.Combine(_proc, pid.ToString());
        var fd = Path.Combine(dir, "fd");
        Directory.CreateDirectory(fd);
        File.WriteAllText(Path.Combine(dir, "cmdline"), cmdline);
        return fd;
    }
}
