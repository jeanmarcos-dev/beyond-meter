using AionMeter.Overlay;

namespace AionMeter.Tests;

public class ShortcutHintsTests
{
    private const string Dump = """
        [aionmeter-lock]
        binding='<Super><Shift>F9'
        command='/home/me/.local/bin/aionmeter-overlay lock'
        name='AION meter: fijar/mover'

        [aionmeter-toggle]
        binding='<Super><Shift>F11'
        command='aionmeter-overlay toggle'
        name='AION meter: mostrar/ocultar'

        [custom0]
        binding='<Control><Alt>t'
        command='ghostty'
        name='Terminal'

        [half]
        command='aionmeter-overlay reset'
        """;

    [Fact]
    public void Parse_maps_overlay_commands_to_readable_bindings()
    {
        var map = ShortcutHints.Parse(Dump);

        Assert.Equal(2, map.Count);
        Assert.Equal("Super+Shift+F9", map[OverlayCommand.Lock]);
        Assert.Equal("Super+Shift+F11", map[OverlayCommand.Toggle]);
    }

    [Theory]
    [InlineData("<Super><Shift>F9", "Super+Shift+F9")]
    [InlineData("<Control><Alt>m", "Ctrl+Alt+M")]
    [InlineData("<Primary><Shift>l", "Ctrl+Shift+L")]
    [InlineData("Pause", "Pause")]
    public void Format_turns_gnome_accelerators_into_labels(string accelerator, string expected)
    {
        Assert.Equal(expected, ShortcutHints.Format(accelerator));
    }

    [Fact]
    public void Parse_ignores_other_programs_and_unknown_words()
    {
        var map = ShortcutHints.Parse("""
            [x]
            binding='<Super>F1'
            command='/usr/bin/other-overlay lock'

            [y]
            binding='<Super>F2'
            command='aionmeter-overlay explode'
            """);

        Assert.Empty(map);
    }

    [Fact]
    public void Footer_names_the_unlock_action_while_locked()
    {
        var map = ShortcutHints.Parse(Dump);

        Assert.Equal("Super+Shift+F9 fijar · Super+Shift+F11 ocultar", ShortcutHints.Footer(map, locked: false));
        Assert.Equal("Super+Shift+F9 soltar · Super+Shift+F11 ocultar", ShortcutHints.Footer(map, locked: true));
    }

    [Fact]
    public void Footer_is_null_without_lock_or_toggle_shortcuts()
    {
        Assert.Null(ShortcutHints.Footer(new Dictionary<OverlayCommand, string>(), locked: false));
        Assert.Equal("Super+Shift+F11 ocultar", ShortcutHints.Footer(new Dictionary<OverlayCommand, string> { [OverlayCommand.Toggle] = "Super+Shift+F11" }, locked: true));
    }
}
