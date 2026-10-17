using static DesktopAdventures.Ui.BookTheme;

namespace DesktopAdventures.Ui;

/// <summary>Reiter "Gebäude". Vorerst leer: Gebäude gibt es noch nicht, die Seiten zeigen den Leerzustand.</summary>
public sealed class BuildingsTab : BookTab
{
    public override string Title => "Gebäude";
    public override uint Color => 0xFF7C9CE0;
    public override PixelIcon Icon => PixelIcon.House;

    public override void Draw(UiContext ui, Rect left, Rect right)
    {
        var b = ui.Buffer;
        var font = ui.Font;

        DrawHeader(ui, left, "Gebäude", "0/0", rightSide: false);
        DrawEmptyState(ui, left, "Noch keine Gebäude. Was du baust, erscheint hier.");

        var (x0, w) = ContentSpan(right, rightSide: true);
        int y = right.Y + 10;
        font.Draw(b, "Informationen", x0, y, Ink);
        b.FillRect(x0, y + font.LineHeight + 1, w, 1, InkFaint);
        var lines = font.Wrap("Wähle links ein Gebäude, um mehr über seine Bewohner und Nutzen zu erfahren.", w);
        int ty = y + font.LineHeight + 8;
        foreach (string line in lines)
        {
            font.Draw(b, line, x0, ty, InkSoft);
            ty += font.LineStep;
        }
    }

    private static void DrawHeader(UiContext ui, Rect page, string title, string counter, bool rightSide)
    {
        var (x0, w) = ContentSpan(page, rightSide);
        int y = page.Y + 10;
        ui.Font.Draw(ui.Buffer, title, x0, y, Ink);
        ui.Font.DrawRight(ui.Buffer, counter, x0 + w, y, InkSoft);
        ui.Buffer.FillRect(x0, y + ui.Font.LineHeight + 1, w, 1, InkFaint);
    }

    private static void DrawEmptyState(UiContext ui, Rect page, string message)
    {
        // Großes, blasses Haus-Symbol als Platzhalter
        int scale = 5;
        var (x0, w) = ContentSpan(page, rightSide: false);
        int iconX = x0 + (w - PixelIcon.House.Width * scale) / 2;
        int iconY = page.Y + 62;
        PixelIcon.House.Draw(ui.Buffer, iconX, iconY, InkFaint, scale);

        int y = iconY + PixelIcon.House.Height * scale + 14;
        foreach (string line in ui.Font.Wrap(message, w))
        {
            ui.Font.DrawCentered(ui.Buffer, line, x0 + w / 2, y, InkSoft);
            y += ui.Font.LineStep;
        }
    }
}
