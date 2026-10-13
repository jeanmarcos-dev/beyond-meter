using AionMeter.Core.Capture;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using PacketDotNet;
using SharpPcap;

namespace AionMeter.Tests;

public sealed class LiveCaptureTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("live-capture").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [PcapFact]
    public void Recording_started_and_stopped_on_an_open_device_leaves_a_pcap_the_replay_can_read()
    {
        var packets = GamePcap.Packets();
        var recorder = new CaptureRecorder();

        var path = recorder.Start(Path.Combine(_dir, "captures"), LinkLayers.Ethernet);
        foreach (var packet in packets) recorder.Write(packet);
        recorder.Stop();

        Assert.False(recorder.IsRecording);
        Assert.Null(recorder.Path);
        Assert.StartsWith("aion2_", Path.GetFileName(path));
        Assert.EndsWith(".pcap", path);
        var events = new List<GameEvent>();
        var replay = new PcapReplaySource(GameData.Empty, path, realtime: false);
        replay.EventDecoded += events.Add;
        Assert.Equal(packets.Count, replay.RunToEnd());
        Assert.Contains(events, e => e is SelfIdentifiedEvent { Name: GamePcap.SelfName });
        Assert.Contains(events, e => e is DamageEvent { SourceId: GamePcap.SelfId });
    }

    [PcapFact]
    public void Packets_written_while_not_recording_are_dropped()
    {
        var recorder = new CaptureRecorder();
        var path = recorder.Start(_dir, LinkLayers.Ethernet);
        recorder.Stop();

        foreach (var packet in GamePcap.Packets()) recorder.Write(packet);

        Assert.Equal(0, new PcapReplaySource(GameData.Empty, path, realtime: false).RunToEnd());
    }

    [PcapFact]
    public void Every_recording_gets_its_own_file()
    {
        var recorder = new CaptureRecorder();

        var first = recorder.Start(_dir, LinkLayers.Ethernet);
        var second = recorder.Start(_dir, LinkLayers.Ethernet);
        recorder.Stop();

        Assert.NotEqual(first, second);
        Assert.Equal(2, Directory.GetFiles(_dir, "*.pcap").Length);
    }

    [Fact]
    public void Starting_to_record_before_the_device_opens_is_remembered_without_creating_a_file()
    {
        using var capture = new LiveCapture(GameData.Empty, new LiveCaptureOptions());

        capture.StartRecording(_dir);

        Assert.True(capture.IsRecording);
        Assert.Null(capture.RecordingPath);
        Assert.Empty(Directory.GetFiles(_dir));

        capture.StopRecording();

        Assert.False(capture.IsRecording);
    }

    [Fact]
    public void The_record_directory_option_still_means_recording()
    {
        using var capture = new LiveCapture(GameData.Empty, new LiveCaptureOptions { RecordDirectory = _dir });

        Assert.True(capture.IsRecording);
    }

    [PcapFact]
    public void Replaying_the_synthetic_game_pcap_produces_the_fight()
    {
        var path = GamePcap.Write(Path.Combine(_dir, "game.pcap"));
        var damage = new List<DamageEvent>();
        var replay = new PcapReplaySource(GameData.Empty, path, realtime: false);
        replay.EventDecoded += e =>
        {
            if (e is DamageEvent d) damage.Add(d);
        };

        replay.RunToEnd();

        Assert.NotEmpty(damage);
        Assert.All(damage, d => Assert.Equal(GamePcap.HitDamage, d.Damage));
    }
}
