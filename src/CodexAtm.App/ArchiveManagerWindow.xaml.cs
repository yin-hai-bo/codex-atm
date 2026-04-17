using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.ComponentModel;
using System.Collections.Generic;
using CodexAtm.Core.Models;
using CodexAtm.Core.Services;
using CodexAtm.Core.ViewModels;

namespace CodexAtm.App;

public partial class ArchiveManagerWindow : Window, INotifyPropertyChanged
{
    private const int GCLP_HICON = -14;
    private const int GCLP_HICONSM = -34;
    private readonly MainWindowViewModel _viewModel;
    private readonly LocalizationService _localizationService;
    private readonly ThemeService _themeService;
    private readonly Dictionary<string, bool> _groupExpansionStates = new(StringComparer.OrdinalIgnoreCase);
    private nint _smallIconHandle;
    private nint _largeIconHandle;

    public ArchiveManagerWindow()
    {
        InitializeComponent();
        _themeService = ((App)Application.Current).ThemeService;
        _localizationService = ((App)Application.Current).LocalizationService;
        _viewModel = new MainWindowViewModel(
            new ArchiveSessionService(GetArchivedSessionsDirectory()),
            _themeService.CurrentThemeMode,
            _localizationService.CurrentLanguageMode);
        DataContext = _viewModel;
        ConfigureSessionGrouping();
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoadedAsync;
        Closed += (_, _) =>
        {
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
            ReleaseTaskbarIcons();
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WindowTitle => AppVersionInfo.WindowTitle;

    public string ProductName => AppText.ProductName;

    public string MainWindowDescription => AppText.MainWindowDescription;

    public string CloseApplicationToolTip => AppText.CloseApplicationToolTip;

    public string SessionListTitle => AppText.SessionListTitle;

    public string RefreshButtonText => AppText.RefreshButton;

    public string NoSessionSelectedText => AppText.NoSessionSelected;

    public string DetailPreviewTitle => AppText.DetailPreviewTitle;

    public string SessionIdLabel => AppText.SessionIdLabel;

    public string FilePathLabel => AppText.FilePathLabel;

    public string WorkingDirectoryLabel => AppText.WorkingDirectoryLabel;

    public string LastModifiedLabel => AppText.LastModifiedLabel;

    public string FileSizeLabel => AppText.FileSizeLabel;

    public string SourceLabel => AppText.SourceLabel;

    public string RecentMessagesTitle => AppText.RecentMessagesTitle;

    public string MoveToRecycleBinButtonText => AppText.MoveToRecycleBinButton;

    public string DeletePermanentlyButtonText => AppText.DeletePermanentlyButton;

    public string FilterHintText => AppText.FilterHint;

    private void RecycleSelectedSession_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedSession(DeletionMode.RecycleBin, AppText.ConfirmRecycleMessage);
    }

    private void DeleteSelectedSession_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedSession(DeletionMode.Permanent, AppText.ConfirmPermanentDeleteMessage);
    }

