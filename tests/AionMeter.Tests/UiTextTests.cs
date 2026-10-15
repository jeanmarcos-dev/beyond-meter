using System.Reflection;
using System.Text.RegularExpressions;
using AionMeter.Core;
using AionMeter.Core.Text;

namespace AionMeter.Tests;

public sealed class UiTextTests
{
    private static readonly HashSet<string> SameInSpanish =
        ["ColDps", "TileDps", "ThDps", "Seconds", "HoursShort", "MinutesShort", "Wipe", "SecOverlay", "OvError", "OvMinutes", "OvNo", "LayoutPing"];

    private static readonly HashSet<string> SameInRussian = ["TileDps", "ThDps"];

    private static readonly HashSet<string> SameInSpanishChat = ["Wipe", "ColDps", "GearScore", "CombatPower"];

    private static readonly Regex Placeholder = new(@"(?<!\{)\{(\d+)(:[^}]*)?\}(?!\})");

    [Fact]
    public void Default_interface_text_is_english()
    {
        Assert.Equal("en", new UiText().Code);
    }

    [Fact]
    public void English_spanish_and_russian_are_offered()
    {
        Assert.Equal(["en", "es", "ru"], UiText.Languages);
    }

    [Theory]
    [InlineData("es", "es")]
    [InlineData("ru", "ru")]
    [InlineData("en", "en")]
    [InlineData("fr", "en")]
    [InlineData(null, "en")]
    public void Unknown_languages_fall_back_to_english(string? saved, string expected)
    {
        Assert.Equal(expected, UiText.Normalize(saved));
    }

    [Fact]
    public void Spanish_interface_formats_dates_in_spanish()
    {
        var t = UiText.For("es");

        Assert.Equal("es", t.Code);
        Assert.Equal("Español", t.LanguageName);
        Assert.Equal("13 de octubre de 2026", new DateTime(2026, 10, 13).ToString(t.DateFormat, t.Culture));
    }

    [Fact]
    public void English_and_russian_dates_keep_their_format()
    {
        Assert.Equal("October 13, 2026", new DateTime(2026, 10, 13).ToString(new UiText().DateFormat, new UiText().Culture));
        var ru = UiText.For("ru");
        Assert.Equal("13 октября 2026", new DateTime(2026, 10, 13).ToString(ru.DateFormat, ru.Culture));
    }

    [Fact]
    public void Every_interface_text_has_a_spanish_version()
    {
        var (en, es) = (new UiText(), Spanish());

        var missing = StringProperties<UiText>()
            .Where(p => p.Name is not ("Code" or "LanguageName" or "DateFormat") && !SameInSpanish.Contains(p.Name))
            .Where(p => Equals(p.GetValue(en), p.GetValue(es)))
            .Select(p => p.Name)
            .ToList();

        Assert.True(missing.Count == 0, "Not translated: " + string.Join(", ", missing));
    }

    [Fact]
    public void Spanish_texts_keep_the_values_of_the_english_ones()
    {
        var (en, es) = (new UiText(), Spanish());

        var mismatched = StringProperties<UiText>()
            .Where(p => !SamePlaceholders((string)p.GetValue(en)!, (string)p.GetValue(es)!))
            .Select(p => p.Name)
            .ToList();

        Assert.True(mismatched.Count == 0, "Placeholders differ: " + string.Join(", ", mismatched));
    }

    [Fact]
    public void Every_interface_text_has_a_russian_version()
    {
        var (en, ru) = (new UiText(), UiText.For("ru"));

        var missing = StringProperties<UiText>()
            .Where(p => p.Name is not ("Code" or "LanguageName" or "DateFormat") && !SameInRussian.Contains(p.Name))
            .Where(p => Equals(p.GetValue(en), p.GetValue(ru)))
            .Select(p => p.Name)
            .ToList();

        Assert.True(missing.Count == 0, "Not translated: " + string.Join(", ", missing));
    }

    [Fact]
    public void Russian_texts_keep_the_values_of_the_english_ones()
    {
        var (en, ru) = (new UiText(), UiText.For("ru"));

        var mismatched = StringProperties<UiText>()
            .Where(p => !SamePlaceholders((string)p.GetValue(en)!, (string)p.GetValue(ru)!))
            .Select(p => p.Name)
            .ToList();

        Assert.True(mismatched.Count == 0, "Placeholders differ: " + string.Join(", ", mismatched));
    }

    [Fact]
    public void Spanish_class_and_hit_names_cover_every_english_one()
    {
        var (en, es) = (new UiText(), Spanish());

        Assert.Equal(en.ClassNames.Keys.Order(), es.ClassNames.Keys.Order());
        Assert.Equal(en.FlagNames.Keys.Order(), es.FlagNames.Keys.Order());
        Assert.DoesNotContain(es.ClassNames, kv => kv.Value == en.ClassNames[kv.Key]);
        Assert.DoesNotContain(es.FlagNames, kv => kv.Value == en.FlagNames[kv.Key] && kv.Key != Core.Events.HitFlags.Dot);
    }

    [Fact]
    public void Spanish_chat_words_are_translated_with_the_same_values()
    {
        var (en, es) = (new UiText().Chat, Spanish().Chat);

        var untranslated = StringProperties<ChatWords>()
            .Where(p => !SameInSpanishChat.Contains(p.Name) && Equals(p.GetValue(en), p.GetValue(es)))
            .Select(p => p.Name)
            .ToList();
        var mismatched = StringProperties<ChatWords>()
            .Where(p => !SamePlaceholders((string)p.GetValue(en)!, (string)p.GetValue(es)!))
            .Select(p => p.Name)
            .ToList();

        Assert.True(untranslated.Count == 0, "Not translated: " + string.Join(", ", untranslated));
        Assert.True(mismatched.Count == 0, "Placeholders differ: " + string.Join(", ", mismatched));
    }

    [Fact]
    public void Spanish_player_count_agrees_in_number()
    {
        var es = Spanish();

        Assert.Equal("1 jugador", es.Players(1));
        Assert.Equal("3 jugadores", es.Players(3));
    }

    private static UiText Spanish() => UiText.For("es");

    private static IEnumerable<PropertyInfo> StringProperties<T>() =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.CanWrite);

    private static bool SamePlaceholders(string english, string spanish) =>
        Placeholders(english).SequenceEqual(Placeholders(spanish));

    private static IEnumerable<string> Placeholders(string text) =>
        Placeholder.Matches(text).Select(m => m.Value).Order();
}
