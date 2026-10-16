namespace DesktopAdventures.Engine;

/// <summary>
/// Software-Zeichenfläche im Kunst-Pixel-Raster. Format: 0xAARRGGBB (entspricht Pbgra32 im
/// Little-Endian-Speicher). Aktuell werden nur deckende (A=FF) oder komplett transparente (0)
/// Pixel gezeichnet, daher ist keine Vormultiplikation nötig.
/// </summary>
public sealed class PixelBuffer
{
    public int Width { get; }
    public int Height { get; }
    public uint[] Pixels { get; }

    public PixelBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new uint[width * height];
    }

    public void Clear(uint argb = 0) => Array.Fill(Pixels, argb);

    public void CopyFrom(PixelBuffer source)
    {
        if (source.Pixels.Length != Pixels.Length)
            throw new ArgumentException("Puffergrößen stimmen nicht überein.", nameof(source));
        Array.Copy(source.Pixels, Pixels, Pixels.Length);
    }

    /// <summary>Setzt ein Pixel; Koordinaten außerhalb werden ignoriert.</summary>
    public void Set(int x, int y, uint argb)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        Pixels[y * Width + x] = argb;
    }

    public uint Get(int x, int y) =>
        (uint)x >= (uint)Width || (uint)y >= (uint)Height ? 0 : Pixels[y * Width + x];
}
