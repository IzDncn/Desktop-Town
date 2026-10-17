using DesktopAdventures.Characters;

namespace DesktopAdventures;

/// <summary>
/// Spielstand: was ist freigeschaltet? Wird von Welt und Buch gemeinsam benutzt.
/// (Speichern/Laden und Freischaltung durch Challenges folgen in Etappe 4.)
/// </summary>
public sealed class GameState
{
    private readonly HashSet<string> _unlockedCharacters = new(StringComparer.OrdinalIgnoreCase);

    public event Action? Changed;

    public GameState(CharacterCatalog catalog, bool unlockAll)
    {
        foreach (var profile in catalog.Profiles.Where(p => unlockAll || p.StartUnlocked))
            _unlockedCharacters.Add(profile.Id);
    }

    public bool IsUnlocked(CharacterProfile profile) => _unlockedCharacters.Contains(profile.Id);

    public int UnlockedCount => _unlockedCharacters.Count;

    public void Unlock(CharacterProfile profile)
    {
        if (_unlockedCharacters.Add(profile.Id)) Changed?.Invoke();
    }
}
