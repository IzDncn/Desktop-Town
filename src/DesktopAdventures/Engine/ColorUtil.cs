using System.Globalization;

namespace DesktopAdventures.Engine;

public static class ColorUtil
{
    /// <summary>Liest "#rrggbb" oder "rrggbb" und liefert 0xFFRRGGBB.</summary>
    public static uint Parse(string text)
    {
        string hex = text.Trim().TrimStart('#');
        if (hex.Length != 6 || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
            throw new FormatException($"Ungültige Farbe '{text}' (erwartet wird z. B. \"#e8b48a\").");
        return 0xFF000000u | rgb;
    }

    /// <summary>Mischt zwei deckende Farben; t = 0 ergibt a, t = 1 ergibt b.</summary>
    public static uint Lerp(uint a, uint b, double t)
    {
        static uint Channel(uint a, uint b, int shift, double t) =>
            (uint)Math.Round(((a >> shift) & 0xFF) * (1 - t) + ((b >> shift) & 0xFF) * t);

        return 0xFF000000u | Channel(a, b, 16, t) << 16 | Channel(a, b, 8, t) << 8 | Channel(a, b, 0, t);
    }
}
