using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DesktopAdventures;

/// <summary>Eine Frame-Folge aus einem horizontalen Sprite-Sheet.</summary>
public sealed class Animation
{
    public BitmapSource[] Frames { get; }
    public int FrameMs { get; }
    public bool Loop { get; }

    public Animation(BitmapSource[] frames, int frameMs, bool loop)
    {
        Frames = frames;
        FrameMs = frameMs;
        Loop = loop;
    }

    /// <summary>Laedt ein Sprite-Sheet aus dem Ordner Assets/ (Feuer, spaeter Kulisse etc.).</summary>
    public static Animation Load(string file, int frameW, int frameH, int frameMs, bool loop)
        => LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Assets", file), frameW, frameH, frameMs, loop);

    /// <summary>Laedt ein einzelnes Bild (Kulisse o. ae.) aus dem Ordner Assets/.</summary>
    public static BitmapSource LoadAssetImage(string file)
        => ReadBitmap(Path.Combine(AppContext.BaseDirectory, "Assets", file));

    /// <summary>Laedt ein Sprite-Sheet von einem beliebigen Pfad und schneidet es in Frames.</summary>
    public static Animation LoadFromFile(string path, int frameW, int frameH, int frameMs, bool loop)
    {
        var sheet = ReadBitmap(path);

        int count = sheet.PixelWidth / frameW;
        if (count < 1 || sheet.PixelHeight < frameH)
            throw new InvalidDataException(
                $"Sprite-Sheet {Path.GetFileName(path)} ({sheet.PixelWidth}x{sheet.PixelHeight}) " +
                $"ist zu klein fuer Frames von {frameW}x{frameH}.");

        var frames = new BitmapSource[count];
        for (int i = 0; i < count; i++)
        {
            var frame = new CroppedBitmap(sheet, new Int32Rect(i * frameW, 0, frameW, frameH));
            frame.Freeze();
            frames[i] = frame;
        }

        return new Animation(frames, frameMs, loop);
    }

    private static BitmapImage ReadBitmap(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
