namespace DesktopAdventures.Ui;

/// <summary>Ein Reiter des Buchs. Zeichnet beide Seiten und meldet klickbare Flächen über den <see cref="UiContext"/> an.</summary>
public abstract class BookTab
{
    public abstract string Title { get; }

    /// <summary>Farbe des Lesezeichens (0xAARRGGBB).</summary>
    public abstract uint Color { get; }

    public abstract PixelIcon Icon { get; }

    public abstract void Draw(UiContext ui, Rect leftPage, Rect rightPage);

    /// <summary>Wird aufgerufen, wenn der Reiter (wieder) geöffnet wird.</summary>
    public virtual void OnOpened() { }
}
