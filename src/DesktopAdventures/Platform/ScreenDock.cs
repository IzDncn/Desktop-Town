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
/// Arbeitsbereichs-Breite des Hauptbildschirms.
///
/// Feste Taskleiste: Oberkante des Arbeitsbereichs.
/// Automatisch ausgeblendete Taskleiste: Wie hoch sie gerade sichtbar ist, wird aus DREI unabhängigen Signalen
/// bestimmt (das größte gewinnt), weil Windows die Taskleiste je nach Version unterschiedlich animiert:
///   1. Fensterrechteck der Taskleiste,
///   2. Abtasten vom unteren Bildschirmrand nach oben (gehört das Fenster an dieser Stelle der Taskleiste?),
///   3. Vorhersage anhand der Maus (Zeiger am unteren Rand = Taskleiste fährt aus).
/// Ist sie ganz ausgeblendet, bleibt ein kleiner Abstand, damit sie am Rand wieder ausfahren kann.
/// </summary>
public sealed class ScreenDock
{
    private const int HotEdgePx = 4;                // so schmal bleibt eine ausgeblendete Taskleiste oft stehen
    private const long AutohideRecheckMs = 1000;
    private const long ReleaseDelayMs = 900;        // so lange nach dem Verlassen der Taskleiste bleibt der Streifen oben
    private const int DefaultTrayDips = 48;

    private IntPtr _tray;
    private bool _autohideFlag;
    private long _autohideCheckedAt = long.MinValue;
    private int _trayFullPx;

    private bool _expectedShown;
    private long _lastInZoneAt;

    private string? _logPath;
    private string _lastLogLine = "";
    private long _lastLogAt;

    /// <summary>Pfad der Diagnose-Datei oder null, wenn die Diagnose aus ist.</summary>
    public string? LogPath => _logPath;

    public void EnableLog(string path)
    {
        _logPath = path;
        try
        {
            File.WriteAllText(path, $"Desktop Adventures – Taskleisten-Diagnose, gestartet {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                                    "Spalten: autohideFlag, rcMonitor.Bottom, rcWork.Bottom, Taskleisten-Rechteck, rect/probe/predicted (px), Streifen-Unterkante" +
                                    Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logPath = null;
        }
    }

    /// <param name="dpiScale">DPI-Faktor des Hauptbildschirms (für Abstand und Standardhöhe der Taskleiste).</param>
    public DockArea Query(double dpiScale)
    {
        var monitor = MonitorFromPoint(default, MONITOR_DEFAULTTOPRIMARY);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info))
            return new DockArea(0, 1920, 1080); // Notnagel, tritt praktisch nie ein

        var screen = info.rcMonitor;
        int bottom = info.rcWork.Bottom;

        bool hasTray = TryGetBottomTray(screen, out var tray);
        bool workAreaReserved = info.rcWork.Bottom < screen.Bottom;
        bool flag = IsAutohideFlag();
        // Bleibt unten kein Arbeitsbereich reserviert, obwohl es eine Taskleiste unten gibt, blendet sie sich aus.
        bool autohide = hasTray && (flag || !workAreaReserved);

        int byRect = 0, byProbe = 0, predicted = 0;
        if (autohide)
        {
            int full = FullTrayHeight(tray, dpiScale);
            byRect = Math.Clamp(screen.Bottom - tray.Top, 0, full);
            byProbe = ProbeTrayHeight(screen);
            predicted = PredictShown(screen, full) ? full : 0;

            int visible = GameConfig.AutohideFollowsTaskbar ? Math.Max(byRect, Math.Max(byProbe, predicted)) : full;
            int gap = (int)Math.Ceiling(GameConfig.AutohideGapDips * dpiScale);
            bottom = screen.Bottom - (visible > HotEdgePx ? visible : gap);
        }

