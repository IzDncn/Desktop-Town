using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAdventures.Engine;
using DesktopAdventures.Ui;
using static DesktopAdventures.Platform.NativeMethods;

namespace DesktopAdventures.Shell;

/// <summary>
/// Das Buch-Popup: ein eigenes, randloses Fenster (transparente Ecken), in dem der <see cref="BookScreen"/>
/// im selben Pixelraster wie der Streifen gezeichnet wird. Schließen: Button, Esc oder erneuter Klick auf das Buch im Gras.
/// </summary>
public sealed class BookWindow : Window
{
    private readonly BookScreen _screen;
    private readonly PixelBuffer _buffer = new(BookTheme.Width, BookTheme.Height);
    private readonly WriteableBitmap _bitmap =
        new(BookTheme.Width, BookTheme.Height, 96, 96, PixelFormats.Pbgra32, null);
    private readonly Image _view = new();
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly int _pixelSize;
    private readonly double _dpiScale;
    private readonly int _screenX, _screenY;

    /// <summary>Größe des Fensters in physischen Pixeln.</summary>
    public static (int Width, int Height) SizePx(int pixelSize) => (BookTheme.Width * pixelSize, BookTheme.Height * pixelSize);

    /// <param name="screenX">Linke obere Ecke in physischen Bildschirm-Pixeln.</param>
    public BookWindow(BookScreen screen, int pixelSize, double dpiScale, int screenX, int screenY)
    {
        _screen = screen;
        _pixelSize = pixelSize;
        _dpiScale = dpiScale;
        (_screenX, _screenY) = (screenX, screenY);

        Title = "Desktop Adventures – Buch";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = 0;
        Top = 0;
        Width = 100;
        Height = 100;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        _view.Source = _bitmap;
        _view.Stretch = Stretch.Fill;
        _view.Width = BookTheme.Width * pixelSize / dpiScale;
        _view.Height = BookTheme.Height * pixelSize / dpiScale;
        _view.HorizontalAlignment = HorizontalAlignment.Left;
        _view.VerticalAlignment = VerticalAlignment.Top;
        RenderOptions.SetBitmapScalingMode(_view, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(_view, EdgeMode.Aliased);
        Content = _view;

        _view.MouseMove += (_, e) => { var (x, y) = ToArt(e); _screen.MouseMove(x, y); };
        _view.MouseLeave += (_, _) => _screen.MouseLeave();
        _view.MouseLeftButtonDown += (_, e) => { var (x, y) = ToArt(e); _screen.MouseMove(x, y); _screen.Click(x, y); };
        KeyDown += OnKeyDown;
        _screen.CloseRequested += Close;

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / GameConfig.TargetFps),
        };
        _timer.Tick += (_, _) => Draw();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW); // nicht in Alt+Tab
        var (w, h) = SizePx(_pixelSize);
        SetWindowPos(hwnd, HWND_TOPMOST, _screenX, _screenY, w, h, SWP_NOOWNERZORDER);

        Draw();
        _timer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _screen.CloseRequested -= Close;
        base.OnClosed(e);
    }

    private void Draw()
    {
        _screen.Render(_buffer, _clock.Elapsed.TotalSeconds);
        _bitmap.WritePixels(new Int32Rect(0, 0, _buffer.Width, _buffer.Height), _buffer.Pixels, _buffer.Width * 4, 0);
        _view.Cursor = _screen.WantsHandCursor ? Cursors.Hand : Cursors.Arrow;
    }

    private (int X, int Y) ToArt(MouseEventArgs e)
    {
        var p = e.GetPosition(_view);
        double dipPerArt = _pixelSize / _dpiScale;
        return ((int)(p.X / dipPerArt), (int)(p.Y / dipPerArt));
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape: Close(); break;
            case Key.Left: _screen.NextTab(-1); break;
            case Key.Right: _screen.NextTab(1); break;
            default: return;
        }
        e.Handled = true;
    }
}
