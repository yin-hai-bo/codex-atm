using CodexAtm.Core.Models;
using CodexAtm.Core.Services;
using CodexAtm.Core.ViewModels;

namespace CodexAtm.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Refresh_LoadsSessionsAndSelectsNothingByDefault()
    {
        var service = new FakeArchiveSessionService(
            [
                CreateSummary("a.jsonl", @"C:\work\a", "alpha task"),
                CreateSummary("b.jsonl", @"C:\work\b", "beta task")
            ]);
        var viewModel = new MainWindowViewModel(service);

        viewModel.Refresh();

        Assert.Equal(2, viewModel.Sessions.Count);
        Assert.Null(viewModel.SelectedSession);
    }

    [Fact]
    public void SearchText_FiltersByPreviewAndCwd()
    {
        var service = new FakeArchiveSessionService(
            [
                CreateSummary("a.jsonl", @"C:\work\billing", "alpha task"),
                CreateSummary("b.jsonl", @"C:\work\ops", "beta preview")
            ]);
        var viewModel = new MainWindowViewModel(service);
        viewModel.Refresh();

        viewModel.SearchText = "billing";
        Assert.Single(viewModel.Sessions);

        viewModel.SearchText = "beta";
        Assert.Single(viewModel.Sessions);
        Assert.Equal("b.jsonl", viewModel.Sessions[0].FileName);
    }

    [Fact]
    public void SelectedSession_LoadsDetail()
    {
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

    private static ArchiveSessionSummary CreateSummary(string fileName, string cwd, string preview)
    {
        return new ArchiveSessionSummary
        {
            FilePath = Path.Combine(@"C:\archives", fileName),
            FileName = fileName,
            SessionId = fileName,
            Cwd = cwd,
            LastWriteTime = DateTimeOffset.Now,
            FileSize = 1,
            FirstUserMessagePreview = preview,
            ParseStatus = ArchiveParseStatus.Success,
            ParseError = string.Empty
        };
    }

    private sealed class FakeArchiveSessionService(
        IReadOnlyList<ArchiveSessionSummary> sessions,
        ArchiveSessionDetail? detail = null) : IArchiveSessionService
    {
        private readonly IReadOnlyList<ArchiveSessionSummary> _sessions = sessions;
        private readonly ArchiveSessionDetail? _detail = detail;

        public void DeleteSession(string filePath, DeletionMode deletionMode)
        {
        }

        public ArchiveSessionDetail GetSessionDetail(string filePath)
        {
            return _detail ?? new ArchiveSessionDetail { Summary = _sessions[0] };
        }

        public IReadOnlyList<ArchiveSessionSummary> GetSessions()
        {
            return _sessions;
        }
    }
}
