using DesktopAdventures.Engine;
using static DesktopAdventures.Ui.BookTheme;

namespace DesktopAdventures.Ui;

/// <summary>
/// Das Buch: zeichnet Einband, Seiten und Lesezeichen und leitet Eingaben an den aktiven Reiter weiter.
/// Neue Reiter (z. B. Challenges) werden nur in die Liste aufgenommen.
/// </summary>
public sealed class BookScreen
{
    private readonly UiContext _ui;
    private readonly IReadOnlyList<BookTab> _tabs;
    private int _active;

    public event Action? CloseRequested;

    public bool WantsHandCursor => _ui.OverClickable;

    public BookScreen(BitmapFont font, IReadOnlyList<BookTab> tabs)
    {
        if (tabs.Count == 0) throw new ArgumentException("Das Buch braucht mindestens einen Reiter.", nameof(tabs));
        _ui = new UiContext(font);
        _tabs = tabs;
        _tabs[0].OnOpened();
    }

    public void MouseMove(int x, int y) => _ui.SetMouse(x, y);

    public void MouseLeave() => _ui.SetMouse(-1, -1);

    public void Click(int x, int y) => _ui.Click(x, y);

    public void NextTab(int direction) => SelectTab((_active + direction + _tabs.Count) % _tabs.Count);

    public void Render(PixelBuffer b, double time)
    {
        b.Clear(0);
        _ui.BeginFrame(b, time);

        DrawCover(b);
        DrawPage(b, LeftPage, rightSide: false);
        DrawPage(b, RightPage, rightSide: true);
        DrawSpine(b);
        DrawTabs(b);

        _tabs[_active].Draw(_ui, LeftPage, RightPage);
        DrawCloseButton(b);
    }

    // ---------- Einband, Seiten, Rücken ----------

    private static void DrawCover(PixelBuffer b)
    {
        int h = Height - BodyTop;
        FillRounded(b, 0, BodyTop, Width, h, CoverOutline);
        FillRounded(b, 1, BodyTop + 1, Width - 2, h - 2, CoverMid);
        b.FillRect(2, BodyTop + 1, Width - 4, 1, CoverHi);          // Lichtkante oben
        b.FillRect(1, BodyTop + 2, 1, h - 4, CoverHi);              // links
        b.FillRect(2, Height - 2, Width - 4, 1, CoverLo);           // Schatten unten
        b.FillRect(Width - 2, BodyTop + 2, 1, h - 4, CoverLo);      // rechts

        // Metallbeschläge in den Ecken
        foreach (bool right in new[] { false, true })
        {
            foreach (bool bottom in new[] { false, true })
            {
                int x = right ? Width - 10 : 3;
                int y = bottom ? Height - 10 : BodyTop + 3;
                b.FillRect(x, bottom ? y + 5 : y, 7, 2, Metal);
                b.FillRect(right ? x + 5 : x, y, 2, 7, Metal);
                b.Set(right ? x + 5 : x + 1, bottom ? y + 5 : y + 1, MetalDark); // Niete
            }
        }
    }

    private static void FillRounded(PixelBuffer b, int x, int y, int w, int h, uint color)
    {
        b.FillRect(x, y, w, h, color);
        foreach (var (cx, cy) in new[] { (x, y), (x + w - 1, y), (x, y + h - 1), (x + w - 1, y + h - 1) })
            b.Set(cx, cy, 0);
    }

