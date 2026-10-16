namespace DesktopAdventures.World;

/// <summary>Wind als Funktion von Ort und Zeit: wandernde Wellen mit langsam wechselnder Böenstärke.</summary>
public static class Wind
{
    /// <summary>Auslenkung in [-1, 1].</summary>
    public static double At(double x, double timeSec)
    {
        double gust = 0.65 + 0.35 * Math.Sin(timeSec * 0.23);
        double wave = Math.Sin(x * 0.035 - timeSec * 1.7) * 0.7
                    + Math.Sin(x * 0.09 - timeSec * 3.1) * 0.3;
        return wave * gust;
    }
}
