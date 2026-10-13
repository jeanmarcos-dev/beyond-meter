using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class SkillPanel : StackPanel
{
    private const string Columns = "*,46,58,38,58,40,40";
    private const int MaxSkills = 15;

    public const string CopyLabel = "Copiar";

    private static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(0xA8, 0xB3, 0xC7));

    private readonly TextBlock _title = new() { FontSize = 14, FontWeight = FontWeight.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _quality = new() { Foreground = Muted, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _extremes = new() { Foreground = Muted, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly List<(Grid Line, TextBlock[] Cells)> _lines = [];

    public const string DetailLabel = "Ver detalle";

    public Button DetailButton { get; } = new() { Content = DetailLabel, FontSize = 11, Padding = new Thickness(8, 1), Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };

    public Image ClassIcon { get; } = new() { Width = 18, Height = 18, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };

    public TextBlock LevelText { get; } = new() { Foreground = Muted, FontSize = 12, Margin = new Thickness(6, 0), VerticalAlignment = VerticalAlignment.Center };

    public Button CopyButton { get; } = new() { Content = CopyLabel, FontSize = 11, Padding = new Thickness(8, 1), VerticalAlignment = VerticalAlignment.Center };

    public SkillPanel()
    {
        Margin = new Thickness(0, 8, 0, 0);
        Children.Add(new Border { Height = 1, Background = Muted, Opacity = 0.4, Margin = new Thickness(0, 0, 0, 6) });
        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto") };
        heading.Children.Add(ClassIcon);
        Grid.SetColumn(_title, 1);
        heading.Children.Add(_title);
        Grid.SetColumn(LevelText, 2);
        heading.Children.Add(LevelText);
        Grid.SetColumn(DetailButton, 3);
        heading.Children.Add(DetailButton);
        Grid.SetColumn(CopyButton, 4);
        heading.Children.Add(CopyButton);
        Children.Add(heading);
        Children.Add(_quality);
        Children.Add(_extremes);
        Children.Add(Line(["Skill", "Golpes", "Daño", "%", "Máx", "Crit", "Esp."], Muted, 11, out _));
        for (var i = 0; i < MaxSkills; i++)
        {
            var line = Line(new string[7], Brushes.White, 12, out var cells);
            line.IsVisible = false;
            _lines.Add((line, cells));
            Children.Add(line);
        }
    }

    public void Show(DetailView detail)
    {
        var rgb = ClassColors.Rgb(detail.Class);
        _title.Foreground = new SolidColorBrush(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
        _title.Text = "Skills de " + detail.Title;
        ClassIcon.Source = ClassIcons.For(detail.Class);
        ClassIcon.IsVisible = ClassIcon.Source is not null;
        LevelText.Text = detail.Level;
        LevelText.IsVisible = detail.Level.Length > 0;
        _quality.Text = detail.Quality;
        _extremes.Text = detail.Extremes;
        for (var i = 0; i < _lines.Count; i++)
        {
            var (line, cells) = _lines[i];
            var visible = i < detail.Skills.Count;
            line.IsVisible = visible;
            if (!visible) continue;
            var s = detail.Skills[i];
            string[] values = [s.Name, s.Hits, s.Damage, s.Share, s.Max, s.Crit, s.Back];
            for (var c = 0; c < cells.Length; c++) cells[c].Text = values[c];
        }
    }

    private static Grid Line(string[] texts, IBrush foreground, double size, out TextBlock[] cells)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(Columns), Margin = new Thickness(6, 1) };
        cells = new TextBlock[texts.Length];
        for (var i = 0; i < texts.Length; i++)
        {
            var cell = new TextBlock
            {
                Text = texts[i],
                Foreground = foreground,
                FontSize = size,
                HorizontalAlignment = i == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(cell, i);
            grid.Children.Add(cell);
            cells[i] = cell;
        }
        return grid;
    }
}
