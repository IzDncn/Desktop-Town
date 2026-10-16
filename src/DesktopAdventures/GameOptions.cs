namespace DesktopAdventures;

/// <summary>Start-Optionen (Kommandozeile). Hauptsächlich zum Entwickeln und Testen.</summary>
public static class GameOptions
{
    /// <summary>--all-characters: alle Charaktere erscheinen sofort (zum Ansehen eigener Designs).</summary>
    public static bool AllCharacters { get; private set; }

    public static void Parse(string[] args)
    {
        AllCharacters = args.Contains("--all-characters", StringComparer.OrdinalIgnoreCase);
    }
}
