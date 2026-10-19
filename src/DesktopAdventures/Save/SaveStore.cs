using System.IO;
using System.Text.Json;

namespace DesktopAdventures.Save;

/// <summary>Liest und schreibt den Spielstand (%APPDATA%\DesktopAdventures\save.json), atomar und fehlertolerant.</summary>
public sealed class SaveStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public string FilePath { get; }

    public SaveStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopAdventures", "save.json");
    }

    /// <summary>Lädt den Spielstand. Ist die Datei beschädigt, wird sie beiseitegelegt und ein neuer Spielstand begonnen.</summary>
    public SaveData Load(out string? warning)
    {
        warning = null;
        if (!File.Exists(FilePath)) return new SaveData();

        try
        {
            return JsonSerializer.Deserialize<SaveData>(File.ReadAllText(FilePath), JsonOptions) ?? new SaveData();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            string backup = Path.Combine(Path.GetDirectoryName(FilePath)!, $"save.defekt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            try { File.Move(FilePath, backup); }
            catch (Exception moveError) when (moveError is IOException or UnauthorizedAccessException) { backup = "(konnte nicht gesichert werden)"; }

            warning = $"Der Spielstand konnte nicht gelesen werden ({ex.Message}). Es wird neu begonnen; die alte Datei liegt hier: {backup}";
            return new SaveData();
        }
    }

    /// <summary>Schreibt erst in eine temporäre Datei und ersetzt dann die alte; ein Absturz mittendrin zerstört den Spielstand nie.</summary>
    public void Save(SaveData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, JsonOptions));
        File.Move(temp, FilePath, overwrite: true);
    }

    public void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }
}
