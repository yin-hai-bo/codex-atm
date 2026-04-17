using System.Collections.ObjectModel;
using System.Threading;
using CodexAtm.Core.Localization;
using CodexAtm.Core.Models;
using CodexAtm.Core.Services;

namespace CodexAtm.Core.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IArchiveSessionService _archiveSessionService;
    private readonly RelayCommand _refreshCommand;
    private string _searchText = string.Empty;
    private string _statusText = CoreText.ArchivedSessionCount(0);
    private ArchiveSessionSummary? _selectedSession;
    private ArchiveSessionDetail? _selectedSessionDetail;
    private ThemeMode _selectedThemeMode;
    private LanguageMode _selectedLanguageMode;
    private bool _isBusy;
    private IReadOnlyList<ArchiveSessionSummary> _allSessions = [];
    private CancellationTokenSource? _refreshCancellationTokenSource;
    private int _refreshVersion;

    public MainWindowViewModel(
        IArchiveSessionService archiveSessionService,
        ThemeMode initialThemeMode = ThemeMode.System,
        LanguageMode initialLanguageMode = LanguageMode.System)
    {
        _archiveSessionService = archiveSessionService;
        _refreshCommand = new RelayCommand(Refresh, () => !IsBusy);
        _selectedThemeMode = initialThemeMode;
        _selectedLanguageMode = initialLanguageMode;
        ThemeModes =
        [
            new ThemeModeOption(ThemeMode.System),
            new ThemeModeOption(ThemeMode.Light),
            new ThemeModeOption(ThemeMode.Dark)
        ];
        LanguageModes =
        [
            new LanguageModeOption(LanguageMode.System),
            new LanguageModeOption(LanguageMode.SimplifiedChinese),
            new LanguageModeOption(LanguageMode.English)
        ];
    }

    public ObservableCollection<ArchiveSessionSummary> Sessions { get; } = [];

    public IReadOnlyList<ThemeModeOption> ThemeModes { get; }

    public IReadOnlyList<LanguageModeOption> LanguageModes { get; }

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
        set
        {
            if (SetProperty(ref _selectedThemeMode, value))
            {
                OnPropertyChanged(nameof(SelectedThemeOption));
            }
        }
    }

    public LanguageMode SelectedLanguageMode
    {
        get => _selectedLanguageMode;
        set
        {
            if (SetProperty(ref _selectedLanguageMode, value))
            {
                OnPropertyChanged(nameof(SelectedLanguageOption));
            }
        }
    }

    public ThemeModeOption? SelectedThemeOption
    {
        get => ThemeModes.FirstOrDefault(item => item.Mode == SelectedThemeMode);
        set
        {
            if (value is null)
            {
                return;
            }

            SelectedThemeMode = value.Mode;
        }
    }

    public LanguageModeOption? SelectedLanguageOption
    {
        get => LanguageModes.FirstOrDefault(item => item.Mode == SelectedLanguageMode);
        set
        {
            if (value is null)
            {
                return;
            }

            SelectedLanguageMode = value.Mode;
        }
    }

    public bool CanDeleteSelectedSession => !IsBusy && SelectedSession is not null;

    public async Task RefreshAsync()
    {
        var previousSelectionPath = SelectedSession?.FilePath;
        var refreshVersion = Interlocked.Increment(ref _refreshVersion);
        var refreshCancellationTokenSource = new CancellationTokenSource();
        var previousCancellationTokenSource = Interlocked.Exchange(ref _refreshCancellationTokenSource, refreshCancellationTokenSource);
        previousCancellationTokenSource?.Cancel();
        previousCancellationTokenSource?.Dispose();

        IsBusy = true;
        StatusText = CoreText.LoadingArchivedSessions;
        try
        {
            _allSessions = await _archiveSessionService.GetSessionsAsync(refreshCancellationTokenSource.Token);
            if (!IsLatestRefresh(refreshVersion, refreshCancellationTokenSource))
            {
                return;
            }

            ApplyFilter(previousSelectionPath);
        }
        catch (OperationCanceledException) when (!IsLatestRefresh(refreshVersion, refreshCancellationTokenSource))
        {
        }
        finally
        {
            if (IsLatestRefresh(refreshVersion, refreshCancellationTokenSource))
            {
                _refreshCancellationTokenSource = null;
                IsBusy = false;
            }

            refreshCancellationTokenSource.Dispose();
        }
    }

    public async Task DeleteSelectedSessionAsync(DeletionMode deletionMode)
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
        }
        catch
        {
            IsBusy = false;
            throw;
        }

        await RefreshAsync();
    }

    public void RefreshLocalizedText()
    {
        foreach (var themeMode in ThemeModes)
        {
            themeMode.RefreshLocalizedText();
        }

        foreach (var languageMode in LanguageModes)
        {
            languageMode.RefreshLocalizedText();
        }

        if (IsBusy)
        {
            StatusText = CoreText.LoadingArchivedSessions;
        }
        else
        {
            UpdateStatusText(SearchText.Trim());
        }

        OnPropertyChanged(nameof(SelectedThemeOption));
        OnPropertyChanged(nameof(SelectedLanguageOption));
        OnPropertyChanged(nameof(SelectedSession));
        OnPropertyChanged(nameof(SelectedSessionDetail));
    }

    private async void Refresh()
    {
        await RefreshAsync();
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
            ? CoreText.ArchivedSessionCount(Sessions.Count)
            : CoreText.FilteredSessionCount(Sessions.Count);
    }

    private bool IsLatestRefresh(int refreshVersion, CancellationTokenSource refreshCancellationTokenSource)
    {
        return refreshVersion == _refreshVersion
            && ReferenceEquals(_refreshCancellationTokenSource, refreshCancellationTokenSource);
    }
}
