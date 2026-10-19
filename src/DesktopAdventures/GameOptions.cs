namespace DesktopAdventures;

/// <summary>Start-Optionen (Kommandozeile). Hauptsächlich zum Entwickeln und Testen.</summary>
public static class GameOptions
{
    /// <summary>--all-characters: alle Charaktere erscheinen sofort (zum Ansehen eigener Designs).</summary>
    public static bool AllCharacters { get; private set; }

    /// <summary>--debug-taskbar: schreibt Messwerte zur Taskleiste nach %TEMP%\desktop-adventures-taskbar.log.</summary>
    public static bool DebugTaskbar { get; private set; }

    /// <summary>--reset-save: löscht den Spielstand beim Start.</summary>
    public static bool ResetSave { get; private set; }

    public static void Parse(string[] args)
    {
        ResetSave = args.Contains("--reset-save", StringComparer.OrdinalIgnoreCase);
        DebugTaskbar = args.Contains("--debug-taskbar", StringComparer.OrdinalIgnoreCase);
        AllCharacters = args.Contains("--all-characters", StringComparer.OrdinalIgnoreCase);
    }
}
