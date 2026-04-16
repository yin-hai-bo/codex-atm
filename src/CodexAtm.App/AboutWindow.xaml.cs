using System.Diagnostics;
using System.Windows.Input;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.ComponentModel;

namespace CodexAtm.App;

public partial class AboutWindow : Window, INotifyPropertyChanged
{
    private const double ToastOffsetY = 10;
    private const double ToastHorizontalPadding = 12;
    private const double ToastVerticalPadding = 12;
    private readonly LocalizationService _localizationService;
    private readonly DispatcherTimer _toastTimer = new()
    {
        Interval = TimeSpan.FromSeconds(1.6)
    };

    public AboutWindow()
    {
        InitializeComponent();
        _localizationService = ((App)Application.Current).LocalizationService;
        DataContext = this;
        _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
        _toastTimer.Tick += ToastTimer_Tick;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WindowTitle => AppText.AboutWindowTitle;

    public string ProductName => AppText.ProductName;

    public string AboutWindowDescription => AppText.AboutWindowDescription;

    public string DisplayVersion => AppVersionInfo.DisplayVersion;

    public string VersionLabel => AppText.AboutVersionLabel;

    public string GitCommitIdLabel => AppText.GitCommitIdLabel;

    public string CommitId => AssemblyBuildInfo.CommitId;

    public string CopyOnClickToolTip => AppText.CopyOnClickToolTip;

    public string RepositoryUrl => AssemblyBuildInfo.RepositoryUrl;

    public string RepositoryLabel => AppText.RepositoryLabel;

    public string CloseButtonText => AppText.CloseButton;

    public string DefaultToastText => AppText.CopiedToClipboard;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CommitId_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        try
        {
            Clipboard.SetText(CommitId);
            ShowToast(AppText.CommitCopiedToClipboard, e.GetPosition(RootGrid));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                AppText.CopyFailed(ex.Message),
                AppText.CopyFailedTitle,
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

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(ProductName));
        OnPropertyChanged(nameof(AboutWindowDescription));
        OnPropertyChanged(nameof(VersionLabel));
        OnPropertyChanged(nameof(GitCommitIdLabel));
        OnPropertyChanged(nameof(CopyOnClickToolTip));
        OnPropertyChanged(nameof(RepositoryLabel));
        OnPropertyChanged(nameof(CloseButtonText));
        OnPropertyChanged(nameof(DefaultToastText));
    }

    protected override void OnClosed(EventArgs e)
    {
        _toastTimer.Stop();
        _toastTimer.Tick -= ToastTimer_Tick;
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
        base.OnClosed(e);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
