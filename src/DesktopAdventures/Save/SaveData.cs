namespace DesktopAdventures.Save;

/// <summary>Inhalt der Speicherdatei (JSON). Neue Felder dürfen jederzeit ergänzt werden; fehlende Felder bekommen Standardwerte.</summary>
public sealed class SaveData
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public List<string> UnlockedCharacters { get; set; } = new();
    public List<string> CompletedChallenges { get; set; } = new();
    public Dictionary<string, double> Stats { get; set; } = new();

    /// <summary>Vom Nutzer gewählte Position des Buchs (physische Bildschirm-Pixel).</summary>
    public int? BookX { get; set; }
    public int? BookY { get; set; }
}
