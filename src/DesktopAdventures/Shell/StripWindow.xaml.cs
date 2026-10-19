using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using DesktopAdventures.Challenges;
using DesktopAdventures.Characters;
using DesktopAdventures.Engine;
using DesktopAdventures.Platform;
using DesktopAdventures.Save;
using DesktopAdventures.Ui;
using DesktopAdventures.World;
using Microsoft.Win32;
using static DesktopAdventures.Platform.NativeMethods;

namespace DesktopAdventures.Shell;

/// <summary>
/// Der Streifen: ein randloses, transparentes Fenster über die volle Bildschirmbreite, das direkt
/// auf der Taskleiste sitzt. Besitzt den Game-Loop und blittet den Software-Puffer auf den Bildschirm.
/// </summary>
public partial class StripWindow : Window
{
    private const double FollowFactor = 0.5; // wie schnell der Streifen nach unten gleitet, wenn die Autohide-Taskleiste ausfährt (0..1 je Tick)

    private readonly ScreenDock _dock = new();
    private readonly CharacterCatalog _catalog;
    private readonly GameState _state;
    private readonly ChallengeManager _challenges;
    private readonly ToastOverlay _toasts = new();
    private double _challengeTimer, _saveTimer;
    private readonly List<string> _startupErrors = new();
    private BitmapFont? _font;
    private BookWindow? _book;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _timer;

    private IntPtr _hwnd;
    private StripLayout _layout;
    private PixelBuffer _buffer = null!;
    private WriteableBitmap _bitmap = null!;
    private GameWorld _world = null!;

    private double _y;
    private bool _positioned;
    private (int X, int Y, int W, int H) _applied;
    private TimeSpan _lastTick;

