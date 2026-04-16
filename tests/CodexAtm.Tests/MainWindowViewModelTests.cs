using System.Globalization;
using CodexAtm.Core.Models;
using CodexAtm.Core.Services;
using CodexAtm.Core.ViewModels;

namespace CodexAtm.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Refresh_LoadsSessionsAndSelectsNothingByDefault()
    {
        using var _ = new UiCultureScope("zh-CN");
        var service = new FakeArchiveSessionService(
            [
                CreateSummary("a.jsonl", @"C:\work\a", "alpha task"),
                CreateSummary("b.jsonl", @"C:\work\b", "beta task")
            ]);
        var viewModel = new MainWindowViewModel(service);

        viewModel.Refresh();

        Assert.Equal(2, viewModel.Sessions.Count);
        Assert.Equal("已归档线程数：2", viewModel.StatusText);
        Assert.Null(viewModel.SelectedSession);
    }

    [Fact]
    public void SearchText_FiltersByPreviewAndCwd()
    {
        using var _ = new UiCultureScope("zh-CN");
        var service = new FakeArchiveSessionService(
            [
                CreateSummary("a.jsonl", @"C:\work\billing", "alpha task"),
                CreateSummary("b.jsonl", @"C:\work\ops", "beta preview")
            ]);
        var viewModel = new MainWindowViewModel(service);
        viewModel.Refresh();

        viewModel.SearchText = "billing";
        Assert.Single(viewModel.Sessions);
        Assert.Equal("符合过滤条件的线程数：1", viewModel.StatusText);

        viewModel.SearchText = "beta";
        Assert.Single(viewModel.Sessions);
        Assert.Equal("符合过滤条件的线程数：1", viewModel.StatusText);
        Assert.Equal("b.jsonl", viewModel.Sessions[0].FileName);
    }

    [Fact]
    public void SearchText_FiltersByDisplayTitle()
    {
        using var _ = new UiCultureScope("zh-CN");
        var service = new FakeArchiveSessionService(
            [
                CreateSummary("a.jsonl", @"C:\work\billing", "alpha task", "从 index 读取的标题"),
                CreateSummary("b.jsonl", @"C:\work\ops", "beta preview")
            ]);
        var viewModel = new MainWindowViewModel(service);
        viewModel.Refresh();

        viewModel.SearchText = "index 读取";

        var session = Assert.Single(viewModel.Sessions);
        Assert.Equal("a.jsonl", session.FileName);
    }

    [Fact]
    public void ClearingSearchText_RestoresArchivedCountStatusText()
    {
        using var _ = new UiCultureScope("zh-CN");
        var service = new FakeArchiveSessionService(
            [
                CreateSummary("a.jsonl", @"C:\work\billing", "alpha task"),
                CreateSummary("b.jsonl", @"C:\work\ops", "beta preview")
            ]);
        var viewModel = new MainWindowViewModel(service);
        viewModel.Refresh();

        viewModel.SearchText = "billing";
        viewModel.SearchText = string.Empty;

        Assert.Equal(2, viewModel.Sessions.Count);
        Assert.Equal("已归档线程数：2", viewModel.StatusText);
    }

    [Fact]
    public void DeleteSelectedSession_UpdatesStatusText()
    {
        using var _ = new UiCultureScope("zh-CN");
        var first = CreateSummary("a.jsonl", @"C:\work\a", "alpha task");
        var second = CreateSummary("b.jsonl", @"C:\work\b", "beta task");
        var service = new FakeArchiveSessionService([first, second]);
        var viewModel = new MainWindowViewModel(service);
        viewModel.Refresh();
        viewModel.SelectedSession = viewModel.Sessions[0];

        viewModel.DeleteSelectedSession(DeletionMode.Permanent);

        Assert.Single(viewModel.Sessions);
        Assert.Equal("已归档线程数：1", viewModel.StatusText);
    }

    [Fact]
    public void SelectedSession_LoadsDetail()
    {
        using var _ = new UiCultureScope("zh-CN");
        var summary = CreateSummary("a.jsonl", @"C:\work\a", "alpha task");
        var detail = new ArchiveSessionDetail
        {
            Summary = summary,
            Source = "vscode",
            Originator = "Codex Desktop",
            CliVersion = "0.1.0",
            Messages =
            [
                new ArchiveSessionMessage { Role = "user", Content = "hello" }
            ]
        };
        var service = new FakeArchiveSessionService([summary], detail);
        var viewModel = new MainWindowViewModel(service);
        viewModel.Refresh();

        viewModel.SelectedSession = viewModel.Sessions[0];

        Assert.NotNull(viewModel.SelectedSessionDetail);
        Assert.Single(viewModel.SelectedSessionDetail.Messages);
    }

    [Fact]
    public void Constructor_UsesProvidedThemeMode()
    {
        using var _ = new UiCultureScope("zh-CN");
        var service = new FakeArchiveSessionService([]);

        var viewModel = new MainWindowViewModel(service, ThemeMode.Dark);

        Assert.Equal(ThemeMode.Dark, viewModel.SelectedThemeMode);
        Assert.Equal(3, viewModel.ThemeModes.Count);
        Assert.Equal(LanguageMode.System, viewModel.SelectedLanguageMode);
        Assert.Equal(3, viewModel.LanguageModes.Count);
        Assert.Equal("已归档线程数：0", viewModel.StatusText);
    }

    [Fact]
    public void SessionSummary_UsesLastDirectoryNameAsGroupDisplayName()
    {
        using var _ = new UiCultureScope("zh-CN");
        var summary = CreateSummary("a.jsonl", @"C:\work\billing", "alpha task");

        Assert.Equal("billing", summary.GroupDisplayName);
    }

    [Fact]
    public void SessionSummary_UsesUngroupedLabelWhenCwdMissing()
    {
        using var _ = new UiCultureScope("zh-CN");
        var summary = CreateSummary("a.jsonl", string.Empty, "alpha task");

        Assert.Equal(ArchiveSessionSummary.UngroupedLabel, summary.GroupDisplayName);
    }

    [Fact]
    public void ViewModel_UsesEnglishTextsWhenUiCultureIsEnglish()
    {
        using var _ = new UiCultureScope("en-US");
        var service = new FakeArchiveSessionService(
            [
                CreateSummary("a.jsonl", @"C:\work\a", "alpha task")
            ]);
        var viewModel = new MainWindowViewModel(service, ThemeMode.Dark);

        viewModel.Refresh();
        viewModel.SearchText = "alpha";

        Assert.Equal("Filtered threads: 1", viewModel.StatusText);
        Assert.Equal("Ungrouped", CreateSummary("b.jsonl", string.Empty, "preview").GroupDisplayName);
        Assert.Equal("System", viewModel.ThemeModes[0].DisplayName);
        Assert.Equal("Light", viewModel.ThemeModes[1].DisplayName);
        Assert.Equal("Dark", viewModel.ThemeModes[2].DisplayName);
        Assert.Equal("System", viewModel.LanguageModes[0].DisplayName);
        Assert.Equal("Simplified Chinese", viewModel.LanguageModes[1].DisplayName);
        Assert.Equal("English", viewModel.LanguageModes[2].DisplayName);
    }

    [Fact]
    public void RefreshLocalizedText_UpdatesLanguageOptions()
    {
        using var english = new UiCultureScope("en-US");
        var service = new FakeArchiveSessionService([]);
        var viewModel = new MainWindowViewModel(service, ThemeMode.System, LanguageMode.System);

        using var chinese = new UiCultureScope("zh-CN");
        viewModel.RefreshLocalizedText();

        Assert.Equal("跟随系统", viewModel.LanguageModes[0].DisplayName);
        Assert.Equal("简体中文", viewModel.LanguageModes[1].DisplayName);
        Assert.Equal("英文", viewModel.LanguageModes[2].DisplayName);
        Assert.Equal("已归档线程数：0", viewModel.StatusText);
    }

    private static ArchiveSessionSummary CreateSummary(string fileName, string cwd, string preview, string threadTitle = "")
    {
        return new ArchiveSessionSummary
        {
            FilePath = Path.Combine(@"C:\archives", fileName),
            FileName = fileName,
            SessionId = fileName,
            Cwd = cwd,
            LastWriteTime = DateTimeOffset.Now,
            FileSize = 1,
            ThreadTitle = threadTitle,
            FirstUserMessagePreview = preview,
            ParseStatus = ArchiveParseStatus.Success,
            ParseError = string.Empty
        };
    }

    private sealed class FakeArchiveSessionService(
        IReadOnlyList<ArchiveSessionSummary> sessions,
        ArchiveSessionDetail? detail = null) : IArchiveSessionService
    {
        private readonly List<ArchiveSessionSummary> _sessions = [.. sessions];
        private readonly ArchiveSessionDetail? _detail = detail;

        public void DeleteSession(string filePath, DeletionMode deletionMode)
        {
            _sessions.RemoveAll(item => string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        }

        public ArchiveSessionDetail GetSessionDetail(string filePath)
        {
            return _detail ?? new ArchiveSessionDetail { Summary = _sessions[0] };
        }

        public IReadOnlyList<ArchiveSessionSummary> GetSessions()
        {
            return _sessions.ToArray();
        }
    }

    private sealed class UiCultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        public UiCultureScope(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}
