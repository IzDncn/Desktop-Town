using DesktopAdventures.Engine;

namespace DesktopAdventures.World;

/// <summary>Das Buch im Gras: Klick öffnet das Buch-Popup. Ein gelegentliches Funkeln lenkt die Aufmerksamkeit darauf.</summary>
public sealed class BookProp
{
    private const int Width = 14, Height = 10;
    private const uint Outline = 0xFF3A1216, Cover = 0xFF9B2D30, CoverHi = 0xFFC0454A, CoverLo = 0xFF6E1E22;
    private const uint Page = 0xFFF2E3BC, PageShade = 0xFFD2BE90, Gold = 0xFFE6B840, GoldHi = 0xFFFFF0A0;

    private static readonly Sprite Image = Create();

    private readonly int _x;

    public BookProp(int centerX) => _x = centerX;

    private int Left => _x - Width / 2;

    private int Top(Landscape landscape) => landscape.SurfaceY(_x) + 1 - Height + 1;

    public void Draw(PixelBuffer target, Landscape landscape, double time)
    {
        int top = Top(landscape);
        target.Draw(Image, 0, 0, Width, Height, Left, top);

        // Funkeln: kurz alle 3 Sekunden, kreuzförmig
        double phase = time % 3.0;
        if (phase < 0.5)
        {
            int size = phase < 0.25 ? 1 : 2;
            int sx = Left + Width - 3, sy = top - 4;
            target.Set(sx, sy, GoldHi);
            for (int i = 1; i <= size; i++)
            {
                target.Set(sx - i, sy, Gold); target.Set(sx + i, sy, Gold);
                target.Set(sx, sy - i, Gold); target.Set(sx, sy + i, Gold);
            }
        }
    }

    public bool Contains(int artX, int artY, Landscape landscape)
    {
        int top = Top(landscape);
        return artX >= Left - 1 && artX < Left + Width + 1 && artY >= top - 1 && artY < top + Height + 1;
    }

    private static Sprite Create()
    {
        var s = new Sprite(Width, Height);
        s.FillRect(0, 0, Width, Height, Outline);
        s.FillRect(1, 1, Width - 2, Height - 3, Cover);         // Deckel
        s.FillRect(1, 1, Width - 2, 1, CoverHi);                // Licht oben
        s.FillRect(1, 1, 2, Height - 3, CoverLo);               // Buchrücken
        s.FillRect(1, Height - 3, Width - 2, 1, CoverLo);       // Deckelkante
        s.FillRect(2, Height - 2, Width - 4, 1, Page);          // Seitenschnitt
        s.FillRect(2, Height - 2, 1, 1, PageShade);
        s.FillRect(7, 3, 4, 3, Gold);                           // Emblem
        s.FillRect(8, 4, 2, 1, GoldHi);
        foreach (var (x, y) in new[] { (0, 0), (Width - 1, 0), (0, Height - 1), (Width - 1, Height - 1) })
            s.Pixels[y * Width + x] = 0; // Ecken abrunden
        return s;
    }
}
