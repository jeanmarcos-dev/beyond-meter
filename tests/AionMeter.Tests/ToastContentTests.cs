using System.Xml.Linq;
using AionMeter.Overlay.Platform;

namespace AionMeter.Tests;

public sealed class ToastContentTests
{
    [Fact]
    public void A_toast_shows_the_title_and_body_with_the_reminder_sound()
    {
        var toast = XElement.Parse(ToastContent.Xml("Reaparece Kromede", "Templo · 21:30"));

        Assert.Equal(["Reaparece Kromede", "Templo · 21:30"], toast.Descendants("text").Select(t => t.Value));
        Assert.Equal("ToastGeneric", toast.Element("visual")!.Element("binding")!.Attribute("template")!.Value);
        Assert.Equal(ToastContent.Sound, toast.Element("audio")!.Attribute("src")!.Value);
    }

    [Fact]
    public void Markup_in_names_stays_text()
    {
        var toast = XElement.Parse(ToastContent.Xml("<b>Boss & co</b>", "\"zone\""));

        Assert.Equal(["<b>Boss & co</b>", "\"zone\""], toast.Descendants("text").Select(t => t.Value));
    }
}
