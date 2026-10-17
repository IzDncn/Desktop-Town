namespace DesktopAdventures.Characters;

/// <summary>Eine auswählbare Aktion mit Gewicht und Wiederholungen.</summary>
public sealed record ActionChoice(string Animation, double Weight, int Repeat);

/// <summary>Anzeigetexte für das Buch (alle Felder sind immer gefüllt, ggf. mit Platzhaltern).</summary>
public sealed record CharacterInfo(string Title, string Description, IReadOnlyList<string> Facts, string UnlockHint);

/// <summary>Ein geladener, spielbereiter Charakter (Daten + fertiges Sprite-Sheet).</summary>
public sealed class CharacterProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required CharacterSheet Sheet { get; init; }
    public required CharacterInfo Info { get; init; }
    public double Speed { get; init; }
    public (double Min, double Max) IdleSeconds { get; init; }
    public (double Min, double Max) WalkSeconds { get; init; }
    public bool StartUnlocked { get; init; }
    public IReadOnlyList<ActionChoice> OnClick { get; init; } = Array.Empty<ActionChoice>();
    public IReadOnlyList<ActionChoice> IdleActions { get; init; } = Array.Empty<ActionChoice>();
    public double IdleActionChance { get; init; }
}
