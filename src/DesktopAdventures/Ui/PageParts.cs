using DesktopAdventures.Characters;
using static DesktopAdventures.Ui.BookTheme;

namespace DesktopAdventures.Ui;

/// <summary>Wiederverwendbare Bausteine für Buchseiten.</summary>
public static class PageParts
{
    /// <summary>Überschrift mit optionalem Zähler rechts und Trennlinie. Liefert das y darunter (inkl. Abstand).</summary>
    public static int Header(UiContext ui, Rect page, bool rightSide, string title, string? counter = null, string? side = null)
    {
        var (x0, w) = ContentSpan(page, rightSide);
        var font = ui.Font;
        int y = page.Y + 10;

        font.Draw(ui.Buffer, title, x0, y, Ink);
        string? right = side ?? counter;
        if (right != null) font.DrawRight(ui.Buffer, right, x0 + w, y, InkSoft);

        y += font.LineHeight + 1;
        ui.Buffer.FillRect(x0, y, w, 1, InkFaint);
        return y + 7;
    }

    /// <summary>Zeichnet Zeilen untereinander; bei Überlänge wird die letzte Zeile mit „…“ gekürzt.</summary>
    public static void Lines(UiContext ui, List<string> lines, int x, int y, int width, int maxLines, uint color)
    {
        if (maxLines <= 0) return;
        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
            lines[^1] = ui.Font.Truncate(lines[^1] + "…", width);
        }
        foreach (string line in lines)
        {
            ui.Font.Draw(ui.Buffer, line, x, y, color);
            y += ui.Font.LineStep;
        }
    }

    public static void Paragraph(UiContext ui, string text, int x, int y, int width, int maxLines, uint color = Ink) =>
        Lines(ui, ui.Font.Wrap(text, width), x, y, width, maxLines, color);

    /// <summary>Blasses Symbol mit Text in der Seitenmitte, wenn es nichts anzuzeigen gibt.</summary>
    public static void EmptyState(UiContext ui, Rect page, bool rightSide, PixelIcon icon, string message)
    {
        var (x0, w) = ContentSpan(page, rightSide);
        const int scale = 4;
        int iconY = page.Y + 96;
        icon.Draw(ui.Buffer, x0 + (w - icon.Width * scale) / 2, iconY, InkFaint, scale);

        int y = iconY + icon.Height * scale + 12;
        foreach (string line in ui.Font.Wrap(message, w))
        {
            ui.Font.DrawCentered(ui.Buffer, line, x0 + w / 2, y, InkSoft);
            y += ui.Font.LineStep;
        }
    }

    /// <summary>Blätter-Leiste am unteren Seitenrand: Pfeil links, "1 / 3", Pfeil rechts.</summary>
    public static void Pager(UiContext ui, Rect page, int x0, int width, int current, int pages, Action<int> setPage)
    {
        var font = ui.Font;
        int y = page.Bottom - 10 - font.LineHeight;
        font.DrawCentered(ui.Buffer, $"{current + 1} / {pages}", x0 + width / 2, y, InkSoft);

        Arrow(ui, new Rect(x0 - 2, y, 14, font.LineHeight), left: true, enabled: current > 0, () => setPage(current - 1));
        Arrow(ui, new Rect(x0 + width - 12, y, 14, font.LineHeight), left: false, enabled: current < pages - 1, () => setPage(current + 1));
    }

    private static void Arrow(UiContext ui, Rect area, bool left, bool enabled, Action onClick)
    {
        bool hover = enabled && ui.Button(area, onClick);
        uint color = !enabled ? InkFaint : hover ? Cream : Ink;
        int x = area.X + 4, y = area.Y + (area.H - 9) / 2;
        for (int j = 0; j < 9; j++)
        {
            int len = (j <= 4 ? j : 8 - j) + 1;
            ui.Buffer.FillRect(left ? x + 5 - len : x, y + j, len, 1, color);
        }
    }

    /// <summary>
    /// Karte mit dem Idle-Bild eines Charakters (bei Hover animiert). revealed = false zeigt nur die Silhouette.
    /// Ohne onClick ist die Karte nicht anklickbar (z. B. als Belohnungsanzeige).
    /// </summary>
    public static void CharacterCard(UiContext ui, Rect rect, CharacterProfile profile, bool revealed, bool selected, Action? onClick = null)
    {
        var b = ui.Buffer;
        bool hover = onClick != null && ui.Button(rect, onClick);

        b.FillRect(rect.X, rect.Y, rect.W, rect.H, CardFill);
        b.StrokeRect(rect.X, rect.Y, rect.W, rect.H, selected || hover ? CardBorderHover : CardBorder);
        if (selected) b.StrokeRect(rect.X + 1, rect.Y + 1, rect.W - 2, rect.H - 2, CardBorderHover);

        var sheet = profile.Sheet;
        var idle = sheet.Get("idle");
        int frame = hover ? (int)(ui.Time * idle.Fps) % idle.Frames : 0;

        b.SetClip(rect.X + 2, rect.Y + 2, rect.W - 4, rect.H - 4);
        b.Draw(sheet.Image, frame * sheet.FrameWidth, idle.Row * sheet.FrameHeight, sheet.FrameWidth, sheet.FrameHeight,
            rect.X + (rect.W - sheet.FrameWidth) / 2, rect.Bottom - sheet.FrameHeight - 3,
            tint: revealed ? (uint?)null : Silhouette);
        b.ResetClip();
    }

    /// <summary>Fortschrittsbalken; fraction zwischen 0 und 1.</summary>
    public static void ProgressBar(UiContext ui, Rect rect, double fraction)
    {
        var b = ui.Buffer;
        b.FillRect(rect.X, rect.Y, rect.W, rect.H, PageOutline);
        b.FillRect(rect.X + 1, rect.Y + 1, rect.W - 2, rect.H - 2, ParchmentShade2);

        int filled = (int)Math.Round((rect.W - 2) * Math.Clamp(fraction, 0, 1));
        if (filled <= 0) return;
        b.FillRect(rect.X + 1, rect.Y + 1, filled, rect.H - 2, 0xFF5FB23F);
        b.FillRect(rect.X + 1, rect.Y + 1, filled, 1, 0xFF9BD65A);
    }
}