        Log(flag, autohide, screen, info.rcWork, tray, byRect, byProbe, predicted, bottom);
        return new DockArea(info.rcWork.Left, info.rcWork.Right, bottom);
    }

    // ---------- Taskleiste finden ----------

    private bool TryGetBottomTray(RECT screen, out RECT rect)
    {
        rect = default;
        if (_tray == IntPtr.Zero || !IsWindow(_tray))
            _tray = FindWindow("Shell_TrayWnd", null);
        if (_tray == IntPtr.Zero || !GetWindowRect(_tray, out rect))
            return false;

        int w = rect.Right - rect.Left, h = rect.Bottom - rect.Top;
        return w >= h && (rect.Top + rect.Bottom) / 2 > (screen.Top + screen.Bottom) / 2;
    }

    private int FullTrayHeight(RECT tray, double dpiScale)
    {
        int h = tray.Bottom - tray.Top;
        if (h >= 20 && h <= 300) _trayFullPx = Math.Max(_trayFullPx, h);
        return _trayFullPx > 0 ? _trayFullPx : (int)Math.Ceiling(DefaultTrayDips * dpiScale);
    }

    private bool IsAutohideFlag()
    {
        long now = Environment.TickCount64;
        if (now - _autohideCheckedAt < AutohideRecheckMs) return _autohideFlag;

        var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
        _autohideFlag = ((uint)SHAppBarMessage(ABM_GETSTATE, ref data) & ABS_AUTOHIDE) != 0;
        _autohideCheckedAt = now;
        return _autohideFlag;
    }

    // ---------- Signal 2: Abtasten ----------

    private int ProbeTrayHeight(RECT screen)
    {
        if (_tray == IntPtr.Zero) return 0;

        int x = (screen.Left + screen.Right) / 2;
        int found = 0;
        for (int dy = 1; dy <= 160; dy += 2)
        {
            var hwnd = WindowFromPoint(new POINT { X = x, Y = screen.Bottom - dy });
            if (hwnd == IntPtr.Zero || GetAncestor(hwnd, GA_ROOT) != _tray) break;
            found = dy;
        }
        return found;
    }

    // ---------- Signal 3: Maus ----------

    /// <summary>
    /// Windows fährt die Taskleiste aus, sobald der Zeiger den unteren Rand berührt, und blendet sie kurz nach dem
    /// Verlassen wieder aus. Das bilden wir nach, falls die beiden anderen Signale nichts liefern.
    /// </summary>
    private bool PredictShown(RECT screen, int fullHeight)
    {
        long now = Environment.TickCount64;
        if (!GetCursorPos(out var p)) return _expectedShown;

        bool onScreenX = p.X >= screen.Left && p.X < screen.Right;
        bool atEdge = onScreenX && p.Y >= screen.Bottom - 2;
        bool inZone = onScreenX && p.Y >= screen.Bottom - fullHeight - 4;

        if (atEdge) _expectedShown = true;
        if (_expectedShown && inZone) _lastInZoneAt = now;
        if (_expectedShown && !inZone && now - _lastInZoneAt > ReleaseDelayMs) _expectedShown = false;
        return _expectedShown;
    }

    // ---------- Diagnose ----------

    private void Log(bool flag, bool autohide, RECT screen, RECT work, RECT tray, int byRect, int byProbe, int predicted, int bottom)
    {
        if (_logPath == null) return;

        string line = $"autohideFlag={flag} autohide={autohide} monitor.Bottom={screen.Bottom} work.Bottom={work.Bottom} " +
                      $"tray=({tray.Left},{tray.Top},{tray.Right},{tray.Bottom}) rect={byRect} probe={byProbe} predicted={predicted} stripBottom={bottom}";
        long now = Environment.TickCount64;
        if (line == _lastLogLine || now - _lastLogAt < 50) return;

        _lastLogLine = line;
        _lastLogAt = now;
        try
        {
            File.AppendAllText(_logPath, $"{DateTime.Now:HH:mm:ss.fff} {line}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logPath = null;
        }
    }
}
