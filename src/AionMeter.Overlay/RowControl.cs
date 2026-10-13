using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AionMeter.Overlay;

public sealed class RowControl : Grid
{
    private readonly Border _bar = new() { CornerRadius = new CornerRadius(3) };
    private readonly ColumnDefinition _filled = new(0, GridUnitType.Star);
    private readonly ColumnDefinition _empty = new(1, GridUnitType.Star);
    private readonly TextBlock _name = Cell(HorizontalAlignment.Left);
    private readonly TextBlock _dps = Cell(HorizontalAlignment.Right);
    private readonly TextBlock _damage = Cell(HorizontalAlignment.Right);
    private readonly TextBlock _share = Cell(HorizontalAlignment.Right);
    private readonly TextBlock _crit = Cell(HorizontalAlignment.Right);

    public Image ClassIcon { get; } = new() { Width = 16, Height = 16, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };

    public TextBlock LevelText { get; } = new()
    {
        Foreground = Brushes.Silver,
        FontSize = 11,
        Margin = new Thickness(4, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center,
    };

    public RowControl()
    {
        Height = 24;
        Background = Brushes.Transparent;
        Margin = new Thickness(0, 1);

        var track = new Grid { ColumnDefinitions = { _filled, _empty } };
        track.Children.Add(_bar);
        Children.Add(track);

        var text = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,64,64,40,40"),
            Margin = new Thickness(6, 0),
        };
        var who = new Grid { ColumnDefinitions = new ColumnDefinitions("20,*,Auto") };
        Place(who, ClassIcon, 0);
        Place(who, _name, 1);
        Place(who, LevelText, 2);
        Place(text, who, 0);
        Place(text, _dps, 1);
        Place(text, _damage, 2);
        Place(text, _share, 3);
        Place(text, _crit, 4);
        _damage.Foreground = Brushes.Gainsboro;
        _share.Foreground = Brushes.Silver;
        _crit.Foreground = Brushes.Silver;
        Children.Add(text);
    }

    public uint ActorId { get; private set; }

    public void Show(RowView row)
    {
        IsVisible = true;
        ActorId = row.ActorId;
        _filled.Width = new GridLength(Math.Max(row.Bar, 0.0001), GridUnitType.Star);
        _empty.Width = new GridLength(Math.Max(1 - row.Bar, 0.0001), GridUnitType.Star);
        var rgb = ClassColors.Rgb(row.Class);
        _bar.Background = new SolidColorBrush(Color.FromArgb(row.IsSelf ? (byte)0xE0 : (byte)0xA0, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
        _bar.BorderBrush = row.IsSelf ? Brushes.White : null;
        _bar.BorderThickness = new Thickness(row.IsSelf ? 1 : 0);
        _name.Text = $"{row.Place}. {row.Name}";
        _name.FontWeight = row.IsSelf ? FontWeight.Bold : FontWeight.Normal;
        ClassIcon.Source = ClassIcons.For(row.Class);
        LevelText.Text = row.Level;
        LevelText.IsVisible = row.Level.Length > 0;
        _dps.Text = row.Dps;
        _damage.Text = row.Damage;
        _share.Text = row.Share;
        _crit.Text = row.Crit;
    }

    private static TextBlock Cell(HorizontalAlignment alignment) => new()
    {
        HorizontalAlignment = alignment,
        VerticalAlignment = VerticalAlignment.Center,
        Foreground = Brushes.White,
        FontSize = 13,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

    private static void Place(Grid grid, Control child, int column)
    {
        SetColumn(child, column);
        grid.Children.Add(child);
    }
}
