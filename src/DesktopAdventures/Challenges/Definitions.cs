namespace DesktopAdventures.Challenges;

// JSON-Modelle für Content/Challenges/*.json. Format: docs/CHALLENGES.md

public sealed class GoalDefinition
{
    /// <summary>Name einer Statistik, siehe GameState.KnownStats.</summary>
    public string Stat { get; set; } = "";
    public double Target { get; set; }
    /// <summary>Einheit für die Fortschrittsanzeige, z. B. "Minuten" (optional).</summary>
    public string? Unit { get; set; }
}

public sealed class RewardDefinition
{
    /// <summary>Aktuell: "unlockCharacter".</summary>
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
}

public sealed class ChallengeDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Sortierung in der Liste (kleiner = weiter oben).</summary>
    public int Order { get; set; }
    public GoalDefinition Goal { get; set; } = new();
    /// <summary>IDs von Challenges, die zuerst abgeschlossen sein müssen, bevor diese aktiv wird.</summary>
    public List<string> Requires { get; set; } = new();
    public List<RewardDefinition> Rewards { get; set; } = new();
}
