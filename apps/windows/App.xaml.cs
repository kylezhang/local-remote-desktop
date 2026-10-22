using System.Windows;
namespace LocalRemoteDesktop;
public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        if (e.Args.Length > 0 && e.Args[0] == "--check-startup") {
            // Exercise the actual packaged apphost/runtime/resources without the
            // normal StartupUri showing a window or opening user preferences.
            // StartupUri rejects null assignments; leave it unset in this mode.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            base.OnStartup(e);
            Shutdown(e.Args.Length == 2 ? StartupCheck.Run(e.Args[1]) : 2);
            return;
        }
        StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
        base.OnStartup(e);
    }
}
