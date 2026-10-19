using DesktopAdventures.Engine;

namespace DesktopAdventures.Ui;

/// <summary>Kleines einfarbiges Symbol aus Text ('#' = gefüllt), z. B. für Lesezeichen.</summary>
public sealed class PixelIcon
{
    private readonly string[] _rows;

    public int Width => _rows[0].Length;
    public int Height => _rows.Length;

    public PixelIcon(params string[] rows) => _rows = rows;

    public void Draw(PixelBuffer target, int x, int y, uint color, int scale = 1)
    {
        for (int j = 0; j < _rows.Length; j++)
            for (int i = 0; i < _rows[j].Length; i++)
                if (_rows[j][i] == '#') target.FillRect(x + i * scale, y + j * scale, scale, scale, color);
    }

    public static readonly PixelIcon Person = new(
        "...#####...",
        "..#######..",
        "..#######..",
        "..#######..",
        "...#####...",
        "....###....",
        ".#########.",
        "###########",
        "###########",
        "###########");

    public static readonly PixelIcon House = new(
        ".....#.....",
        "....###....",
        "...#####...",
        "..#######..",
        ".#########.",
        "###########",
        ".#########.",
        ".###...###.",
        ".###...###.",
        ".###...###.");

    public static readonly PixelIcon Star = new(
        ".....#.....",
        "....###....",
        "....###....",
        "###########",
        ".#########.",
        "..#######..",
        "...#####...",
        "..###.###..",
        ".###...###.",
        ".##.....##.");

    public static readonly PixelIcon Check = new(
        "........#",
        ".......##",
        "#.....##.",
        "##...##..",
        ".##.##...",
        "..###....",
        "...#.....");
}
