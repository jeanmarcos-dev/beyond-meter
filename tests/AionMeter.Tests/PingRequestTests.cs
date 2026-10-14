using AionMeter.Core.Ping;
using AionMeter.Core.Protocol;

namespace AionMeter.Tests;

public class PingRequestTests
{
    private static byte[] Frame(int size) => [(byte)(size + 3), .. Enumerable.Repeat((byte)0xAB, size - 1)];

    [Theory]
    [InlineData(13)]
    [InlineData(12)]
    public void A_segment_holding_one_ping_sized_frame_is_a_request(int size)
    {
        Assert.True(PingRequest.Detect(Frame(size)));
    }

    [Theory]
    [InlineData("0F09AA8BE103BEB5A7CE81E3", true)]
    [InlineData("0EB142805654707EE24798", false)]
    public void Requests_from_a_global_capture_are_told_from_the_client_heartbeat(string hex, bool request)
    {
        Assert.Equal(request, PingRequest.Detect(Wire.FromHex(hex)));
    }

    [Fact]
    public void A_ping_sized_frame_among_others_is_a_request()
    {
        Assert.True(PingRequest.Detect([.. Frame(40), .. Frame(13)]));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(14)]
    [InlineData(40)]
    public void A_segment_without_a_ping_sized_frame_is_not_a_request(int size)
    {
        Assert.False(PingRequest.Detect(Frame(size)));
    }

    [Fact]
    public void A_segment_that_does_not_frame_cleanly_is_not_a_request()
    {
        Assert.False(PingRequest.Detect([.. Frame(13), 0x05]));
        Assert.False(PingRequest.Detect([.. Frame(13), 0xFF, 0xFF]));
        Assert.False(PingRequest.Detect([0x02, .. Frame(13)]));
        Assert.False(PingRequest.Detect([]));
    }

    [Theory]
    [InlineData("C0010523 0C0F AB")]
    [InlineData("0523 0D10")]
    public void A_relayed_request_is_recognised_by_its_wrapper(string hex)
    {
        Assert.True(PingRequest.Detect(Wire.FromHex(hex.Replace(" ", ""))));
    }
}
