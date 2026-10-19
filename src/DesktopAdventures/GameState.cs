using System.IO;
using DesktopAdventures.Characters;
using DesktopAdventures.Save;

namespace DesktopAdventures;

/// <summary>
/// Spielstand: freigeschaltete Charaktere, abgeschlossene Challenges, Statistiken (Zähler), Buch-Position.
/// Wird von Welt, Buch und Challenge-System gemeinsam benutzt und regelmäßig gespeichert.
/// </summary>
public sealed class GameState
{
    /// <summary>
    /// Statistiken, auf die Challenges zielen dürfen. Neue Statistik: hier eintragen und an der passenden Stelle im Code
    /// <see cref="AddStat"/> aufrufen (siehe docs/CHALLENGES.md).
    /// </summary>
    public static readonly string[] KnownStats =
    {
        "playtime_minutes",     // gespielte Minuten (abgeleitet aus playtime_seconds)
        "playtime_seconds",     // gespielte Sekunden
        "character_clicks",     // Klicks auf Charaktere in der Landschaft
        "book_opened",          // wie oft das Buch geöffnet wurde
        "characters_unlocked",  // Anzahl freigeschalteter Charaktere (abgeleitet)
        "challenges_completed", // Anzahl abgeschlossener Challenges (abgeleitet)
    };

    private readonly CharacterCatalog _catalog;
    private readonly SaveStore? _store;
    private readonly HashSet<string> _unlocked = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _completed;
    private readonly Dictionary<string, double> _stats;
    private bool _dirty;
    private (int X, int Y)? _bookPosition;

    /// <summary>Wird ausgelöst, wenn ein Charakter neu freigeschaltet wurde.</summary>
    public event Action<CharacterProfile>? CharacterUnlocked;

    /// <summary>true (Entwickler-Option --all-characters): alles freigeschaltet, nichts wird gespeichert.</summary>
    public bool UnlockAll { get; }

    /// <param name="store">null = nichts speichern.</param>
    public GameState(CharacterCatalog catalog, SaveData data, SaveStore? store, bool unlockAll)
    {
        _catalog = catalog;
        _store = unlockAll ? null : store;
        UnlockAll = unlockAll;

        var saved = new HashSet<string>(data.UnlockedCharacters, StringComparer.OrdinalIgnoreCase);
        foreach (var profile in catalog.Profiles.Where(p => p.StartUnlocked || saved.Contains(p.Id)))
            _unlocked.Add(profile.Id);

        _completed = new HashSet<string>(data.CompletedChallenges, StringComparer.OrdinalIgnoreCase);
        _stats = new Dictionary<string, double>(data.Stats, StringComparer.OrdinalIgnoreCase);
        if (data.BookX is { } x && data.BookY is { } y) _bookPosition = (x, y);
    }

    // ---------- Charaktere ----------

    public bool IsUnlocked(CharacterProfile profile) => UnlockAll || _unlocked.Contains(profile.Id);

    public int UnlockedCount => UnlockAll ? _catalog.Profiles.Count : _unlocked.Count;

    public void UnlockCharacter(CharacterProfile profile)
    {
        if (!_unlocked.Add(profile.Id)) return;
        _dirty = true;
        CharacterUnlocked?.Invoke(profile);
    }

    // ---------- Challenges ----------

    public bool IsChallengeCompleted(string id) => _completed.Contains(id);

    public int CompletedChallengeCount => _completed.Count;

    public void MarkChallengeCompleted(string id)
    {
        if (_completed.Add(id)) _dirty = true;
    }

    // ---------- Statistiken ----------

    public double GetStat(string name) => name.ToLowerInvariant() switch
    {
        "playtime_minutes" => Raw("playtime_seconds") / 60.0,
        "characters_unlocked" => UnlockedCount,
        "challenges_completed" => _completed.Count,
        _ => Raw(name),
    };

    public void AddStat(string name, double amount = 1)
    {
        _stats[name] = Raw(name) + amount;
        _dirty = true;
    }

    private double Raw(string name) => _stats.TryGetValue(name, out double value) ? value : 0;

    // ---------- Buch ----------

    public (int X, int Y)? BookPosition
    {
        get => _bookPosition;
        set
        {
            if (_bookPosition == value) return;
            _bookPosition = value;
            _dirty = true;
        }
    }

    // ---------- Speichern ----------

    public void SaveIfDirty()
    {
        if (_dirty) SaveNow();
    }

    public void SaveNow()
    {
        if (_store == null) return;

        var data = new SaveData
        {
            UnlockedCharacters = _unlocked.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList(),
            CompletedChallenges = _completed.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList(),
            Stats = new Dictionary<string, double>(_stats),
            BookX = _bookPosition?.X,
            BookY = _bookPosition?.Y,
        };

        try
        {
            _store.Save(data);
            _dirty = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nicht speicherbar (z. B. Datei gesperrt): beim nächsten Intervall erneut versuchen.
            System.Diagnostics.Debug.WriteLine($"Speichern fehlgeschlagen: {ex.Message}");
        }
    }
}
