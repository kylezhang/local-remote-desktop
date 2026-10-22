using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace LocalRemoteDesktop;

internal static class StartupCheck {
    internal static int Run(string reportPath) {
        bool passed = false;
        string error = "";
        try {
            var window = new MainWindow(false, null);
            try {
                var content = (FrameworkElement)window.Content;
                content.Measure(new Size(584, 640));
                content.Arrange(new Rect(0, 0, 584, 640));
                content.UpdateLayout();
                if (window.IsVisible || window.IsActive || content.ActualWidth <= 0 ||
                    ((FrameworkElement)window.FindName("EntryPage")).Visibility != Visibility.Visible ||
                    ((Image)window.FindName("ConnectionStage")).Source is not BitmapSource { PixelWidth: 1500, PixelHeight: 798 } ||
                    ((RadioButton)window.FindName("NavConnect")).ActualWidth <= 0 ||
                    ((RadioButton)window.FindName("NavShare")).ActualWidth <= 0) {
                    throw new InvalidOperationException("Packaged home resources/layout failed the unshown startup check.");
                }
            } finally { window.Close(); }
            passed = true;
        } catch (Exception ex) { error = ex.ToString(); }
        try {
            var path = Path.GetFullPath(reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new {
                passed, error, build = BuildInfo.Display,
                check = "packaged-apphost-unshown-startup",
                physicalAcceptance = "not-run"
            }, new JsonSerializerOptions { WriteIndented = true }));
        } catch { return 1; }
        return passed ? 0 : 1;
    }
}
