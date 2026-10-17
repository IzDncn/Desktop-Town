namespace DesktopAdventures.Engine;

/// <summary>
/// Software-Zeichenfläche im Kunst-Pixel-Raster. Format: 0xAARRGGBB (entspricht Pbgra32 im
/// Little-Endian-Speicher). Es werden nur deckende (A=FF) oder komplett transparente (0) Pixel
/// gezeichnet, daher ist keine Vormultiplikation nötig.
/// </summary>
public sealed class PixelBuffer
{
    private int _clipX0, _clipY0, _clipX1, _clipY1; // Rechteck [x0, x1) x [y0, y1)

    public int Width { get; }
    public int Height { get; }
    public uint[] Pixels { get; }

    public PixelBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new uint[width * height];
        ResetClip();
    }

    public void Clear(uint argb = 0) => Array.Fill(Pixels, argb);

    public void CopyFrom(PixelBuffer source)
    {
        if (source.Pixels.Length != Pixels.Length)
            throw new ArgumentException("Puffergrößen stimmen nicht überein.", nameof(source));
        Array.Copy(source.Pixels, Pixels, Pixels.Length);
    }

    /// <summary>Setzt ein Pixel; Koordinaten außerhalb des Puffers werden ignoriert (Clip-Rechteck wird NICHT beachtet).</summary>
    public void Set(int x, int y, uint argb)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        Pixels[y * Width + x] = argb;
    }

    public uint Get(int x, int y) =>
        (uint)x >= (uint)Width || (uint)y >= (uint)Height ? 0 : Pixels[y * Width + x];

    // ---------- Clip-Rechteck (nur UI-Funktionen: SetClipped, FillRect, StrokeRect, Draw) ----------

    public void SetClip(int x, int y, int width, int height)
    {
        _clipX0 = Math.Max(0, x);
        _clipY0 = Math.Max(0, y);
        _clipX1 = Math.Min(Width, x + width);
        _clipY1 = Math.Min(Height, y + height);
    }

    public void ResetClip() => (_clipX0, _clipY0, _clipX1, _clipY1) = (0, 0, Width, Height);

    public void SetClipped(int x, int y, uint argb)
    {
        if (x < _clipX0 || x >= _clipX1 || y < _clipY0 || y >= _clipY1) return;
        Pixels[y * Width + x] = argb;
    }

    public void FillRect(int x, int y, int width, int height, uint argb)
    {
        int x0 = Math.Max(x, _clipX0), x1 = Math.Min(x + width, _clipX1);
        int y0 = Math.Max(y, _clipY0), y1 = Math.Min(y + height, _clipY1);
        for (int j = y0; j < y1; j++)
            for (int i = x0; i < x1; i++)
                Pixels[j * Width + i] = argb;
    }

    /// <summary>1 Pixel breiter Rahmen innerhalb des Rechtecks.</summary>
    public void StrokeRect(int x, int y, int width, int height, uint argb)
    {
        FillRect(x, y, width, 1, argb);
        FillRect(x, y + height - 1, width, 1, argb);
        FillRect(x, y, 1, height, argb);
        FillRect(x + width - 1, y, 1, height, argb);
    }

    /// <summary>
    /// Zeichnet einen Ausschnitt eines Sprites (transparente Pixel werden übersprungen).
    /// flipX spiegelt horizontal (Figur schaut nach links), scale vergrößert ganzzahlig,
    /// tint färbt alle deckenden Pixel einheitlich (z. B. Silhouette).
    /// </summary>
    public void Draw(Sprite sprite, int srcX, int srcY, int width, int height, int destX, int destY,
        bool flipX = false, int scale = 1, uint? tint = null)
    {
        for (int j = 0; j < height; j++)
        {
            int srcRow = (srcY + j) * sprite.Width + srcX;
            for (int i = 0; i < width; i++)
            {
                uint color = sprite.Pixels[srcRow + (flipX ? width - 1 - i : i)];
                if (color >> 24 == 0) continue;
                if (tint is { } t) color = t;

                if (scale == 1) SetClipped(destX + i, destY + j, color);
                else FillRect(destX + i * scale, destY + j * scale, scale, scale, color);
            }
        }
    }
}
