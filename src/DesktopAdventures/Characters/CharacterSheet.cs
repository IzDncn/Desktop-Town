using DesktopAdventures.Engine;

namespace DesktopAdventures.Characters;

public readonly record struct AnimationClip(int Row, int Frames, double Fps, bool Loop)
{
    public double Duration => Frames / Fps;
}

/// <summary>Fertig eingefärbtes Sprite-Sheet eines Charakters inklusive Animationsdefinitionen.</summary>
public sealed class CharacterSheet
{
    private readonly Dictionary<string, AnimationClip> _clips;

    public Sprite Image { get; }
    public int FrameWidth { get; }
    public int FrameHeight { get; }

    public CharacterSheet(Sprite image, int frameWidth, int frameHeight, Dictionary<string, AnimationClip> clips)
    {
        Image = image;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        _clips = new Dictionary<string, AnimationClip>(clips, StringComparer.OrdinalIgnoreCase);
    }

    public bool Has(string name) => _clips.ContainsKey(name);

    /// <summary>Liefert die Animation oder, falls es sie nicht gibt, "idle".</summary>
    public AnimationClip Get(string name) => _clips.TryGetValue(name, out var clip) ? clip : _clips["idle"];
}
