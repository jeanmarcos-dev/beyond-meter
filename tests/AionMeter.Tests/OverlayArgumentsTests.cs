using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class OverlayArgumentsTests
{
    [Fact]
    public void No_arguments_means_show_over_live_capture()
    {
        var parsed = OverlayArguments.Parse([]);

        Assert.Null(parsed.Error);
        Assert.Equal(OverlayCommand.Show, parsed.Command);
        Assert.False(parsed.Demo);
        Assert.False(parsed.Hidden);
        Assert.Null(parsed.ReplayPath);
        Assert.False(parsed.Fast);
    }

    [Fact]
    public void Replay_takes_the_next_argument_as_the_file_and_runs_in_real_time_by_default()
    {
        var parsed = OverlayArguments.Parse(["--replay", "/tmp/a.pcap", "--hidden"]);

        Assert.Null(parsed.Error);
        Assert.Equal(OverlayCommand.Show, parsed.Command);
        Assert.Equal("/tmp/a.pcap", parsed.ReplayPath);
        Assert.False(parsed.Fast);
        Assert.True(parsed.Hidden);
    }

    [Fact]
    public void Fast_goes_with_replay_in_any_order()
    {
        var parsed = OverlayArguments.Parse(["--fast", "show", "--replay", "a.pcap"]);

        Assert.Null(parsed.Error);
        Assert.True(parsed.Fast);
        Assert.Equal(OverlayCommand.Show, parsed.Command);
    }

    [Fact]
    public void Record_is_a_command_word()
    {
        Assert.Equal(OverlayCommand.Record, OverlayArguments.Parse(["record"]).Command);
    }

    [Theory]
    [InlineData("--replay")]
    [InlineData("--fast")]
    [InlineData("--replay", "a.pcap", "--demo")]
    [InlineData("record", "--replay", "a.pcap")]
    [InlineData("explode")]
    public void Invalid_combinations_report_an_error(params string[] args)
    {
        Assert.NotNull(OverlayArguments.Parse(args).Error);
    }

    [Fact]
    public void Replay_without_a_file_names_the_flag_in_the_error()
    {
        Assert.Contains("--replay", OverlayArguments.Parse(["--replay"]).Error);
        Assert.Contains("--replay", OverlayArguments.Parse(["--fast"]).Error);
    }
}

public sealed class ReplayFileTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("replay-file").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [PcapFact]
    public void A_valid_capture_has_no_problem()
    {
        Assert.Null(ReplayFile.Problem(GamePcap.Write(Path.Combine(_dir, "ok.pcap"))));
    }

    [Fact]
    public void A_missing_file_is_reported_with_its_path()
    {
        var path = Path.Combine(_dir, "no-existe.pcap");

        Assert.Contains(path, ReplayFile.Problem(path));
    }

    [Fact]
    public void A_file_that_is_not_a_capture_is_reported()
    {
        var path = Path.Combine(_dir, "garbage.pcap");
        File.WriteAllText(path, "esto no es un pcap");

        Assert.NotNull(ReplayFile.Problem(path));
    }
}
