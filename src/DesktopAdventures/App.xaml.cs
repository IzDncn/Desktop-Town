using System.Windows;
using DesktopAdventures.Shell;

namespace DesktopAdventures;

public partial class App : Application
{
    // Referenz halten, sonst gibt der GC den Mutex frei und eine zweite Instanz könnte starten.
    private Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(initiallyOwned: true, "DesktopAdventures.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        MainWindow = new StripWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
