namespace DesktopAdventures.Shell;

/// <summary>
/// Rastermaße des Streifens. Ein Kunst-Pixel wird immer auf eine GANZZAHLIGE Anzahl
/// physischer Bildschirm-Pixel abgebildet (auch bei 125 % / 150 % Skalierung), damit
/// die Pixel-Art nie unscharf oder ungleichmäßig wird.
/// </summary>
public readonly record struct StripLayout(
    int PixelSize, int ArtWidth, int ArtHeight, int WindowWidthPx, int WindowHeightPx, double DpiScale)
{
    public static StripLayout Create(int availableWidthPx, double dpiScale)
    {
        int pixelSize = Math.Max(1, (int)Math.Round(GameConfig.ArtPixelDips * dpiScale));
        int artWidth = Math.Max(1, availableWidthPx / pixelSize);
        int artHeight = GameConfig.StripHeightArt;
        return new StripLayout(pixelSize, artWidth, artHeight, availableWidthPx, artHeight * pixelSize, dpiScale);
    }

    /// <summary>Größe der Bildfläche in DIPs, so dass sie genau ArtWidth*PixelSize physische Pixel belegt.</summary>
    public double ViewWidthDips => ArtWidth * PixelSize / DpiScale;
    public double ViewHeightDips => ArtHeight * PixelSize / DpiScale;
}
