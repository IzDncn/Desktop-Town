using System.IO;
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

    /// <param name="dpiScale">DPI-Faktor des Hauptbildschirms (für den Autohide-Abstand).</param>
    public DockArea Query(double dpiScale)
    {
        var monitor = MonitorFromPoint(default, MONITOR_DEFAULTTOPRIMARY);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info))
            return new DockArea(0, 1920, 1080); // Notnagel, tritt praktisch nie ein

        int bottom = info.rcWork.Bottom;

        if (IsAutohide())
        {
            // Arbeitsbereich ignoriert die ausgeblendete Taskleiste -> ihr aktuelles Rechteck auswerten.
            int byRect = VisibleTrayHeight(info.rcMonitor);
            int byProbe = ProbeTrayHeight(info.rcMonitor);
            int visible = Math.Max(byRect, byProbe);
            LogDebug(byRect, byProbe);
            int gap = (int)Math.Ceiling(GameConfig.AutohideGapDips * dpiScale);
            bottom = info.rcMonitor.Bottom - (visible > HotEdgePx ? visible : gap);
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

    /// <summary>
    /// Zweite, unabhängige Messung: Von der Bildschirmunterkante nach oben abtasten, solange an der Stelle
    /// ein Fenster der Taskleiste liegt. Greift auch, wenn Windows (z. B. Windows 11) das Fensterrechteck der
    /// Taskleiste beim Ein-/Ausfahren nicht mitführt.
    /// </summary>
    private int ProbeTrayHeight(RECT monitor)
    {
        if (_tray == IntPtr.Zero) return 0;

        int x = (monitor.Left + monitor.Right) / 2;
        int found = 0;
        for (int dy = 1; dy <= 160; dy += 2)
        {
            var hwnd = WindowFromPoint(new POINT { X = x, Y = monitor.Bottom - dy });
            if (hwnd == IntPtr.Zero || GetAncestor(hwnd, GA_ROOT) != _tray) break;
            found = dy;
        }
        return found;
    }

    private (int Rect, int Probe) _lastLogged = (-1, -1);

    private void LogDebug(int byRect, int byProbe)
    {
        if (!GameOptions.DebugTaskbar || _lastLogged == (byRect, byProbe)) return;
        _lastLogged = (byRect, byProbe);
        try
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "desktop-adventures-taskbar.log"),
                $"{DateTime.Now:HH:mm:ss.fff} autohide=true rect={byRect}px probe={byProbe}px{Environment.NewLine}");
        }
        catch (IOException) { /* Log ist nur Hilfsmittel */ }
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
