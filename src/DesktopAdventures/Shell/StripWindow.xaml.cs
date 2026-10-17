using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using DesktopAdventures.Characters;
using DesktopAdventures.Engine;
using DesktopAdventures.Platform;
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

        string content = Path.Combine(AppContext.BaseDirectory, "Content");
        _catalog = CharacterCatalog.Load(content);
        _startupErrors.AddRange(_catalog.Errors);
        _state = new GameState(_catalog, GameOptions.AllCharacters);

        try
        {
            _font = BitmapFont.Load(Path.Combine(content, "Fonts", "jersey10"));
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
        PlaceWindow(); // folgt der (Autohide-)Taskleiste
        Draw();
    }

    private void Draw()
    {
        _world.Render(_buffer);
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
        if (_world.Click((int)(p.X / dipPerArt), (int)(p.Y / dipPerArt)) == WorldClick.Book)
            ToggleBook();
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
            new CharactersTab(_catalog.Profiles, _state),
            new BuildingsTab(),
        });

        var area = _dock.Query(_layout.DpiScale);
        int pixel = _layout.PixelSize;
        var (width, height) = BookWindow.SizePx(pixel);
        int x = (area.Left + area.Right - width) / 2;
        int y = Math.Max(0, area.Bottom - 40 * pixel - height); // knapp über dem Gras

        _book = new BookWindow(screen, pixel, _layout.DpiScale, x, y);
        _book.Closed += (_, _) => _book = null;
        _book.Show();
        _book.Activate();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
