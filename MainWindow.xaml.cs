using System.Windows;
using System.Windows.Threading;
using DesktopAdventures.Platform;

namespace DesktopAdventures;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _loop = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private DateTime _lastTick = DateTime.UtcNow;

    private readonly WindowDocker _docker;
    private readonly CampScene _camp;

    public MainWindow()
    {
        InitializeComponent();

        Width = SceneLayout.Width;
        Height = SceneLayout.Height;

        _docker = new WindowDocker(this);
        _camp = new CampScene(SceneLayer, OverlayLayer);

        _loop.Tick += OnTick;
        _loop.Start();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _docker.Snap();

        if (_camp.LoadErrors.Count > 0)
        {
            MessageBox.Show(this,
                "Einige Charaktere konnten nicht geladen werden:\n\n" + string.Join("\n", _camp.LoadErrors),
                "Desktop Adventures", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        double dtMs = (now - _lastTick).TotalMilliseconds;
        _lastTick = now;

        _docker.Update();   // folgt der (Autohide-)Taskleiste
        _camp.Update(dtMs); // Feuer + Charaktere
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
