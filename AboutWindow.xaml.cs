using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace ZenDisk;

/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow : Window
{
    private const string RepositoryUrl = "https://github.com/vlkvkn/ZenDisk";

    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        VersionTextBlock.Text = version is null
            ? "Version: unknown"
            : $"Version: {version.Major}.{version.Minor}.{version.Build}";
        RepositoryTextRun.Text = RepositoryUrl;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void GitHubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        OpenExternalUrl(e.Uri);
        e.Handled = true;
    }

    private static void OpenExternalUrl(Uri? uri)
    {
        if (uri is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Win32Exception)
        {
            MessageBox.Show(
                "Unable to open the repository link in your browser.",
                "Navigation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