    private static void DrawPage(PixelBuffer b, Rect page, bool rightSide)
    {
        // Seitenstapel unten
        b.FillRect(page.X + 2, page.Bottom, page.W - 4, 1, PageEdgeLight);
        b.FillRect(page.X + 3, page.Bottom + 1, page.W - 6, 1, PageEdgeDark);

        FillRounded(b, page.X, page.Y, page.W, page.H, PageOutline);
        FillRounded(b, page.X + 1, page.Y + 1, page.W - 2, page.H - 2, Parchment);
        b.FillRect(page.X + 2, page.Y + 1, page.W - 4, 1, ParchmentHi);
        b.StrokeRect(page.X + 4, page.Y + 4, page.W - 8, page.H - 8, PageBorder);

        // Schatten zur Buchmitte hin, mit Schachbrett-Dithering
        for (int k = 0; k < 14; k++)
        {
            int x = rightSide ? page.X + 1 + k : page.Right - 2 - k;
            for (int y = page.Y + 2; y < page.Bottom - 2; y++)
            {
                bool checker = ((x + y) & 1) == 0;
                uint? color = k switch
                {
                    < 3 => ParchmentShade2,
                    < 6 => checker ? ParchmentShade2 : ParchmentShade1,
                    < 9 => ParchmentShade1,
                    < 12 => checker ? (uint?)ParchmentShade1 : null,
                    _ => null,
                };
                if (color is { } c) b.Set(x, y, c);
            }
        }
    }

    private static void DrawSpine(PixelBuffer b)
    {
        b.FillRect(SpineX, PageY, SpineW, PageH, Spine);
        b.FillRect(SpineX + SpineW / 2 - 1, PageY, 2, PageH, SpineHi);
    }

    // ---------- Lesezeichen ----------

    private void DrawTabs(PixelBuffer b)
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            int index = i;
            bool selected = i == _active;
            int x = TabFirstX + i * TabStep;
            var area = new Rect(x, selected ? 0 : 5, TabW, TabH);
            bool hover = _ui.Button(area, () => SelectTab(index));
            DrawTab(b, _tabs[i], x, area.Y - (hover && !selected ? 1 : 0));
        }
    }

    private static void DrawTab(PixelBuffer b, BookTab tab, int x, int y)
    {
        uint fill = tab.Color;
        uint hi = ColorUtil.Lerp(fill, 0xFFFFFFFF, 0.35);
        uint lo = ColorUtil.Lerp(fill, 0xFF000000, 0.30);

        bool Inside(int i, int j)
        {
            if (i < 0 || j < 0 || i >= TabW || j >= TabH) return false;
            int n = j - (TabH - 6);                          // Einkerbung unten (Schwalbenschwanz)
            return n < 0 || Math.Abs(i - (TabW - 1) / 2.0) >= n + 0.5;
        }

        for (int j = -1; j <= TabH; j++)
        {
            for (int i = -1; i <= TabW; i++)
            {
                if (Inside(i, j))
                    b.Set(x + i, y + j, j == 0 || i == 0 ? hi : i == TabW - 1 ? lo : fill);
                else if (Inside(i - 1, j) || Inside(i + 1, j) || Inside(i, j - 1) || Inside(i, j + 1))
                    b.Set(x + i, y + j, CoverOutline);
            }
        }

        tab.Icon.Draw(b, x + (TabW - tab.Icon.Width) / 2, y + 7, Ink);
    }

    private void SelectTab(int index)
    {
        if (index == _active) return;
        _active = index;
        _tabs[_active].OnOpened();
    }

    // ---------- Schließen ----------

    private void DrawCloseButton(PixelBuffer b)
    {
        var font = _ui.Font;
        const string label = "Schließen";
        int w = font.Measure(label) + 16, h = font.LineHeight + 4;
        var rect = new Rect(RightPage.Right - w - 12, RightPage.Bottom - h - 10, w, h);

        bool hover = _ui.Button(rect, () => CloseRequested?.Invoke());
        b.FillRect(rect.X, rect.Y, rect.W, rect.H, hover ? ParchmentHi : ParchmentShade1);
        b.StrokeRect(rect.X, rect.Y, rect.W, rect.H, hover ? Cream : CardBorder);
        b.FillRect(rect.X + 1, rect.Bottom - 2, rect.W - 2, 1, hover ? ParchmentShade1 : ParchmentShade2);
        font.DrawCentered(b, label, rect.X + rect.W / 2, rect.Y + 2, Ink);
    }
}
