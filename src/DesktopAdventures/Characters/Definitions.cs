using System.Text.Json;
using System.Text.Json.Serialization;

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

/// <summary>
/// Eine Aktion, die ein Charakter abspielen kann. In JSON entweder nur der Animationsname ("react")
/// oder ein Objekt: { "animation": "cheer", "weight": 2, "repeat": 3 }.
/// </summary>
[JsonConverter(typeof(ActionDefinitionConverter))]
public sealed class ActionDefinition
{
    public string Animation { get; set; } = "";
    /// <summary>Relative Auswahlwahrscheinlichkeit gegenüber den anderen Aktionen der Liste.</summary>
    public double Weight { get; set; } = 1;
    /// <summary>Wie oft die Animation hintereinander abgespielt wird.</summary>
    public int Repeat { get; set; } = 1;
}

public sealed class ActionDefinitionConverter : JsonConverter<ActionDefinition>
{
    public override ActionDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return new ActionDefinition { Animation = reader.GetString() ?? "" };

        using var doc = JsonDocument.ParseValue(ref reader);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("Eine Aktion ist ein Animationsname oder ein Objekt { \"animation\": ... }.");

        var action = new ActionDefinition();
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            switch (property.Name.ToLowerInvariant())
            {
                case "animation": action.Animation = property.Value.GetString() ?? ""; break;
                case "weight": action.Weight = property.Value.GetDouble(); break;
                case "repeat": action.Repeat = property.Value.GetInt32(); break;
                default:
                    throw new JsonException($"Unbekanntes Feld '{property.Name}' in einer Aktion (erlaubt: animation, weight, repeat).");
            }
        }
        return action;
    }

    public override void Write(Utf8JsonWriter writer, ActionDefinition value, JsonSerializerOptions options) =>
        throw new NotSupportedException();
}

/// <summary>Was ein Charakter bei Klick oder im Stehen tut. Nicht gesetzte Felder erbt er vom Template.</summary>
public sealed class BehaviorDefinition
{
    /// <summary>Beim Anklicken wird eine dieser Aktionen (gewichtet zufällig) abgespielt.</summary>
    public List<ActionDefinition>? OnClick { get; set; }

    /// <summary>Aktionen, die der Charakter gelegentlich von selbst macht, bevor er weiterläuft.</summary>
    public List<ActionDefinition>? IdleActions { get; set; }

    /// <summary>Wahrscheinlichkeit (0..1), dass nach dem Stehen eine Idle-Aktion statt Laufen kommt.</summary>
    public double? IdleActionChance { get; set; }
}

/// <summary>Zusätzliche Animationen nur für diesen einen Charakter (eigenes Sheet, gleiche Bildgröße wie das Template).</summary>
public sealed class ExtraAnimationsDefinition
{
    /// <summary>PNG relativ zu Content/Characters/.</summary>
    public string Sheet { get; set; } = "";
    public Dictionary<string, AnimationDefinition> Animations { get; set; } = new();
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

    /// <summary>Standardverhalten aller Charaktere dieses Templates.</summary>
    public BehaviorDefinition? Behavior { get; set; }
}

/// <summary>Texte, die das Buch über einen Charakter anzeigt.</summary>
public sealed class InfoDefinition
{
    /// <summary>Kurze Bezeichnung unter dem Namen, z. B. "Barbar".</summary>
    public string? Title { get; set; }

    /// <summary>Beschreibung (Fließtext, \n = neue Zeile). Richtwert: bis ca. 150 Zeichen.</summary>
    public string? Description { get; set; }

    /// <summary>Kurze Zeilen neben dem Vorschaubild, z. B. "Lieblingsessen: Beeren".</summary>
    public List<string>? Facts { get; set; }

    /// <summary>Text, solange der Charakter noch nicht freigeschaltet ist.</summary>
    public string? UnlockHint { get; set; }
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

    /// <summary>Infos für das Buch (Titel, Beschreibung, Fakten, Freischalt-Hinweis).</summary>
    public InfoDefinition? Info { get; set; }

    /// <summary>Überschreibt das Verhalten des Templates (nur die angegebenen Felder).</summary>
    public BehaviorDefinition? Behavior { get; set; }

    /// <summary>Animationen, die nur dieser Charakter hat.</summary>
    public ExtraAnimationsDefinition? Extra { get; set; }

    /// <summary>Lauftempo in Kunst-Pixeln pro Sekunde.</summary>
    public double Speed { get; set; } = 12;

    /// <summary>[min, max] Sekunden, die der Charakter steht bzw. läuft.</summary>
    public double[] IdleSeconds { get; set; } = new double[] { 2, 6 };
    public double[] WalkSeconds { get; set; } = new double[] { 2, 7 };

    /// <summary>Ist der Charakter schon zu Spielbeginn da? (Später: Freischaltung per Challenge.)</summary>
    public bool StartUnlocked { get; set; }
}
