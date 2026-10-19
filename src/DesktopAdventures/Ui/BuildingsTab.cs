using static DesktopAdventures.Ui.BookTheme;

namespace DesktopAdventures.Ui;

/// <summary>
/// Reiter "Gebäude". Untermenü: Gebaut / Noch offen. Vorerst leer: Gebäude gibt es noch nicht, die Seiten zeigen den Leerzustand.
/// </summary>
public sealed class BuildingsTab : BookTab
{
    private static readonly string[] SubNames = { "Gebaut", "Noch offen" };

    private int _sub;

    public override string Title => "Gebäude";
    public override uint Color => 0xFF7C9CE0;
    public override PixelIcon Icon => PixelIcon.House;

    public override void Draw(UiContext ui, Rect left, Rect right)
    {
        var (x0, w) = ContentSpan(left, rightSide: false);
        int y = PageParts.Header(ui, left, false, "Gebäude", "0");
        SubTabBar.Draw(ui, x0, y, SubNames, _sub, i => _sub = i);

        PageParts.EmptyState(ui, left, false, PixelIcon.House,
            _sub == 0 ? "Du hast noch nichts gebaut." : "Hier erscheinen Gebäude, die du noch bauen oder freischalten kannst.");

        var (rx, rw) = ContentSpan(right, rightSide: true);
        int ry = PageParts.Header(ui, right, true, "Informationen");
        PageParts.Paragraph(ui, "Wähle links ein Gebäude, um mehr über seine Bewohner und seinen Nutzen zu erfahren.", rx, ry, rw, 6, InkSoft);
    }
}
