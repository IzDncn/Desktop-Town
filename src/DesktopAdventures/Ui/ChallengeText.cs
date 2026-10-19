using System.Globalization;
using DesktopAdventures.Challenges;

namespace DesktopAdventures.Ui;

/// <summary>Textdarstellung von Challenge-Zahlen.</summary>
public static class ChallengeText
{
    public static string Number(double value) =>
        value % 1 == 0 ? ((long)value).ToString(CultureInfo.InvariantCulture) : value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>"3 / 10" oder "3 / 10 Minuten". Der aktuelle Wert wird abgerundet, damit nie "5 / 5" erscheint, bevor es geschafft ist.</summary>
    public static string Progress(Challenge c, double current, bool withUnit = true)
    {
        string text = $"{Number(Math.Floor(Math.Min(current, c.Target)))} / {Number(c.Target)}";
        return withUnit && c.Unit.Length > 0 ? $"{text} {c.Unit}" : text;
    }
}