    public StripWindow()
    {
        InitializeComponent();

        if (GameOptions.DebugTaskbar) EnableTaskbarLog();
#if DEBUG
        AddDevMenu();
#endif

        string content = Path.Combine(AppContext.BaseDirectory, "Content");
        _catalog = CharacterCatalog.Load(content);
        _startupErrors.AddRange(_catalog.Errors);

        // Spielstand (mit --all-characters wird nichts gespeichert)
        var store = new SaveStore();
        if (GameOptions.ResetSave) store.Delete();
        var data = store.Load(out string? saveWarning);
        if (saveWarning != null) _startupErrors.Add(saveWarning);
        _state = new GameState(_catalog, data, store, GameOptions.AllCharacters);

        var challengeCatalog = ChallengeCatalog.Load(content, _catalog);
        _startupErrors.AddRange(challengeCatalog.Errors);
        _challenges = new ChallengeManager(_state, challengeCatalog.Challenges);
        _challenges.Completed += OnChallengeCompleted;
        _state.CharacterUnlocked += profile => _world?.AddCharacter(profile);

        try
        {
            _font = BitmapFont.Load(Path.Combine(content, "Fonts", "jersey20"));
        }
        catch (Exception ex)
        {
            _startupErrors.Add($"Schrift konnte nicht geladen werden (das Buch ist deaktiviert): {ex.Message}");
        }

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / GameConfig.TargetFps),
        };
        _timer.Tick += OnTick;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;

        // Nicht in Alt+Tab auftauchen.
        SetWindowLong(_hwnd, GWL_EXSTYLE, GetWindowLong(_hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
        HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Application.Current.SessionEnding += (_, _) => _state.SaveNow(); // Windows wird heruntergefahren

        Rebuild();
        _lastTick = _clock.Elapsed;
        _timer.Start();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_startupErrors.Count == 0) return;

        MessageBox.Show("Beim Laden sind Probleme aufgetreten:\n\n" + string.Join("\n", _startupErrors),
            "Desktop Adventures", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _book?.Close();
        _state.SaveNow();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        base.OnClosed(e);
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        Rebuild();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(new Action(Rebuild));

    /// <summary>Klicks auf den Streifen sollen dem Programm im Vordergrund nicht den Fokus stehlen.</summary>
    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_MOUSEACTIVATE) return IntPtr.Zero;
        handled = true;
        return new IntPtr(MA_NOACTIVATE);
    }

    // ---------- Layout / Puffer ----------

    /// <summary>Berechnet Raster und Puffer neu (Start, Auflösungs- oder DPI-Wechsel).</summary>
    private void Rebuild()
    {
        double dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var area = _dock.Query(dpi);
        _layout = StripLayout.Create(area.Width, dpi);

        _buffer = new PixelBuffer(_layout.ArtWidth, _layout.ArtHeight);
        _bitmap = new WriteableBitmap(_layout.ArtWidth, _layout.ArtHeight, 96, 96, PixelFormats.Pbgra32, null);
        View.Source = _bitmap;
        View.Width = _layout.ViewWidthDips;
        View.Height = _layout.ViewHeightDips;

        IReadOnlyList<CharacterProfile> spawn = _catalog.Profiles.Where(_state.IsUnlocked).ToList();
        _world = new GameWorld(_layout.ArtWidth, _layout.ArtHeight, GameConfig.WorldSeed, spawn);

        _positioned = false;
        _applied = default;
        PlaceWindow();
        Draw();
    }

    // ---------- Game-Loop ----------

    private void OnTick(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed;
        double dt = Math.Min((now - _lastTick).TotalSeconds, 0.1); // nach Hängern keine Zeitsprünge
        _lastTick = now;

        _world.Update(dt);
        _state.AddStat("playtime_seconds", dt);

        _challengeTimer += dt;
        if (_challengeTimer >= 0.5)
        {
            _challengeTimer = 0;
            _challenges.Update();
        }
        _saveTimer += dt;
        if (_saveTimer >= 20)
        {
            _saveTimer = 0;
            _state.SaveIfDirty();
        }

        PlaceWindow(); // folgt der (Autohide-)Taskleiste
        Draw();
    }

    private void Draw()
    {
        _world.Render(_buffer);
        if (_font != null) _toasts.Draw(_buffer, _font, _clock.Elapsed.TotalSeconds);
        _bitmap.WritePixels(new Int32Rect(0, 0, _buffer.Width, _buffer.Height), _buffer.Pixels, _buffer.Width * 4, 0);
    }

    // ---------- Positionierung ----------

    private void PlaceWindow()
    {
        var area = _dock.Query(_layout.DpiScale);
        double targetY = area.Bottom - _layout.WindowHeightPx;

        if (!_positioned)
        {
            _y = targetY;
            _positioned = true;
        }
        else
        {
            // Fährt die Taskleiste ein (Ziel höher), sofort mitgehen, damit sie nie über dem Spiel liegt;
            // beim Ausfahren weich nach unten gleiten.
            double d = targetY - _y;
            _y = d < 0 || Math.Abs(d) < 0.5 ? targetY : _y + d * FollowFactor;
        }

        var next = (X: area.Left, Y: (int)Math.Round(_y), W: _layout.WindowWidthPx, H: _layout.WindowHeightPx);
        if (next == _applied) return;

        SetWindowPos(_hwnd, HWND_TOPMOST, next.X, next.Y, next.W, next.H, SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        _applied = next;
    }

    private void View_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // DIPs -> Kunst-Pixel: ein Kunst-Pixel ist PixelSize/Dpi DIPs groß.
        var p = e.GetPosition(View);
        double dipPerArt = _layout.PixelSize / _layout.DpiScale;
        switch (_world.Click((int)(p.X / dipPerArt), (int)(p.Y / dipPerArt)))
        {
            case WorldClick.Character: _state.AddStat("character_clicks"); break;
            case WorldClick.Book: ToggleBook(); break;
        }
    }

    private void OnChallengeCompleted(Challenge challenge)
    {
        string reward = string.Join(" ", challenge.Rewards
            .Where(r => r.Character != null)
            .Select(r => $"{r.Character!.Name} ist jetzt dabei!"));
        _toasts.Show($"Challenge geschafft: {challenge.Name}", reward.Length > 0 ? reward : "Gut gemacht!");
        _state.SaveNow(); // wichtige Fortschritte sofort sichern
    }

    private void OpenBook_Click(object sender, RoutedEventArgs e)
    {
        if (_book == null) ToggleBook();
        else _book.Activate();
    }

    /// <summary>Öffnet das Buch über dem Gras, mittig auf dem Bildschirm; ein zweiter Aufruf schließt es wieder.</summary>
    private void ToggleBook()
    {
        if (_book != null)
        {
            _book.Close();
            return;
        }
        if (_font == null) return;

        var screen = new BookScreen(_font, new BookTab[]
        {
            new CharactersTab(_catalog.Profiles, _state, _challenges),
            new BuildingsTab(),
            new ChallengesTab(_challenges, _state),
        });
        _state.AddStat("book_opened");

        var area = _dock.Query(_layout.DpiScale);
        int pixel = _layout.PixelSize;
        var (width, height) = BookWindow.SizePx(pixel);
        int x = (area.Left + area.Right - width) / 2;
        int y = Math.Max(0, area.Bottom - 40 * pixel - height); // knapp über dem Gras
        if (_state.BookPosition is { } saved)
        {
            // zuletzt vom Nutzer gewählte Position, aber immer vollständig auf dem Bildschirm
            x = Math.Clamp(saved.X, area.Left, Math.Max(area.Left, area.Right - width));
            y = Math.Clamp(saved.Y, 0, Math.Max(0, area.Bottom - height));
        }

        _book = new BookWindow(screen, pixel, _layout.DpiScale, x, y);
        _book.Closed += (_, _) =>
        {
            if (_book != null && _book.Moved) _state.BookPosition = _book.LastPosition;
            _book = null;
        };
        _book.Show();
        _book.Activate();
    }

    private string EnableTaskbarLog()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "taskbar-debug.log");
        if (_dock.LogPath == null) _dock.EnableLog(path);
        return _dock.LogPath ?? path;
    }

#if DEBUG
    /// <summary>Entwickler-Menüpunkte, nur in Debug-Builds (z. B. beim Starten aus der IDE).</summary>
    private void AddDevMenu()
    {
        var item = new MenuItem { Header = "[Dev] Taskleisten-Log starten" };
        item.Click += (_, _) =>
        {
            string path = EnableTaskbarLog();
            MessageBox.Show("Die Diagnose läuft jetzt. Bewege die Maus zum unteren Rand, lass die Taskleiste ein- und ausfahren " +
                            "und schick mir danach diese Datei:\n\n" + path,
                "Desktop Adventures", MessageBoxButton.OK, MessageBoxImage.Information);
            if (File.Exists(path)) Process.Start("explorer.exe", $"/select,\"{path}\"");
        };
        ContextMenu?.Items.Insert(1, item);
    }
#endif

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
