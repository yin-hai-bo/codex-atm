using System.Collections.ObjectModel;
using CodexAtm.Core.Models;
using CodexAtm.Core.Services;

namespace CodexAtm.Core.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IArchiveSessionService _archiveSessionService;
    private readonly RelayCommand _refreshCommand;
    private string _searchText = string.Empty;
    private string _statusText = "已归档线程数：0";
    private ArchiveSessionSummary? _selectedSession;
    private ArchiveSessionDetail? _selectedSessionDetail;
    private ThemeMode _selectedThemeMode;
    private bool _isBusy;
    private IReadOnlyList<ArchiveSessionSummary> _allSessions = [];

    public MainWindowViewModel(IArchiveSessionService archiveSessionService, ThemeMode initialThemeMode = ThemeMode.System)
    {
        _archiveSessionService = archiveSessionService;
        _refreshCommand = new RelayCommand(Refresh, () => !IsBusy);
        _selectedThemeMode = initialThemeMode;
    }

    public ObservableCollection<ArchiveSessionSummary> Sessions { get; } = [];

    public IReadOnlyList<ThemeModeOption> ThemeModes { get; } = ThemeModeOption.DefaultOptions;

    public RelayCommand RefreshCommand => _refreshCommand;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
            {
                return;
            }

            ApplyFilter();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            _refreshCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanDeleteSelectedSession));
        }
    }

    public ArchiveSessionSummary? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (!SetProperty(ref _selectedSession, value))
            {
                return;
            }

            LoadSelectedSessionDetail();
            OnPropertyChanged(nameof(CanDeleteSelectedSession));
        }
    }

    public ArchiveSessionDetail? SelectedSessionDetail
    {
        get => _selectedSessionDetail;
        private set => SetProperty(ref _selectedSessionDetail, value);
    }

    public ThemeMode SelectedThemeMode
    {
        get => _selectedThemeMode;
        set => SetProperty(ref _selectedThemeMode, value);
    }

    public bool CanDeleteSelectedSession => !IsBusy && SelectedSession is not null;

    public void Refresh()
    {
        IsBusy = true;
        try
        {
            var previousSelectionPath = SelectedSession?.FilePath;
            _allSessions = _archiveSessionService.GetSessions();
            ApplyFilter(previousSelectionPath);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void DeleteSelectedSession(DeletionMode deletionMode)
    {
        if (SelectedSession is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            _archiveSessionService.DeleteSession(SelectedSession.FilePath, deletionMode);
            SelectedSession = null;
            SelectedSessionDetail = null;
            _allSessions = _archiveSessionService.GetSessions();
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadSelectedSessionDetail()
    {
        if (SelectedSession is null)
        {
            SelectedSessionDetail = null;
            return;
        }

        SelectedSessionDetail = _archiveSessionService.GetSessionDetail(SelectedSession.FilePath);
    }

    private void ApplyFilter(string? preferredSelectionPath = null)
    {
        var keyword = SearchText.Trim();
        var filtered = string.IsNullOrWhiteSpace(keyword)
            ? _allSessions
            : _allSessions.Where(item =>
                Contains(item.GroupDisplayName, keyword) ||
                Contains(item.DisplayTitle, keyword) ||
                Contains(item.FileName, keyword) ||
                Contains(item.Cwd, keyword) ||
                Contains(item.FirstUserMessagePreview, keyword))
                .ToArray();

        Sessions.Clear();
        foreach (var session in filtered)
        {
            Sessions.Add(session);
        }

        UpdateStatusText(keyword);

        var nextSelection = !string.IsNullOrWhiteSpace(preferredSelectionPath)
            ? Sessions.FirstOrDefault(item => string.Equals(item.FilePath, preferredSelectionPath, StringComparison.OrdinalIgnoreCase))
            : null;

        if (nextSelection is null && SelectedSession is not null)
        {
            nextSelection = Sessions.FirstOrDefault(item => string.Equals(item.FilePath, SelectedSession.FilePath, StringComparison.OrdinalIgnoreCase));
        }

        SelectedSession = nextSelection;
    }

    private static bool Contains(string source, string keyword)
    {
        return source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateStatusText(string keyword)
    {
        StatusText = string.IsNullOrWhiteSpace(keyword)
            ? $"已归档线程数：{Sessions.Count}"
            : $"符合过滤条件的线程数：{Sessions.Count}";
    }
}
