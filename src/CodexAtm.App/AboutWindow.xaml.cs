using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CodexAtm.App;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string DisplayVersion => AppVersionInfo.DisplayVersion;

    public string CommitId => AssemblyBuildInfo.CommitId;

    public string RepositoryUrl => AssemblyBuildInfo.RepositoryUrl;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RepositoryLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }
}
