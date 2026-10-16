using System.IO;
using System.Text.Json;

namespace DesktopAdventures.Data;

/// <summary>
/// Liest alle Charaktere aus Characters/&lt;id&gt;/character.json.
/// Fehlerhafte Charaktere werden uebersprungen und als Meldung zurueckgegeben,
/// damit ein Tippfehler in einer JSON nicht das ganze Spiel verhindert.
/// </summary>
public static class CharacterLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip, // // Kommentare in der JSON sind erlaubt
        AllowTrailingCommas = true
    };

    public static CharacterLoadResult LoadAll(string rootDir, int seatCount)
    {
        var characters = new List<LoadedCharacter>();
        var errors = new List<string>();

        if (!Directory.Exists(rootDir))
        {
            errors.Add($"Charakter-Ordner nicht gefunden: {rootDir}");
            return new CharacterLoadResult(characters, errors);
        }

        var usedSeats = new Dictionary<int, string>();

        foreach (var dir in Directory.GetDirectories(rootDir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var jsonPath = Path.Combine(dir, "character.json");
            if (!File.Exists(jsonPath)) continue; // Ordner ohne character.json ignorieren

            var id = Path.GetFileName(dir);
            try
            {
                var def = JsonSerializer.Deserialize<CharacterDefinition>(File.ReadAllText(jsonPath), Options)
                          ?? throw new InvalidDataException("character.json ist leer.");

                Validate(def, dir, id, seatCount, usedSeats);
                usedSeats[def.Seat] = id;
                characters.Add(new LoadedCharacter(id, dir, def));
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or IOException)
            {
                errors.Add($"[{id}] {ex.Message}");
            }
        }

        return new CharacterLoadResult(characters, errors);
    }

    /// <summary>Loest einen relativen Pfad aus der JSON gegen den Charakter-Ordner auf.</summary>
    public static string ResolvePath(string folder, string relativePath)
        => Path.GetFullPath(Path.Combine(folder, relativePath));

    private static void Validate(CharacterDefinition d, string dir, string id, int seatCount,
                                 Dictionary<int, string> usedSeats)
    {
        if (string.IsNullOrWhiteSpace(d.Name)) d.Name = id;

        if (d.Seat < 0 || d.Seat >= seatCount)
            throw new InvalidDataException($"seat {d.Seat} gibt es nicht (erlaubt: 0 bis {seatCount - 1}).");
        if (usedSeats.TryGetValue(d.Seat, out var other))
            throw new InvalidDataException($"seat {d.Seat} ist schon von '{other}' belegt.");

        if (d.FrameWidth <= 0 || d.FrameHeight <= 0)
            throw new InvalidDataException("frameWidth und frameHeight muessen groesser als 0 sein.");
        if (d.IdleFrameMs <= 0 || d.ReactFrameMs <= 0)
            throw new InvalidDataException("idleFrameMs und reactFrameMs muessen groesser als 0 sein.");

        if (d.Outfits.Count == 0)
            throw new InvalidDataException("Es ist kein Outfit unter \"outfits\" definiert.");
        if (!d.Outfits.ContainsKey(d.Outfit))
            throw new InvalidDataException($"outfit \"{d.Outfit}\" ist nicht unter \"outfits\" definiert.");

        foreach (var (name, outfit) in d.Outfits)
        {
            CheckFile(dir, name, "idle", outfit.Idle);
            CheckFile(dir, name, "react", outfit.React);
        }
    }

    private static void CheckFile(string dir, string outfit, string animation, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new InvalidDataException($"Outfit \"{outfit}\": Pfad fuer \"{animation}\" fehlt.");
        if (!File.Exists(ResolvePath(dir, relativePath)))
            throw new InvalidDataException($"Outfit \"{outfit}\": Datei nicht gefunden: {relativePath}");
    }
}
