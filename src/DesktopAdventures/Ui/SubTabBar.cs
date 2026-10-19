using static DesktopAdventures.Ui.BookTheme;

namespace DesktopAdventures.Ui;

/// <summary>Untermenü: eine Reihe kleiner Schaltflächen oben auf der Seite, mit der man die Ansicht eines Reiters umschaltet.</summary>
public static class SubTabBar
{
    /// <summary>Zeichnet die Leiste und liefert ihre Höhe.</summary>
    public static int Draw(UiContext ui, int x, int y, IReadOnlyList<string> names, int active, Action<int> onSelect)
    {
        var font = ui.Font;
        int height = font.LineHeight + 2;
        int cx = x;

        for (int i = 0; i < names.Count; i++)
        {
            int index = i;
            int width = font.Measure(names[i]) + 14;
            var rect = new Rect(cx, y, width, height);
            bool hover = ui.Button(rect, () => onSelect(index));
            bool selected = i == active;

            if (selected)
            {
                ui.Buffer.FillRect(rect.X, rect.Y, rect.W, rect.H, CardFill);
                ui.Buffer.StrokeRect(rect.X, rect.Y, rect.W, rect.H, CardBorderHover);
            }
            else if (hover)
            {
                ui.Buffer.StrokeRect(rect.X, rect.Y, rect.W, rect.H, CardBorder);
            }

            font.Draw(ui.Buffer, names[i], cx + 7, y + 1, selected || hover ? Ink : InkSoft);
            cx += width + 4;
        }
        return height;
    }
}
