using DesktopAdventures.Engine;

namespace DesktopAdventures.Ui;

public readonly record struct Rect(int X, int Y, int W, int H)
{
    public int Right => X + W;
    public int Bottom => Y + H;
    public bool Contains(int px, int py) => px >= X && px < X + W && py >= Y && py < Y + H;
}

/// <summary>
/// Zeichen- und Eingabekontext eines Frames (Immediate-Mode-UI): Beim Zeichnen werden klickbare Flächen
/// mit <see cref="Button"/> angemeldet; <see cref="Click"/> löst die Aktion der obersten getroffenen Fläche aus.
/// </summary>
public sealed class UiContext
{
    private readonly List<(Rect Rect, Action Action)> _hotspots = new();

    public PixelBuffer Buffer { get; private set; } = null!;
    public BitmapFont Font { get; }
    public double Time { get; private set; }
    public int MouseX { get; private set; } = -1;
    public int MouseY { get; private set; } = -1;

    /// <summary>true, wenn die Maus gerade über etwas Klickbarem liegt (für den Hand-Cursor).</summary>
    public bool OverClickable { get; private set; }

    public UiContext(BitmapFont font) => Font = font;

    public void SetMouse(int x, int y) => (MouseX, MouseY) = (x, y);

    public void BeginFrame(PixelBuffer buffer, double time)
    {
        Buffer = buffer;
        Time = time;
        _hotspots.Clear();
        OverClickable = false;
    }

    /// <summary>Meldet eine klickbare Fläche an und liefert, ob die Maus darüber liegt.</summary>
    public bool Button(Rect rect, Action onClick)
    {
        _hotspots.Add((rect, onClick));
        bool hover = rect.Contains(MouseX, MouseY);
        if (hover) OverClickable = true;
        return hover;
    }

    public bool Click(int x, int y)
    {
        for (int i = _hotspots.Count - 1; i >= 0; i--) // zuletzt gezeichnet = oben
        {
            if (!_hotspots[i].Rect.Contains(x, y)) continue;
            _hotspots[i].Action();
            return true;
        }
        return false;
    }
}
