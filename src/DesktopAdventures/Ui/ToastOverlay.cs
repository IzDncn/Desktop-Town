using DesktopAdventures.Engine;
using static DesktopAdventures.Ui.BookTheme;

namespace DesktopAdventures.Ui;

/// <summary>
/// Kurze Benachrichtigung am oberen Rand des Streifens (z. B. "Challenge geschafft"). Mehrere Meldungen
/// werden nacheinander gezeigt: einfahren, stehen bleiben, ausfahren.
/// </summary>
public sealed class ToastOverlay
{
    private const double SlideSeconds = 0.35, HoldSeconds = 5.0;
    private const int Padding = 8, MaxTextWidth = 420, Top = 6;

    private readonly Queue<(string Title, string Text)> _queue = new();
    private (string Title, string Text)? _current;
    private double _startedAt;

    public void Show(string title, string text) => _queue.Enqueue((title, text));

    public void Draw(PixelBuffer b, BitmapFont font, double now)
    {
        if (_current == null)
        {
            if (!_queue.TryDequeue(out var next)) return;
            _current = next;
            _startedAt = now;
        }

        double t = now - _startedAt;
        if (t > SlideSeconds * 2 + HoldSeconds)
        {
            _current = null;
            return;
        }

        var (title, text) = _current.Value;
        title = font.Truncate(title, MaxTextWidth);
        text = font.Truncate(text, MaxTextWidth);

        int iconW = PixelIcon.Star.Width * 2;
        int textW = Math.Max(font.Measure(title), font.Measure(text));
        int w = Padding * 3 + iconW + textW, h = Padding * 2 + font.LineHeight + font.LineStep;
        int x = (b.Width - w) / 2;

        // Einfahren von oben, Ausfahren nach oben
        double slide = t < SlideSeconds ? t / SlideSeconds
                     : t > SlideSeconds + HoldSeconds ? 1 - (t - SlideSeconds - HoldSeconds) / SlideSeconds
                     : 1;
        double eased = 1 - Math.Pow(1 - Math.Clamp(slide, 0, 1), 3);
        int y = (int)Math.Round(-h + (h + Top) * eased);

        b.FillRect(x, y, w, h, PageOutline);
        b.FillRect(x + 1, y + 1, w - 2, h - 2, Parchment);
        b.StrokeRect(x + 3, y + 3, w - 6, h - 6, PageBorder);

        PixelIcon.Star.Draw(b, x + Padding, y + (h - PixelIcon.Star.Height * 2) / 2, 0xFFE6B840, 2);
        int tx = x + Padding * 2 + iconW;
        font.Draw(b, title, tx, y + Padding, Ink);
        font.Draw(b, text, tx, y + Padding + font.LineHeight - 2, InkSoft);
    }
}
