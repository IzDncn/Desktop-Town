using System.IO;
using System.Text.Json;
using DesktopAdventures.Characters;

namespace DesktopAdventures.Challenges;

/// <summary>
/// Lädt alle Challenges aus Content/Challenges/*.json und prüft sie (Statistik bekannt? Belohnung existiert?
/// Voraussetzungen vorhanden?). Fehlerhafte Dateien werden übersprungen und in <see cref="Errors"/> gemeldet.
/// </summary>
public sealed class ChallengeCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly List<Challenge> _challenges = new();
    private readonly List<string> _errors = new();

    public IReadOnlyList<Challenge> Challenges => _challenges;
    public IReadOnlyList<string> Errors => _errors;

    private ChallengeCatalog() { }

    public static ChallengeCatalog Load(string contentRoot, CharacterCatalog characters)
    {
        var catalog = new ChallengeCatalog();
        catalog.LoadAll(Path.Combine(contentRoot, "Challenges"), characters);
        return catalog;
    }

    private void LoadAll(string directory, CharacterCatalog characters)
    {
        if (!Directory.Exists(directory)) return; // keine Challenges ist erlaubt

        var loaded = new List<Challenge>();
        foreach (string file in Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                loaded.Add(LoadOne(file, characters));
            }
            catch (Exception ex)
            {
                _errors.Add($"Challenges/{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        // Voraussetzungen prüfen (kann erst jetzt, wenn alle IDs bekannt sind)
        var ids = loaded.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var challenge in loaded)
        {
            string? missing = challenge.Requires.FirstOrDefault(r => !ids.Contains(r) || r.Equals(challenge.Id, StringComparison.OrdinalIgnoreCase));
            if (missing != null)
                _errors.Add($"Challenges/{challenge.Id}.json: Voraussetzung '{missing}' gibt es nicht (oder verweist auf sich selbst).");
            else
                _challenges.Add(challenge);
        }

        // Challenges, deren Voraussetzungen wegen Fehlern oben fehlen, wären nie erreichbar -> ebenfalls melden und entfernen
        bool removed;
        do
        {
            var valid = _challenges.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unreachable = _challenges.Where(c => c.Requires.Any(r => !valid.Contains(r))).ToList();
            removed = unreachable.Count > 0;
            foreach (var c in unreachable)
            {
                _errors.Add($"Challenges/{c.Id}.json: Eine Voraussetzung konnte nicht geladen werden, die Challenge wird übersprungen.");
                _challenges.Remove(c);
            }
        } while (removed);

        _challenges.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));
    }

    private static Challenge LoadOne(string file, CharacterCatalog characters)
    {
        ChallengeDefinition def;
        try
        {
            def = JsonSerializer.Deserialize<ChallengeDefinition>(File.ReadAllText(file), JsonOptions)
                  ?? throw new InvalidDataException("Datei ist leer.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Ungültiges JSON (Zeile {(ex.LineNumber ?? 0) + 1}): {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(def.Name)) throw new InvalidDataException("\"name\" fehlt.");
        if (!GameState.KnownStats.Contains(def.Goal.Stat, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Unbekannte Statistik '{def.Goal.Stat}' in goal.stat. Bekannt: {string.Join(", ", GameState.KnownStats)}.");
        if (def.Goal.Target <= 0) throw new InvalidDataException("goal.target muss größer als 0 sein.");

        var rewards = new List<Reward>();
        foreach (var r in def.Rewards)
        {
            if (!r.Type.Equals("unlockCharacter", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Unbekannter Belohnungstyp '{r.Type}' (möglich: unlockCharacter).");

            var profile = characters.Profiles.FirstOrDefault(p => p.Id.Equals(r.Id, StringComparison.OrdinalIgnoreCase))
                          ?? throw new InvalidDataException(
                              $"Belohnung: Charakter '{r.Id}' gibt es nicht. Vorhanden: {string.Join(", ", characters.Profiles.Select(p => p.Id))}.");
            rewards.Add(new Reward("unlockCharacter", profile));
        }

        return new Challenge
        {
            Id = Path.GetFileNameWithoutExtension(file),
            Name = def.Name,
            Description = def.Description,
            Order = def.Order,
            Stat = def.Goal.Stat.ToLowerInvariant(),
            Target = def.Goal.Target,
            Unit = def.Goal.Unit ?? "",
            Requires = def.Requires,
            Rewards = rewards,
        };
    }
}
