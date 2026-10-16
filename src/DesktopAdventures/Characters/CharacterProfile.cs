namespace DesktopAdventures.Characters;

/// <summary>Ein geladener, spielbereiter Charakter (Daten + fertiges Sprite-Sheet).</summary>
public sealed class CharacterProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required CharacterSheet Sheet { get; init; }
    public double Speed { get; init; }
    public (double Min, double Max) IdleSeconds { get; init; }
    public (double Min, double Max) WalkSeconds { get; init; }
    public bool StartUnlocked { get; init; }
}
