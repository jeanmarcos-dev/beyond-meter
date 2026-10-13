using System.Xml.Linq;

namespace AionMeter.Tests;

public static class Repository
{
    public static string Root { get; } = FindRoot();

    public static string Path(params string[] parts) => System.IO.Path.Combine([Root, .. parts]);

    public static string BuildProperty(string name) =>
        XDocument.Load(Path("Directory.Build.props")).Descendants(name).Single().Value;

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(System.IO.Path.Combine(dir.FullName, "AionMeter.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("AionMeter.sln not found above " + AppContext.BaseDirectory);
    }
}
