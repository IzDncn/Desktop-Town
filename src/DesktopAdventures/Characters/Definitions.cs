using System.Text.Json;

namespace DesktopAdventures.Characters;

// JSON-Modelle. Dateiformat und Beispiele: docs/CHARAKTERE.md

public sealed class FrameSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class AnimationDefinition
{
    /// <summary>Zeile im Sprite-Sheet (0 = oberste).</summary>
    public int Row { get; set; }
    public int Frames { get; set; } = 1;
    public double Fps { get; set; } = 6;
    public bool Loop { get; set; } = true;
}

public sealed class SlotDefinition
{
    /// <summary>Marker-Farbfamilie im Sprite: red, green, blue, yellow, magenta oder cyan.</summary>
    public string Marker { get; set; } = "";
    /// <summary>Standardfarbe, wenn der Charakter keine eigene angibt.</summary>
    public string Default { get; set; } = "#ff00ff";
}

/// <summary>Content/Templates/&lt;name&gt;/template.json – ein Körperbau samt Animationen, Farb-Slots und Ebenen.</summary>
public sealed class TemplateDefinition
{
    public FrameSize Frame { get; set; } = new();
    public string Sheet { get; set; } = "sheet.png";
    public Dictionary<string, AnimationDefinition> Animations { get; set; } = new();
    public Dictionary<string, SlotDefinition> Slots { get; set; } = new();
    /// <summary>Optionale Zusatz-Ebenen (Hüte, Bart ...): Name → PNG relativ zum Template-Ordner.</summary>
    public Dictionary<string, string> Layers { get; set; } = new();
}

/// <summary>Content/Characters/&lt;id&gt;.json – ein konkreter Charakter.</summary>
public sealed class CharacterDefinition
{
    public string Name { get; set; } = "";
    public string Template { get; set; } = "";

    /// <summary>Slot-Name → Farbe ("#rrggbb") oder drei Farben [hell, mittel, dunkel].</summary>
    public Dictionary<string, JsonElement>? Palette { get; set; }

    /// <summary>Ebenen des Templates, in Zeichenreihenfolge (die letzte liegt oben).</summary>
    public List<string>? Layers { get; set; }

    /// <summary>Lauftempo in Kunst-Pixeln pro Sekunde.</summary>
    public double Speed { get; set; } = 12;

    /// <summary>[min, max] Sekunden, die der Charakter steht bzw. läuft.</summary>
    public double[] IdleSeconds { get; set; } = new double[] { 2, 6 };
    public double[] WalkSeconds { get; set; } = new double[] { 2, 7 };

    /// <summary>Ist der Charakter schon zu Spielbeginn da? (Später: Freischaltung per Challenge.)</summary>
    public bool StartUnlocked { get; set; }
}
