using System.Net;
using AionMeter.Core.Capture;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.Tests;

public class PingPipelineTests
{
    private static readonly IPAddress Server = IPAddress.Parse("193.202.112.99");
    private static readonly IPAddress Client = IPAddress.Parse("192.168.1.5");
    private static readonly FlowKey Down = new(Server, 13328, Client, 50000);
    private static readonly FlowKey Up = new(Client, 50000, Server, 13328);

    private static byte[] Frame(byte[] payload) => [(byte)(payload.Length + 4), .. payload];
    private static readonly byte[] Heartbeat = Frame([0x00, 0x36, 0, 0, 0, 0, 0, 0, 0, 0]);
    private static readonly byte[] Request = [0x10, .. Enumerable.Repeat((byte)0xAB, 12)];

    private readonly List<GameEvent> _events = new();
    private readonly PacketPipeline _pipeline;
    private uint _downSeq = 1000;
    private uint _upSeq = 5000;

    public PingPipelineTests()
    {
        _pipeline = new PacketPipeline(GameData.Empty, _events.Add) { LockThreshold = 3 };
    }

    private void SendDown(long timeMs, byte[] payload)
    {
        _pipeline.OnSegment(timeMs, Down, _downSeq, false, false, payload);
        _downSeq += (uint)payload.Length;
    }

    private void SendUp(long timeMs, byte[] payload, FlowKey? key = null)
    {
        _pipeline.OnSegment(timeMs, key ?? Up, _upSeq, false, false, payload);
        _upSeq += (uint)payload.Length;
    }

    private void Lock(long timeMs)
    {
        for (var i = 0; i < 3; i++) SendDown(timeMs, Heartbeat);
    }

    [Fact]
    public void Locking_a_game_flow_is_announced()
    {
        Lock(700);

        var locked = Assert.Single(_events.OfType<GameFlowLockedEvent>());
        Assert.Equal(700, locked.TimeMs);
    }

    [Fact]
    public void A_ping_request_from_the_client_side_is_reported_with_its_time()
    {
        Lock(700);
        SendUp(1_234, Request);

        Assert.Equal(1_234, Assert.Single(_events.OfType<PingRequestEvent>()).TimeMs);
    }

    [Fact]
    public void Requests_before_the_lock_are_not_reported()
    {
        SendUp(500, Request);
        Lock(700);

        Assert.Empty(_events.OfType<PingRequestEvent>());
    }

    [Fact]
    public void Requests_on_an_unrelated_flow_are_not_reported()
    {
        Lock(700);
        SendUp(1_000, Request, new FlowKey(Client, 50001, Server, 13328));

        Assert.Empty(_events.OfType<PingRequestEvent>());
    }

    [Fact]
    public void The_client_side_is_neither_reassembled_nor_parsed()
    {
        Lock(700);
        var before = _pipeline.Parser.Heartbeats;
        SendUp(1_000, Heartbeat);

        Assert.Equal(before, _pipeline.Parser.Heartbeats);
        Assert.Equal(1, _pipeline.GameFlows);
    }
}
