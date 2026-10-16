namespace DesktopAdventures.Engine;

/// <summary>Zustandslose, deterministische Zufallswerte (gleicher Seed = gleiche Welt).</summary>
public static class Noise
{
    public static uint Hash(int x, int seed)
    {
        unchecked
        {
            uint h = (uint)x * 0x9E3779B1u ^ (uint)seed * 0x85EBCA6Bu;
            h ^= h >> 15; h *= 0x2C1B3C6Du;
            h ^= h >> 12; h *= 0x297A2D39u;
            h ^= h >> 15;
            return h;
        }
    }

    /// <summary>Gleichverteilter Wert in [0, 1].</summary>
    public static double Value(int x, int seed) => Hash(x, seed) / (double)uint.MaxValue;

    /// <summary>Weicher 1D-Rauschwert in [0, 1] (Value Noise mit Smoothstep).</summary>
    public static double Smooth(double x, int seed)
    {
        int i = (int)Math.Floor(x);
        double f = x - i;
        f = f * f * (3 - 2 * f);
        return Value(i, seed) * (1 - f) + Value(i + 1, seed) * f;
    }
}
