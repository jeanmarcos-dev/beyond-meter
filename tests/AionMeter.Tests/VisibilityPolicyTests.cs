using AionMeter.Overlay;

namespace AionMeter.Tests;

public sealed class VisibilityPolicyTests
{
    private const long Second = 1_000;

    private static VisibilityInput At(long seconds, bool? game = true, bool fight = false, bool locked = true, int autoHide = 60) =>
        new(seconds * Second, game, fight, locked, autoHide);

    private static VisibilityPolicy Hidden() => new(startVisible: false);

    [Fact]
    public void Closed_game_keeps_a_hidden_overlay_hidden()
    {
        var policy = Hidden();

        Assert.False(policy.Update(At(0, game: false)));
        Assert.False(policy.Update(At(300, game: false)));
    }

    [Fact]
    public void Opening_the_game_shows_the_overlay()
    {
        var policy = Hidden();
        policy.Update(At(0, game: false));

        Assert.True(policy.Update(At(5, game: true)));
    }

    [Fact]
    public void Closing_the_game_hides_the_overlay()
    {
        var policy = Hidden();
        policy.Update(At(0, game: true));

        Assert.False(policy.Update(At(5, game: false)));
    }

    [Fact]
    public void Unknown_game_state_changes_nothing()
    {
        var policy = Hidden();
        policy.Update(At(0, game: true));

        Assert.True(policy.Update(At(5, game: null)));
    }

    [Fact]
    public void Starting_visible_with_the_game_closed_stays_visible_until_the_game_state_changes()
    {
        var policy = new VisibilityPolicy(startVisible: true);

        Assert.True(policy.Update(At(0, game: false, locked: false)));
        Assert.True(policy.Update(At(5, game: false, locked: false)));
    }

    [Fact]
    public void Hides_after_the_configured_seconds_without_combat_while_locked()
    {
        var policy = Hidden();
        policy.Update(At(0));
        policy.Update(At(10, fight: true));

        Assert.True(policy.Update(At(69)));
        Assert.False(policy.Update(At(70)));
    }

    [Fact]
    public void Zero_disables_auto_hide()
    {
        var policy = Hidden();
        policy.Update(At(0, autoHide: 0));

        Assert.True(policy.Update(At(100_000, autoHide: 0)));
    }

    [Fact]
    public void Unlocked_overlay_never_auto_hides_and_restarts_the_count_when_locked_again()
    {
        var policy = Hidden();
        policy.Update(At(0, locked: false));

        Assert.True(policy.Update(At(600, locked: false)));
        Assert.True(policy.Update(At(601, locked: true)));
        Assert.True(policy.Update(At(659, locked: true)));
        Assert.False(policy.Update(At(660, locked: true)));
    }

    [Fact]
    public void A_new_fight_shows_an_auto_hidden_overlay()
    {
        var policy = Hidden();
        policy.Update(At(0));
        Assert.False(policy.Update(At(60)));

        Assert.True(policy.Update(At(90, fight: true)));
    }

    [Fact]
    public void A_fight_in_progress_prevents_auto_hide()
    {
        var policy = Hidden();
        policy.Update(At(0));

        Assert.True(policy.Update(At(50, fight: true)));
        Assert.True(policy.Update(At(100, fight: true)));
        Assert.True(policy.Update(At(159)));
        Assert.False(policy.Update(At(160)));
    }

    [Fact]
    public void Manual_hide_blocks_auto_show_during_a_fight()
    {
        var policy = Hidden();
        policy.Update(At(0));
        policy.Hide();

        Assert.False(policy.Update(At(5, fight: true)));
        Assert.False(policy.Update(At(100)));
        Assert.False(policy.Update(At(110, fight: true)));
    }

    [Fact]
    public void Showing_by_hand_lifts_the_manual_hide_and_restarts_the_idle_count()
    {
        var policy = Hidden();
        policy.Update(At(0));
        policy.Hide();
        policy.Update(At(500));

        policy.Show();

        Assert.True(policy.Update(At(501)));
        Assert.True(policy.Update(At(560)));
        Assert.False(policy.Update(At(561)));
        Assert.True(policy.Update(At(600, fight: true)));
    }

    [Fact]
    public void Manual_hide_before_the_first_game_reading_is_kept()
    {
        var policy = new VisibilityPolicy(startVisible: true);
        policy.Hide();

        Assert.False(policy.Update(At(0, game: true)));
        Assert.False(policy.Update(At(1, game: true, fight: true)));
    }

    [Fact]
    public void Reopening_the_game_after_a_manual_hide_shows_the_overlay()
    {
        var policy = Hidden();
        policy.Update(At(0));
        policy.Hide();
        policy.Update(At(10, game: false));

        Assert.True(policy.Update(At(20, game: true)));
        Assert.True(policy.Update(At(21, fight: true)));
    }

    [Fact]
    public void Manual_hide_with_the_game_running_survives_until_the_game_closes()
    {
        var policy = Hidden();
        policy.Update(At(0));
        policy.Hide();

        Assert.False(policy.Update(At(30, fight: true)));
        Assert.False(policy.Update(At(40, game: false)));
        Assert.False(policy.Update(At(45, game: false, fight: true)));
        Assert.True(policy.Update(At(50, game: true)));
    }

    [Fact]
    public void Toggle_flips_the_visibility_and_records_a_manual_hide()
    {
        var policy = Hidden();
        policy.Update(At(0));

        policy.Toggle();
        Assert.False(policy.Visible);
        Assert.False(policy.Update(At(1, fight: true)));

        policy.Toggle();
        Assert.True(policy.Visible);
        Assert.True(policy.Update(At(2)));
    }

    [Fact]
    public void Toggle_on_an_auto_hidden_overlay_shows_it_without_a_manual_hide()
    {
        var policy = Hidden();
        policy.Update(At(0));
        policy.Update(At(60));

        policy.Toggle();

        Assert.True(policy.Update(At(61)));
        Assert.False(policy.Update(At(121)));
        Assert.True(policy.Update(At(130, fight: true)));
    }

    [Fact]
    public void Manual_show_with_the_game_closed_stays_visible_until_it_idles_out()
    {
        var policy = Hidden();
        policy.Update(At(0, game: false));

        policy.Show();

        Assert.True(policy.Update(At(1, game: false)));
        Assert.True(policy.Update(At(30, game: false)));
        Assert.False(policy.Update(At(61, game: false)));
    }
}
