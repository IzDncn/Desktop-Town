namespace DesktopAdventures;

/// <summary>Zentrale Konstanten. Später durch Einstellungen/Speicherstand ergänzt.</summary>
public static class GameConfig
{
    /// <summary>Kantenlänge eines Kunst-Pixels in DIPs (96-dpi-Pixel). Kleiner = feinere Grafik.</summary>
    public const double ArtPixelDips = 2.0;

    /// <summary>Höhe des Streifens in Kunst-Pixeln.</summary>
    public const int StripHeightArt = 120;

    /// <summary>Simulationsrate. Pixel-Art wirkt bei 30 fps flüssig genug und bleibt sparsam.</summary>
    public const int TargetFps = 30;

    /// <summary>Seed der Landschaft (später pro Speicherstand).</summary>
    public const int WorldSeed = 20261016;

    /// <summary>Abstand (DIPs) zum unteren Bildschirmrand, solange die Taskleiste automatisch ausgeblendet ist.
    /// Ohne diesen Spielraum kann sie nicht mehr einfahren. Bei Problemen größer wählen.</summary>
    public const double AutohideGapDips = 14;

    /// <summary>true: Der Streifen folgt einer ausgeblendeten Taskleiste beim Ein-/Ausfahren.
    /// false: Er bleibt dauerhaft über der vollen Taskleistenhöhe (robuste Notlösung, falls das Mitwandern
    /// auf einem System nicht funktioniert).</summary>
    public static readonly bool AutohideFollowsTaskbar = true;
}
