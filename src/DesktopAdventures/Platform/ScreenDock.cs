using System.Runtime.InteropServices;
using static DesktopAdventures.Platform.NativeMethods;

namespace DesktopAdventures.Platform;

/// <summary>Bereich (physische Pixel) des Hauptbildschirms, auf dem der Streifen sitzen darf.</summary>
public readonly record struct DockArea(int Left, int Right, int Bottom)
{
    public int Width => Right - Left;
}

/// <summary>
/// Ermittelt, wo der Streifen sitzen soll: direkt über der Taskleiste, über die volle
/// Arbeitsbereichs-Breite des Hauptbildschirms. Berücksichtigt feste und automatisch
/// ausgeblendete Taskleisten (dann folgt der Streifen dem Ein-/Ausfahren).
/// </summary>
public sealed class ScreenDock
{
    // Eine ausgeblendete Taskleiste lässt meist einen 1–3 px schmalen Streifen stehen.
    private const int HotEdgePx = 4;
    private const long AutohideRecheckMs = 1000;

    private IntPtr _tray;
    private bool _autohide;
    private long _autohideCheckedAt = long.MinValue;

    public DockArea Query()
    {
        var monitor = MonitorFromPoint(default, MONITOR_DEFAULTTOPRIMARY);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info))
            return new DockArea(0, 1920, 1080); // Notnagel, tritt praktisch nie ein

        int bottom = info.rcWork.Bottom;

        if (IsAutohide())
        {
            // Arbeitsbereich ignoriert die ausgeblendete Taskleiste -> ihr aktuelles Rechteck auswerten.
            int visible = VisibleTrayHeight(info.rcMonitor);
            bottom = info.rcMonitor.Bottom - (visible > HotEdgePx ? visible : GameConfig.AutohideGapPx);
        }

        return new DockArea(info.rcWork.Left, info.rcWork.Right, bottom);
    }

    private bool IsAutohide()
    {
        long now = Environment.TickCount64;
        if (now - _autohideCheckedAt < AutohideRecheckMs) return _autohide;

        var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
        _autohide = ((uint)SHAppBarMessage(ABM_GETSTATE, ref data) & ABS_AUTOHIDE) != 0;
        _autohideCheckedAt = now;
        return _autohide;
    }

    /// <summary>Sichtbare Höhe einer unten liegenden Taskleiste; 0, wenn oben/seitlich oder nicht gefunden.</summary>
    private int VisibleTrayHeight(RECT monitor)
    {
        if (_tray == IntPtr.Zero || !IsWindow(_tray))
            _tray = FindWindow("Shell_TrayWnd", null);
        if (_tray == IntPtr.Zero || !GetWindowRect(_tray, out var r))
            return 0;

        int w = r.Right - r.Left, h = r.Bottom - r.Top;
        bool atBottom = w >= h && (r.Top + r.Bottom) / 2 > (monitor.Top + monitor.Bottom) / 2;
        return atBottom ? Math.Clamp(monitor.Bottom - r.Top, 0, h) : 0;
    }
}
