using AionMeter.Core.Combat;
using AionMeter.Core.Updates;
using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class VersionTests
{
    [Fact]
    public void Core_and_overlay_carry_the_version_of_the_build_props()
    {
        var expected = UpdateFeed.ParseVersion(Repository.BuildProperty("Version"));

        Assert.Equal(new Version(2, 2, 0), expected);
        Assert.Equal(expected, UpdateFeed.Normalize(typeof(CombatTracker).Assembly.GetName().Version!));
        Assert.Equal(expected, UpdateFeed.Normalize(typeof(OverlayWindow).Assembly.GetName().Version!));
    }

    [Fact]
    public void Build_props_name_the_upstream_version_the_fork_is_based_on()
    {
        Assert.NotNull(UpdateFeed.ParseVersion(Repository.BuildProperty("UpstreamVersion")));
    }
}
