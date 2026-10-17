using System.IO;
using System.Text.Json;
using DesktopAdventures.Engine;

namespace DesktopAdventures.Ui;

/// <summary>
/// Pixel-Schrift aus einem Atlas (font.png, weiße Glyphen) + font.json. Erzeugt von tools/make_font.py.
/// Zeichnet im Kunst-Pixel-Raster, daher immer gestochen scharf.
/// </summary>
public sealed class BitmapFont
{
    private sealed class FontFile
    {
        public int LineHeight { get; set; }
        public int Spacing { get; set; }
        public int SpaceWidth { get; set; } = 3;
        public Dictionary<string, int[]> Glyphs { get; set; } = new();
    }

    private readonly Sprite _atlas;
    private readonly Dictionary<char, (int X, int W)> _glyphs = new();
    private readonly int _spacing;
    private readonly int _spaceWidth;

    public int LineHeight { get; }

    /// <summary>Zeilenabstand für Fließtext (etwas enger als die volle Zeilenhöhe).</summary>
    public int LineStep => LineHeight - 2;

    private BitmapFont(Sprite atlas, FontFile file)
    {
        _atlas = atlas;
        LineHeight = file.LineHeight;
        _spacing = file.Spacing;
        _spaceWidth = file.SpaceWidth;
        foreach (var (key, value) in file.Glyphs)
            if (key.Length == 1 && value.Length == 2) _glyphs[key[0]] = (value[0], value[1]);

        if (!_glyphs.ContainsKey('?'))
            throw new InvalidDataException("Schrift enthält kein Ersatzzeichen '?'.");
    }

    public static BitmapFont Load(string directory)
    {
        string json = Path.Combine(directory, "font.json");
        if (!File.Exists(json)) throw new FileNotFoundException($"Schriftdatei nicht gefunden: {json}");

        var file = JsonSerializer.Deserialize<FontFile>(File.ReadAllText(json),
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? throw new InvalidDataException("font.json ist leer.");
        return new BitmapFont(Sprite.Load(Path.Combine(directory, "font.png")), file);
    }

    public int Measure(string text)
    {
        int width = 0;
        foreach (char c in text) width += Advance(c);
        return Math.Max(0, width - _spacing);
    }

    public bool Has(char c) => c == ' ' || _glyphs.ContainsKey(c);

    public void Draw(PixelBuffer target, string text, int x, int y, uint color)
    {
        foreach (char c in text)
        {
            if (c != ' ')
            {
                var (gx, gw) = Glyph(c);
                for (int j = 0; j < LineHeight; j++)
                    for (int i = 0; i < gw; i++)
                        if (_atlas.Pixels[j * _atlas.Width + gx + i] >> 24 != 0)
                            target.SetClipped(x + i, y + j, color);
            }
            x += Advance(c);
        }
    }

    public void DrawCentered(PixelBuffer target, string text, int centerX, int y, uint color) =>
        Draw(target, text, centerX - Measure(text) / 2, y, color);

    public void DrawRight(PixelBuffer target, string text, int rightX, int y, uint color) =>
        Draw(target, text, rightX - Measure(text), y, color);

    /// <summary>Bricht Text auf maxWidth um (Zeilenumbrüche mit \n bleiben erhalten, zu lange Wörter werden getrennt).</summary>
    public List<string> Wrap(string text, int maxWidth)
    {
        var lines = new List<string>();
        foreach (string paragraph in text.Replace("\r", "").Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (Measure(candidate) <= maxWidth) { line = candidate; continue; }

                if (line.Length > 0) lines.Add(line);
                line = word;
                while (Measure(line) > maxWidth && line.Length > 1) // einzelnes Wort zu lang
                {
                    int cut = line.Length - 1;
                    while (cut > 1 && Measure(line[..cut] + "-") > maxWidth) cut--;
                    lines.Add(line[..cut] + "-");
                    line = line[cut..];
                }
            }
            lines.Add(line);
        }
        return lines;
    }

    /// <summary>Kürzt Text auf maxWidth und hängt "…" bzw. "..." an.</summary>
    public string Truncate(string text, int maxWidth)
    {
        if (Measure(text) <= maxWidth) return text;
        string ellipsis = Has('…') ? "…" : "...";
        while (text.Length > 0 && Measure(text + ellipsis) > maxWidth) text = text[..^1];
        return text.TrimEnd() + ellipsis;
    }

    private (int X, int W) Glyph(char c) => _glyphs.TryGetValue(c, out var g) ? g : _glyphs['?'];

    private int Advance(char c) => (c == ' ' ? _spaceWidth : Glyph(c).W) + _spacing;
}
