namespace DesktopAdventures.Characters;

/// <summary>
/// Marker-Farben in den Template-Sprites. Jede Farbfamilie hat drei Töne (hell, mittel, dunkel).
/// Im Spiel werden sie pro Charakter durch die Palette ersetzt. Muss zu tools/make_sprites.py passen.
/// </summary>
public static class SlotMarkers
{
    private static readonly Dictionary<string, (uint Hi, uint Mid, uint Lo)> Families =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["red"] = (0xFFFF9999, 0xFFFF5555, 0xFFAA2222),
            ["green"] = (0xFF99FF99, 0xFF55FF55, 0xFF22AA22),
            ["blue"] = (0xFF9999FF, 0xFF5555FF, 0xFF2222AA),
            ["yellow"] = (0xFFFFFF99, 0xFFFFFF55, 0xFFAAAA22),
            ["magenta"] = (0xFFFF99FF, 0xFFFF55FF, 0xFFAA22AA),
            ["cyan"] = (0xFF99FFFF, 0xFF55FFFF, 0xFF22AAAA),
        };

    private static readonly Dictionary<uint, (string Family, int Shade)> Lookup = BuildLookup();

    public static IEnumerable<string> FamilyNames => Families.Keys;

    public static bool IsFamily(string name) => Families.ContainsKey(name);

    /// <summary>Erkennt eine Marker-Farbe. Shade: 0 = hell, 1 = mittel, 2 = dunkel.</summary>
    public static bool TryGet(uint argb, out string family, out int shade)
    {
        if (Lookup.TryGetValue(argb, out var hit))
        {
            (family, shade) = hit;
            return true;
        }
        family = "";
        shade = 0;
        return false;
    }

    private static Dictionary<uint, (string, int)> BuildLookup()
    {
        var map = new Dictionary<uint, (string, int)>();
        foreach (var (name, tones) in Families)
        {
            string key = name.ToLowerInvariant();
            map[tones.Hi] = (key, 0);
            map[tones.Mid] = (key, 1);
            map[tones.Lo] = (key, 2);
        }
        return map;
    }
}
