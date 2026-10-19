using DesktopAdventures.Characters;

namespace DesktopAdventures.Challenges;

/// <summary>Belohnung (geprüft und aufgelöst). Character ist bei Typ unlockCharacter gesetzt.</summary>
public sealed record Reward(string Type, CharacterProfile? Character);

/// <summary>Eine geladene, geprüfte Challenge.</summary>
public sealed class Challenge
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int Order { get; init; }
    public required string Stat { get; init; }
    public double Target { get; init; }
    public string Unit { get; init; } = "";
    public IReadOnlyList<string> Requires { get; init; } = Array.Empty<string>();
    public IReadOnlyList<Reward> Rewards { get; init; } = Array.Empty<Reward>();
}
