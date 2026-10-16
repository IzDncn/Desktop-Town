namespace DesktopAdventures.Engine;

/// <summary>
/// Gemeinsame Farbpalette. Alle Grafik nutzt nur diese Farben, damit alles zusammenpasst.
/// Format 0xAARRGGBB.
/// </summary>
public static class Palette
{
    // Gras
    public const uint GrassHi = 0xFF9BD65A;
    public const uint GrassMid = 0xFF5FB23F;
    public const uint GrassLo = 0xFF3C8C3A;
    public const uint GrassDeep = 0xFF2B6B35;

    // Erde
    public const uint SoilHi = 0xFF8A5A33;
    public const uint SoilMid = 0xFF6B4226;
    public const uint SoilLo = 0xFF4E2F1C;
    public const uint SoilDeep = 0xFF3A2214;
    public const uint SoilEdge = 0xFF22130B;  // unterste Zeile: saubere Kante, wenn der Streifen über dem Rand schwebt

    // Stein
    public const uint StoneHi = 0xFFB8B2A7;
    public const uint StoneMid = 0xFF8C867F;
    public const uint StoneLo = 0xFF605B58;
    public const uint StoneOutline = 0xFF3F3B3A;

    // Pilz
    public const uint CapHi = 0xFFF06464;
    public const uint CapMid = 0xFFD43C3C;
    public const uint CapLo = 0xFFA02A38;
    public const uint CapOutline = 0xFF5E1B2B;
    public const uint MushroomStem = 0xFFE8DCC0;

    // Blumen
    public const uint FlowerRed = 0xFFE04B4B;
    public const uint FlowerYellow = 0xFFF5D547;
    public const uint FlowerWhite = 0xFFF4F1E6;
    public const uint FlowerBlue = 0xFF5B8EF0;
    public const uint FlowerPink = 0xFFEF7FB5;
    public const uint FlowerCenter = 0xFFF2A93B;
    public const uint Berry = 0xFFC8324B;
}
