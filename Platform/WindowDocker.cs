using System.Windows;
using System.Windows.Media;

namespace DesktopAdventures.Platform;

/// <summary>
/// Haelt ein Fenster unten rechts im Hauptbildschirm und schiebt es sanft mit,
/// wenn die Taskleiste ein- oder ausfaehrt, damit sie nie verdeckt wird (oder uns verdeckt).
/// </summary>
public sealed class WindowDocker
{
    private readonly Window _window;
    private double _right;   // aktueller Versatz vom rechten Rand (DIPs)
    private double _bottom;  // aktueller Versatz vom unteren Rand (DIPs)

    /// <summary>Abstand zum rechten Bildschirmrand.</summary>
    public double Margin { get; set; } = 16;

    /// <summary>
    /// Abstand zum unteren Bildschirmrand. Muss > 0 sein, sonst deckt unser Fenster die
    /// Kante ab, an der die Autohide-Taskleiste per Mausberuehrung eingeblendet wird.
    /// </summary>
    public double BottomGap { get; set; } = 6;

    /// <summary>Wie schnell das Fenster der Taskleiste folgt (0..1 pro Tick).</summary>
    public double Follow { get; set; } = 0.5;

    public WindowDocker(Window window) => _window = window;

    /// <summary>Sofort an die Zielposition setzen (z. B. beim Start).</summary>
    public void Snap()
    {
        (_right, _bottom) = GetTarget();
        Apply();
    }

    /// <summary>Pro Game-Loop-Tick aufrufen.</summary>
    public void Update()
    {
        var (targetRight, targetBottom) = GetTarget();
        _right = Approach(_right, targetRight);
        _bottom = Approach(_bottom, targetBottom);
        Apply();
    }

    private (double Right, double Bottom) GetTarget()
    {
        var dpi = VisualTreeHelper.GetDpi(_window);
        var (rightPx, bottomPx) = Taskbar.GetObstructionPx();
        return (rightPx / dpi.DpiScaleX, bottomPx / dpi.DpiScaleY);
    }

    private double Approach(double current, double target)
    {
        double d = target - current;
        return Math.Abs(d) < 0.5 ? target : current + d * Follow;
    }

    private void Apply()
    {
        double left = SystemParameters.PrimaryScreenWidth - _window.Width - Margin - _right;
        double top = SystemParameters.PrimaryScreenHeight - _window.Height - _bottom - BottomGap;

        if (Math.Abs(_window.Left - left) > 0.01) _window.Left = left;
        if (Math.Abs(_window.Top - top) > 0.01) _window.Top = top;
    }
}
