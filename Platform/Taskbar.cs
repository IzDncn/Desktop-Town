using System.Runtime.InteropServices;

namespace DesktopAdventures.Platform;

/// <summary>
/// Liest aus, wie weit die Windows-Taskleiste aktuell in den Bildschirm hineinragt.
/// Funktioniert fuer feste und fuer automatisch ausgeblendete Taskleisten: Waehrend sie
/// ein- oder ausfaehrt, aendert sich ihr Fenster-Rechteck laufend.
/// </summary>
public static class Taskbar
{
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    // Eine ausgeblendete Taskleiste laesst meist einen 1-3 px schmalen Streifen stehen.
    private const int HotEdgePx = 4;

    private static IntPtr _hwnd;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    /// <summary>
    /// Sichtbare Taskleisten-Ausdehnung am rechten und unteren Rand des Hauptbildschirms
    /// in physischen Pixeln. Taskleisten oben/links stoeren unser Fenster nicht -> 0.
    /// </summary>
    public static (int Right, int Bottom) GetObstructionPx()
    {
        if (_hwnd == IntPtr.Zero || !IsWindow(_hwnd))
            _hwnd = FindWindow("Shell_TrayWnd", null);
        if (_hwnd == IntPtr.Zero || !GetWindowRect(_hwnd, out var r))
            return (0, 0);

        int screenW = GetSystemMetrics(SM_CXSCREEN);
        int screenH = GetSystemMetrics(SM_CYSCREEN);
        int w = r.Right - r.Left;
        int h = r.Bottom - r.Top;
        if (w <= 0 || h <= 0) return (0, 0);

        if (w >= h) // Taskleiste liegt oben oder unten
        {
            bool atBottom = (r.Top + r.Bottom) / 2 > screenH / 2;
            if (!atBottom) return (0, 0);

            int visible = Math.Clamp(screenH - r.Top, 0, h);
            return (0, visible > HotEdgePx ? visible : 0);
        }
        else // Taskleiste liegt links oder rechts
        {
            bool atRight = (r.Left + r.Right) / 2 > screenW / 2;
            if (!atRight) return (0, 0);

            int visible = Math.Clamp(screenW - r.Left, 0, w);
            return (visible > HotEdgePx ? visible : 0, 0);
        }
    }
}
