using DesktopAdventures.Engine;

namespace DesktopAdventures.World;

/// <summary>Die Spielwelt: hält Landschaft (und später Gebäude, Figuren, Fortschritt) und treibt sie an.</summary>
public sealed class GameWorld
{
    public Landscape Landscape { get; }
    public double Time { get; private set; }

    public GameWorld(int artWidth, int artHeight, int seed)
    {
        Landscape = new Landscape(artWidth, artHeight, seed);
    }

    public void Update(double dtSec) => Time += dtSec;

    public void Render(PixelBuffer target) => Landscape.Render(target, Time);
}
