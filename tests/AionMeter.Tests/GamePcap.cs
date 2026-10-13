using System.Net;
using System.Net.NetworkInformation;
using AionMeter.Core.Capture;
using AionMeter.Core.Protocol;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace AionMeter.Tests;

public static class GamePcap
{
    public const uint SelfId = 14957;
    public const string SelfName = "Nimara";
    public const long HitDamage = 1700;
    public const int SpanSeconds = 30;

    private const string SelfInfo = "ed745e91c12837064e696d61726118051e000000011c000000";
    private const string Hit = "b1ea013600ed7440c0f4007a038000010b199b5f01000000ac52a40d021818";
    private static readonly IPAddress Server = IPAddress.Parse("193.202.112.99");
    private static readonly IPAddress Client = IPAddress.Parse("192.168.1.5");

    public static string Write(string path)
    {
        var writer = new CaptureFileWriterDevice(path);
        writer.Open(new DeviceConfiguration { LinkLayerType = LinkLayers.Ethernet });
        foreach (var raw in Packets()) writer.Write(raw);
        writer.Close();
        return path;
    }

    public static IReadOnlyList<RawCapture> Packets()
    {
        var packets = new List<RawCapture>();
        uint seq = 1_000;
        var second = 1_700_000_000UL;
        packets.Add(Segment(second, 0, seq - 1, syn: true, []));
        void Send(ulong at, byte[] payload)
        {
            packets.Add(Segment(second + at, 0, seq, syn: false, payload));
            seq += (uint)payload.Length;
        }
        var heartbeat = Frame([0x00, 0x36, 0, 0, 0, 0, 0, 0, 0, 0]);
        Send(0, Frame([0x33, 0x36, .. Wire.FromHex(SelfInfo)]));
        for (var i = 0; i < 4; i++) Send(0, heartbeat);
        for (ulong t = 1; t <= SpanSeconds; t += 5)
        {
            Send(t, heartbeat);
            Send(t, Frame([0x04, 0x38, .. Wire.FromHex(Hit)]));
        }
        return packets;
    }

    private static byte[] Frame(byte[] payload) => [(byte)(payload.Length + 4), .. payload];

    private static RawCapture Segment(ulong seconds, ulong micros, uint seq, bool syn, byte[] payload)
    {
        var tcp = new TcpPacket(13328, 50000)
        {
            SequenceNumber = seq,
            Synchronize = syn,
            PayloadData = payload,
        };
        var ip = new IPv4Packet(Server, Client) { PayloadPacket = tcp };
        var ethernet = new EthernetPacket(PhysicalAddress.Parse("02-00-00-00-00-01"), PhysicalAddress.Parse("02-00-00-00-00-02"), EthernetType.IPv4)
        {
            PayloadPacket = ip,
        };
        return new RawCapture(LinkLayers.Ethernet, new PosixTimeval(seconds, micros), ethernet.Bytes);
    }
}
