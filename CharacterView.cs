using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DesktopAdventures;

/// <summary>Die sichtbare Seite eines Charakters: Sprite-Image und Sprechblase.</summary>
public sealed class CharacterView
{
    private readonly Image _image;
    private readonly Border _bubble;
    private readonly TextBlock _bubbleText;
    private readonly DispatcherTimer _bubbleTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly double _centerX;
    private readonly double _top;

    public AnimatedSprite Sprite { get; }

    public event Action? Clicked;

    public CharacterView(Canvas sceneLayer, Canvas overlayLayer, Seat seat, int frameWidth, int frameHeight)
    {
        double w = frameWidth * SceneLayout.Scale;
        double h = frameHeight * SceneLayout.Scale;
        _top = SceneLayout.FloorY - h;
        _centerX = seat.X + w / 2;

        _image = new Image { Width = w, Height = h, Stretch = Stretch.Fill, Cursor = Cursors.Hand };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.NearestNeighbor);
        if (seat.FacesLeft)
        {
            _image.RenderTransformOrigin = new Point(0.5, 0.5);
            _image.RenderTransform = new ScaleTransform(-1, 1);
        }
        Canvas.SetLeft(_image, seat.X);
        Canvas.SetTop(_image, _top);
        _image.MouseLeftButtonDown += (_, _) => Clicked?.Invoke();
        sceneLayer.Children.Add(_image);

        Sprite = new AnimatedSprite(_image);

        var ink = new SolidColorBrush(Color.FromRgb(0x3A, 0x2A, 0x1A));
        _bubbleText = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = ink,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 140
        };
        _bubble = new Border
        {
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false, // Sprechblase blockiert keine Klicks
            Background = new SolidColorBrush(Color.FromArgb(0xF2, 0xFF, 0xF4, 0xD6)),
            BorderBrush = ink,
            BorderThickness = new Thickness(3),
            Padding = new Thickness(8, 4, 8, 4),
            Child = _bubbleText
        };
        overlayLayer.Children.Add(_bubble);

        _bubbleTimer.Tick += (_, _) =>
        {
            _bubble.Visibility = Visibility.Collapsed;
            _bubbleTimer.Stop();
        };
    }

    public void ShowBubble(string text)
    {
        _bubbleText.Text = text;

        // Wichtig: Ein Collapsed-Element misst sich auf 0x0. Darum erst sichtbar machen, dann messen -
        // sonst stimmt die Breite nicht und die Blase ragt ueber den Fensterrand hinaus.
        _bubble.Visibility = Visibility.Visible;
        _bubble.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = _bubble.DesiredSize;

        // ueber dem Kopf zentrieren, aber innerhalb des Holzrahmens halten
        double minLeft = SceneLayout.Inset + 4;
        double maxLeft = Math.Max(minLeft, SceneLayout.Width - SceneLayout.Inset - size.Width - 4);
        double left = Math.Clamp(_centerX - size.Width / 2, minLeft, maxLeft);
        double top = Math.Max(SceneLayout.Inset + 2, _top - size.Height - 4);
        Canvas.SetLeft(_bubble, left);
        Canvas.SetTop(_bubble, top);

        _bubbleTimer.Stop();
        _bubbleTimer.Start();
    }
}
