using DesktopAdventures.Characters;

namespace DesktopAdventures.Challenges;

/// <summary>Prüft, welche Challenges aktiv sind und ob sie geschafft wurden, und vergibt die Belohnungen.</summary>
public sealed class ChallengeManager
{
    private readonly GameState _state;

    public IReadOnlyList<Challenge> All { get; }

    /// <summary>Wird ausgelöst, nachdem eine Challenge abgeschlossen und ihre Belohnungen vergeben wurden.</summary>
    public event Action<Challenge>? Completed;

    public ChallengeManager(GameState state, IReadOnlyList<Challenge> challenges)
    {
        _state = state;
        All = challenges;
    }

    public bool IsCompleted(Challenge c) => _state.IsChallengeCompleted(c.Id);

    /// <summary>Aktiv = noch nicht geschafft und alle Voraussetzungen erfüllt.</summary>
    public bool IsActive(Challenge c) => !IsCompleted(c) && c.Requires.All(_state.IsChallengeCompleted);

    public IEnumerable<Challenge> Active => All.Where(IsActive);

    public IEnumerable<Challenge> Done => All.Where(IsCompleted);

    /// <summary>Aktueller Fortschritt, begrenzt auf das Ziel.</summary>
    public double Progress(Challenge c) => Math.Min(c.Target, _state.GetStat(c.Stat));

    /// <summary>Noch offene Challenge, die diesen Charakter freischaltet (für den Hinweis im Buch).</summary>
    public Challenge? FindUnlocking(CharacterProfile profile) =>
        All.FirstOrDefault(c => !IsCompleted(c) && c.Rewards.Any(r => r.Character == profile));

    /// <summary>Schließt erreichte Challenges ab. Abschlüsse können weitere Challenges aktivieren, daher wird wiederholt.</summary>
    public void Update()
    {
        for (int pass = 0; pass < 20; pass++)
        {
            var reached = Active.Where(c => _state.GetStat(c.Stat) >= c.Target).ToList();
            if (reached.Count == 0) return;
            foreach (var challenge in reached) Complete(challenge);
        }
    }

    private void Complete(Challenge challenge)
    {
        _state.MarkChallengeCompleted(challenge.Id);
        foreach (var reward in challenge.Rewards)
            if (reward.Character != null) _state.UnlockCharacter(reward.Character);
        Completed?.Invoke(challenge);
    }
}
