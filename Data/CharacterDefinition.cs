namespace DesktopAdventures.Data;

/// <summary>Inhalt einer character.json. Alle Pfade sind relativ zum Charakter-Ordner.</summary>
public sealed class CharacterDefinition
{
    /// <summary>Anzeigename. Fehlt er, wird der Ordnername genommen.</summary>
    public string Name { get; set; } = "";

    /// <summary>Sitzplatz-Index (siehe SceneLayout.Seats).</summary>
    public int Seat { get; set; }

    /// <summary>Aktives Outfit (Schluessel aus "outfits").</summary>
    public string Outfit { get; set; } = "default";

    public int FrameWidth { get; set; } = 16;
    public int FrameHeight { get; set; } = 24;
    public int IdleFrameMs { get; set; } = 400;
    public int ReactFrameMs { get; set; } = 150;

    public Dictionary<string, OutfitDefinition> Outfits { get; set; } = new();
    public LinesDefinition Lines { get; set; } = new();
}

/// <summary>Ein Outfit = ein Satz Sprite-Sheets (ein Frame neben dem anderen).</summary>
public sealed class OutfitDefinition
{
    public string Idle { get; set; } = "";
    public string React { get; set; } = "";
}

/// <summary>Alle Sprechtexte des Charakters.</summary>
public sealed class LinesDefinition
{
    /// <summary>Wenn man den Charakter anklickt.</summary>
    public string[] Click { get; set; } = Array.Empty<string>();

    /// <summary>Gelegentliches Selbstgespraech am Feuer.</summary>
    public string[] Idle { get; set; } = Array.Empty<string>();
}

public sealed record LoadedCharacter(string Id, string Folder, CharacterDefinition Definition);

public sealed record CharacterLoadResult(IReadOnlyList<LoadedCharacter> Characters, IReadOnlyList<string> Errors);
