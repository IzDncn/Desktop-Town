namespace DesktopAdventures;

/// <summary>Ein Sitzplatz am Feuer. FacesLeft spiegelt die Figur, damit sie zum Feuer schaut.</summary>
public readonly record struct Seat(double X, bool FacesLeft);

/// <summary>Feste Masse der Szene (in Bildschirm-Pixeln / DIPs).</summary>
public static class SceneLayout
{
    /// <summary>1 Sprite-Pixel = Scale Bildschirm-Pixel.</summary>
    public const int Scale = 5;

    public const double Width = 380;
    public const double Height = 240;

    /// <summary>Staerke des Holzrahmens in Sprite-Pixeln (muss zu Assets/backdrop.png passen).</summary>
    public const int FrameThickness = 3;

    /// <summary>Innenrand: so weit liegt der Spielbereich vom Fensterrand entfernt.</summary>
    public const double Inset = FrameThickness * Scale;

    /// <summary>Hoehe der Standlinie: hier stehen Fuesse der Figuren und der Fuss des Feuers.</summary>
    public const double FloorY = 210;

    /// <summary>Linke Kante des Feuers (Feuer ist 16 Sprite-Pixel breit).</summary>
    public const double FireX = 150;

    /// <summary>Sitzplaetze; in character.json per "seat" (Index) gewaehlt.</summary>
    public static readonly Seat[] Seats =
    {
        new(45, FacesLeft: false),   // links vom Feuer
        new(255, FacesLeft: true),   // rechts vom Feuer
    };
}
