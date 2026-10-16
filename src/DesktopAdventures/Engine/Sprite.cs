using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopAdventures.Engine;

/// <summary>
/// Pixelbild im Format 0xAARRGGBB. Pixel sind entweder deckend oder komplett transparent:
/// beim Laden wird Alpha &lt; 128 zu transparent und alles andere zu deckend (keine Halbtransparenz in Pixel-Art).
/// </summary>
public sealed class Sprite
{
    public int Width { get; }
    public int Height { get; }
    public uint[] Pixels { get; }

    public Sprite(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new uint[width * height];
    }

    public Sprite Clone()
    {
        var copy = new Sprite(Width, Height);
        Array.Copy(Pixels, copy.Pixels, Pixels.Length);
        return copy;
    }

    public static Sprite Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Bilddatei nicht gefunden: {path}");

        var decoder = BitmapDecoder.Create(new Uri(Path.GetFullPath(path), UriKind.Absolute),
            BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var bitmap = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);

        int w = bitmap.PixelWidth, h = bitmap.PixelHeight;
        var bytes = new byte[w * h * 4];
        bitmap.CopyPixels(bytes, w * 4, 0);

        var sprite = new Sprite(w, h);
        for (int i = 0; i < sprite.Pixels.Length; i++)
        {
            byte b = bytes[i * 4], g = bytes[i * 4 + 1], r = bytes[i * 4 + 2], a = bytes[i * 4 + 3];
            sprite.Pixels[i] = a < 128 ? 0 : 0xFF000000u | (uint)r << 16 | (uint)g << 8 | b;
        }
        return sprite;
    }
}