    private void AppTitle_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowAboutDialog();
        e.Handled = true;
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void WindowRootGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount > 1)
        {
            return;
        }

        if (e.OriginalSource is not DependencyObject dependencyObject || !CanStartWindowDrag(dependencyObject))
        {
            return;
        }

        DragMove();
        e.Handled = true;
    }

    private void DeleteSelectedSession(DeletionMode deletionMode, string message)
    {
        _ = DeleteSelectedSessionAsync(deletionMode, message);
    }

    private async Task DeleteSelectedSessionAsync(DeletionMode deletionMode, string message)
    {
        if (_viewModel.SelectedSession is null)
        {
            return;
        }

        var result = MessageBox.Show(
            message,
            AppText.ConfirmDeleteTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _viewModel.DeleteSelectedSessionAsync(deletionMode);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, AppText.DeleteFailedTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedThemeMode))
        {
            _themeService.SetThemeMode(_viewModel.SelectedThemeMode);
        }

        if (e.PropertyName == nameof(MainWindowViewModel.SelectedLanguageMode))
        {
            _localizationService.SetLanguageMode(_viewModel.SelectedLanguageMode);
        }
    }

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
    {
        _viewModel.RefreshLocalizedText();
        CollectionViewSource.GetDefaultView(_viewModel.Sessions).Refresh();
        RaiseLocalizedPropertyChanges();
    }

    private void ConfigureSessionGrouping()
    {
        var view = CollectionViewSource.GetDefaultView(_viewModel.Sessions);
        if (view.GroupDescriptions.OfType<PropertyGroupDescription>()
            .Any(item => string.Equals(item.PropertyName, nameof(ArchiveSessionSummary.GroupDisplayName), StringComparison.Ordinal)))
        {
            return;
        }

        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ArchiveSessionSummary.GroupDisplayName)));
    }

    private void SessionGroupExpander_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Expander expander || TryGetGroupKey(expander) is not { } groupKey)
        {
            return;
        }

        if (_groupExpansionStates.TryGetValue(groupKey, out var isExpanded))
        {
            expander.IsExpanded = isExpanded;
            return;
        }

        _groupExpansionStates[groupKey] = expander.IsExpanded;
    }

    private void SessionGroupExpander_Expanded(object sender, RoutedEventArgs e)
    {
        UpdateGroupExpansionState(sender, isExpanded: true);
    }

    private void SessionGroupExpander_Collapsed(object sender, RoutedEventArgs e)
    {
        UpdateGroupExpansionState(sender, isExpanded: false);
    }

    private static string GetArchivedSessionsDirectory()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".codex", "archived_sessions");
    }

    private void ShowAboutDialog()
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };

        aboutWindow.ShowDialog();
    }

    private bool CanStartWindowDrag(DependencyObject source)
    {
        return !HasAncestor(source, AppTitleTextBlock)
            && !HasAncestor(source, SessionListPanel)
            && !HasAncestor(source, SessionDetailPanel)
            && !HasAncestor(source, BottomStatusPanel)
            && !IsInteractiveElement(source);
    }

    private static bool HasAncestor(DependencyObject source, DependencyObject target)
    {
        for (var current = source; current is not null; current = GetParent(current))
        {
            if (ReferenceEquals(current, target))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInteractiveElement(DependencyObject source)
    {
        for (var current = source; current is not null; current = GetParent(current))
        {
            if (current is ButtonBase
                or TextBoxBase
                or ComboBox
                or ListView
                or ListViewItem
                or ScrollViewer
                or Expander
                or Selector
                or Thumb)
            {
                return true;
            }
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject source)
    {
        return source switch
        {
            Visual visual => VisualTreeHelper.GetParent(visual),
            Visual3D visual3D => VisualTreeHelper.GetParent(visual3D),
            FrameworkContentElement frameworkContentElement => frameworkContentElement.Parent,
            _ => null
        };
    }

    private void RaiseLocalizedPropertyChanges()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(ProductName));
        OnPropertyChanged(nameof(MainWindowDescription));
        OnPropertyChanged(nameof(CloseApplicationToolTip));
        OnPropertyChanged(nameof(SessionListTitle));
        OnPropertyChanged(nameof(RefreshButtonText));
        OnPropertyChanged(nameof(NoSessionSelectedText));
        OnPropertyChanged(nameof(DetailPreviewTitle));
        OnPropertyChanged(nameof(SessionIdLabel));
        OnPropertyChanged(nameof(FilePathLabel));
        OnPropertyChanged(nameof(WorkingDirectoryLabel));
        OnPropertyChanged(nameof(LastModifiedLabel));
        OnPropertyChanged(nameof(FileSizeLabel));
        OnPropertyChanged(nameof(SourceLabel));
        OnPropertyChanged(nameof(RecentMessagesTitle));
        OnPropertyChanged(nameof(MoveToRecycleBinButtonText));
        OnPropertyChanged(nameof(DeletePermanentlyButtonText));
        OnPropertyChanged(nameof(FilterHintText));
    }

    private void UpdateGroupExpansionState(object sender, bool isExpanded)
    {
        if (sender is not Expander expander || TryGetGroupKey(expander) is not { } groupKey)
        {
            return;
        }

        _groupExpansionStates[groupKey] = isExpanded;
    }

    private static string? TryGetGroupKey(Expander expander)
    {
        return expander.DataContext is CollectionViewGroup collectionViewGroup && collectionViewGroup.Name is not null
            ? collectionViewGroup.Name.ToString()
            : null;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ApplyTaskbarIcon();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, AppText.RefreshButton, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyTaskbarIcon()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return;
        }

        var largeIcons = new nint[1];
        var smallIcons = new nint[1];
        if (ExtractIconEx(executablePath, 0, largeIcons, smallIcons, 1) == 0)
        {
            return;
        }

        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle == nint.Zero)
        {
            DestroyExtractedIcons(largeIcons[0], smallIcons[0]);
            return;
        }

        ReleaseTaskbarIcons();
        _largeIconHandle = largeIcons[0];
        _smallIconHandle = smallIcons[0];

        if (_smallIconHandle != nint.Zero)
        {
            SetClassLongPtr(windowHandle, GCLP_HICONSM, _smallIconHandle);
        }

        if (_largeIconHandle != nint.Zero)
        {
            SetClassLongPtr(windowHandle, GCLP_HICON, _largeIconHandle);
        }
    }

    private void ReleaseTaskbarIcons()
    {
        DestroyExtractedIcons(_largeIconHandle, _smallIconHandle);
        _largeIconHandle = nint.Zero;
        _smallIconHandle = nint.Zero;
    }

    private static void DestroyExtractedIcons(nint largeIconHandle, nint smallIconHandle)
    {
        if (smallIconHandle != nint.Zero)
        {
            DestroyIcon(smallIconHandle);
        }

        if (largeIconHandle != nint.Zero)
        {
            DestroyIcon(largeIconHandle);
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [DllImport("shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(
        string fileName,
        int iconIndex,
        [Out] nint[] largeIcons,
        [Out] nint[] smallIcons,
        uint iconCount);

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW", SetLastError = true)]
    private static extern nint SetClassLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint hIcon);
}
