using System.Xml.Linq;

namespace AionMeter.Overlay.Platform;

public static class ToastContent
{
    public const string Sound = "ms-winsoundevent:Notification.Reminder";

    public static string Xml(string title, string body) =>
        new XElement("toast",
            new XElement("visual",
                new XElement("binding", new XAttribute("template", "ToastGeneric"),
                    new XElement("text", title),
                    new XElement("text", body))),
            new XElement("audio", new XAttribute("src", Sound)))
        .ToString(SaveOptions.DisableFormatting);
}
