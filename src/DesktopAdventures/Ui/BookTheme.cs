namespace DesktopAdventures.Ui;

/// <summary>Maße (Kunst-Pixel) und Farben des Buchs.</summary>
public static class BookTheme
{
    // Buchkörper und Seiten (die Gesamtgröße ergibt sich daraus)
    public const int BodyTop = 20;
    public const int PageY = 32, PageW = 232, PageH = 256;
    public const int LeftX = 14, SpineW = 12;
    public const int SpineX = LeftX + PageW;
    public const int RightX = SpineX + SpineW;

    // Gesamtgröße des Popups
    public const int Width = RightX + PageW + 14;
    public const int Height = PageY + PageH + 10;

    // Lesezeichen (Tabs)
    public const int TabW = 30, TabH = 28, TabStep = 34, TabFirstX = 34;

    /// <summary>Innenbereich einer Seite: außen 12 px Rand, zur Buchmitte 20 px (dort liegt der Seitenschatten).</summary>
    public static (int X, int Width) ContentSpan(Rect page, bool rightSide) =>
        rightSide ? (page.X + 20, page.W - 32) : (page.X + 12, page.W - 32);

    public static readonly Rect LeftPage = new(LeftX, PageY, PageW, PageH);
    public static readonly Rect RightPage = new(RightX, PageY, PageW, PageH);

    // Einband
    public const uint CoverOutline = 0xFF2A0E12, CoverLo = 0xFF4A1A20, CoverMid = 0xFF6B2A2E, CoverHi = 0xFF8A3C40;
    public const uint Metal = 0xFF9A9AA4, MetalDark = 0xFF55555E;
    public const uint Spine = 0xFF33161A, SpineHi = 0xFF55262B;

    // Papier
    public const uint PageOutline = 0xFF6E4220, Parchment = 0xFFCD9B5A, ParchmentHi = 0xFFDDB070;
    public const uint ParchmentShade1 = 0xFFBB854B, ParchmentShade2 = 0xFFA9743F, PageBorder = 0xFFB07C42;
    public const uint PageEdgeLight = 0xFFE2C089, PageEdgeDark = 0xFF9B6A35;

    // Text
    public const uint Ink = 0xFF3A2314, InkSoft = 0xFF7A4E2A, InkFaint = 0xFFA8794A, Cream = 0xFFF6E7C4;

    // Karten und Vorschau
    public const uint CardFill = 0xFFB98348, CardBorder = 0xFF8E5A2E, CardBorderHover = 0xFFE9C98C;
    public const uint Silhouette = 0xFF4A3550;
    public const uint Sky = 0xFFA8D0E0, SkyLow = 0xFFC8E2E6;
}
