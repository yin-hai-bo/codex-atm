using System.Diagnostics;
using System.Windows.Input;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace CodexAtm.App;

public partial class AboutWindow : Window
{
    private const double ToastOffsetY = 10;
    private const double ToastHorizontalPadding = 12;
    private const double ToastVerticalPadding = 12;
    private readonly DispatcherTimer _toastTimer = new()
    {
        Interval = TimeSpan.FromSeconds(1.6)
    };

    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
        _toastTimer.Tick += ToastTimer_Tick;
    }

    public string DisplayVersion => AppVersionInfo.DisplayVersion;

    public string CommitId => AssemblyBuildInfo.CommitId;

    public string RepositoryUrl => AssemblyBuildInfo.RepositoryUrl;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CommitId_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        try
        {
            Clipboard.SetText(CommitId);
            ShowToast("Commit ID 已复制到剪贴板", e.GetPosition(RootGrid));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"复制失败：{ex.Message}",
                "复制失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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

    private void ShowToast(string message, Point anchorPoint)
    {
        ToastText.Text = message;
        ToastBorder.Visibility = Visibility.Visible;
        ToastBorder.Opacity = 1;
        ToastBorder.UpdateLayout();

        var left = Math.Max(ToastHorizontalPadding, anchorPoint.X);
        var top = Math.Max(ToastVerticalPadding, anchorPoint.Y + ToastOffsetY);
        var maxLeft = Math.Max(
            ToastHorizontalPadding,
            RootGrid.ActualWidth - ToastBorder.ActualWidth - ToastHorizontalPadding);
        var maxTop = Math.Max(
            ToastVerticalPadding,
            RootGrid.ActualHeight - ToastBorder.ActualHeight - ToastVerticalPadding);

        ToastBorder.Margin = new Thickness(
            Math.Min(left, maxLeft),
            Math.Min(top, maxTop),
            0,
            0);

        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void ToastTimer_Tick(object? sender, EventArgs e)
    {
        _toastTimer.Stop();
        ToastBorder.Visibility = Visibility.Collapsed;
        ToastBorder.Opacity = 0;
    }

    protected override void OnClosed(EventArgs e)
    {
        _toastTimer.Stop();
        _toastTimer.Tick -= ToastTimer_Tick;
        base.OnClosed(e);
    }
}
