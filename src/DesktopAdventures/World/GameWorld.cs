using DesktopAdventures.Characters;
using DesktopAdventures.Engine;

namespace DesktopAdventures.World;

/// <summary>Die Spielwelt: Landschaft und Figuren (später auch Gebäude und Fortschritt).</summary>
public sealed class GameWorld
{
    private readonly List<CharacterActor> _actors = new();

    public Landscape Landscape { get; }
    public double Time { get; private set; }

    public GameWorld(int artWidth, int artHeight, int seed, IReadOnlyList<CharacterProfile> characters)
    {
        Landscape = new Landscape(artWidth, artHeight, seed);

        // Gleichmäßig über die Breite verteilen, damit sich Figuren beim Start nicht überlagern.
        for (int i = 0; i < characters.Count; i++)
        {
            double slot = (i + 0.5) / characters.Count;
            double jitter = (Noise.Value(i, seed + 50) - 0.5) * artWidth / (characters.Count * 3.0);
            _actors.Add(new CharacterActor(characters[i], artWidth * slot + jitter, (int)Noise.Hash(i, seed + 60), artWidth));
        }
    }

    public void Update(double dtSec)
    {
        Time += dtSec;
        foreach (var actor in _actors) actor.Update(dtSec);
    }

    public void Render(PixelBuffer target)
    {
        Landscape.Render(target, Time);

        // Weiter vorn (größeres Y) liegt weiter oben im Bild.
        _actors.Sort((a, b) => a.FootY(Landscape).CompareTo(b.FootY(Landscape)));
        foreach (var actor in _actors) actor.Draw(target, Landscape);
    }

    /// <summary>Klick in Kunst-Pixel-Koordinaten; liefert true, wenn eine Figur getroffen wurde.</summary>
    public bool Click(int artX, int artY)
    {
        for (int i = _actors.Count - 1; i >= 0; i--) // oberste Figur zuerst
        {
            if (!_actors[i].Contains(artX, artY, Landscape)) continue;
            _actors[i].Poke();
            return true;
        }
        return false;
    }
}
