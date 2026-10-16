using DesktopAdventures.Data;

namespace DesktopAdventures;

public enum CharacterState
{
    Idle,
    Reacting
}

/// <summary>Die beiden Animationen eines Outfits.</summary>
public sealed record OutfitAnimations(Animation Idle, Animation React);

/// <summary>
/// Ein Charakter am Lagerfeuer: Zustandsmaschine (Idle / Reacting), Outfit-Wechsel
/// und Sprechtexte aus der character.json. Kennt keine UI - die kommt ueber Events.
/// </summary>
public sealed class Character
{
    private readonly AnimatedSprite _sprite;
    private readonly Dictionary<string, OutfitAnimations> _outfits;
    private readonly Random _rng = new();
    private OutfitAnimations _current;
    private double _chatterInMs;

    public CharacterDefinition Definition { get; }
    public string Name => Definition.Name;
    public string OutfitName { get; private set; }
    public CharacterState State { get; private set; } = CharacterState.Idle;

    /// <summary>Wird ausgeloest, wenn der Charakter etwas sagt (Sprechblase).</summary>
    public event Action<string>? Said;

    public Character(CharacterDefinition definition, AnimatedSprite sprite,
                     Dictionary<string, OutfitAnimations> outfits)
    {
        Definition = definition;
        _sprite = sprite;
        _outfits = outfits;

        OutfitName = definition.Outfit;
        _current = outfits[OutfitName];

        _sprite.Finished += OnReactFinished;
        _sprite.Play(_current.Idle);
        ScheduleChatter();
    }

    /// <summary>Wechselt das Outfit (z. B. spaeter durch Freischaltungen). Unbekannte Namen werden ignoriert.</summary>
    public void SetOutfit(string name)
    {
        if (!_outfits.TryGetValue(name, out var outfit)) return;

        OutfitName = name;
        _current = outfit;
        _sprite.Play(State == CharacterState.Reacting ? outfit.React : outfit.Idle);
    }

    public void OnClicked()
    {
        if (State == CharacterState.Reacting) return;

        State = CharacterState.Reacting;
        _sprite.Play(_current.React);
        Say(Definition.Lines.Click);
        ScheduleChatter();
    }

    public void Update(double dtMs)
    {
        _sprite.Update(dtMs);

        if (State != CharacterState.Idle) return;

        _chatterInMs -= dtMs;
        if (_chatterInMs <= 0)
        {
            Say(Definition.Lines.Idle);
            ScheduleChatter();
        }
    }

    private void Say(string[] lines)
    {
        if (lines.Length == 0) return;
        Said?.Invoke(lines[_rng.Next(lines.Length)]);
    }

    private void ScheduleChatter() => _chatterInMs = _rng.Next(15_000, 35_000);

    private void OnReactFinished()
    {
        State = CharacterState.Idle;
        _sprite.Play(_current.Idle);
    }
}
