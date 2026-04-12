using System.ComponentModel;
using System.IO;
using System.Windows;
using CodexAtm.Core.Models;
using CodexAtm.Core.Services;
using CodexAtm.Core.ViewModels;

namespace CodexAtm.App;

public partial class ArchiveManagerWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ThemeService _themeService;

    public ArchiveManagerWindow()
    {
        InitializeComponent();
        _themeService = ((App)Application.Current).ThemeService;
        _viewModel = new MainWindowViewModel(
            new ArchiveSessionService(GetArchivedSessionsDirectory()),
            _themeService.CurrentThemeMode);
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        Loaded += (_, _) => _viewModel.Refresh();
        Closed += (_, _) => _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
    }

    private void RecycleSelectedSession_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedSession(DeletionMode.RecycleBin, "确定要将所选归档线程移到回收站吗？");
    }

    private void DeleteSelectedSession_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedSession(DeletionMode.Permanent, "确定要永久删除所选归档线程吗？此操作不可恢复。");
    }

    private void DeleteSelectedSession(DeletionMode deletionMode, string message)
    {
        if (_viewModel.SelectedSession is null)
        {
            return;
        }

        var result = MessageBox.Show(
            message,
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _viewModel.DeleteSelectedSession(deletionMode);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedThemeMode))
        {
            _themeService.SetThemeMode(_viewModel.SelectedThemeMode);
        }
    }

    private static string GetArchivedSessionsDirectory()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".codex", "archived_sessions");
    }
}
