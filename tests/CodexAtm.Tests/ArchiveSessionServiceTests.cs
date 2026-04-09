using CodexAtm.Core.Models;
using CodexAtm.Core.Services;

namespace CodexAtm.Tests;

public sealed class ArchiveSessionServiceTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly string _archivedSessionsDirectory;

    public ArchiveSessionServiceTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "CodexAtm.Tests", Guid.NewGuid().ToString("N"));
        _archivedSessionsDirectory = Path.Combine(_rootDirectory, "archived_sessions");
        Directory.CreateDirectory(_archivedSessionsDirectory);
    }

    [Fact]
    public void GetSessions_ParsesSummaryFromJsonl()
    {
        var filePath = WriteArchiveFile(
            "session.jsonl",
            """
            {"timestamp":"2026-04-08T03:45:08.420Z","type":"session_meta","payload":{"id":"session-1","cwd":"C:\\work\\demo","originator":"Codex Desktop","cli_version":"0.118.0","source":"vscode"}}
            {"timestamp":"2026-04-08T03:45:08.426Z","type":"response_item","payload":{"type":"message","role":"user","content":[{"type":"input_text","text":"请帮我检查 first_run 埋点。"}]}}
            {"timestamp":"2026-04-08T03:45:10.000Z","type":"response_item","payload":{"type":"message","role":"assistant","content":[{"type":"output_text","text":"我先看实现。"}]}}
            """);

        var service = new ArchiveSessionService(_archivedSessionsDirectory);
        var sessions = service.GetSessions();

        var session = Assert.Single(sessions);
        Assert.Equal(filePath, session.FilePath);
        Assert.Equal("session-1", session.SessionId);
        Assert.Equal(@"C:\work\demo", session.Cwd);
        Assert.Equal("请帮我检查 first_run 埋点。", session.FirstUserMessagePreview);
        Assert.Equal(ArchiveParseStatus.Success, session.ParseStatus);
    }

    [Fact]
    public void GetSessionDetail_ReturnsRecentMessages()
    {
        var filePath = WriteArchiveFile(
            "detail.jsonl",
            """
            {"timestamp":"2026-04-08T03:45:08.420Z","type":"session_meta","payload":{"id":"session-2","cwd":"C:\\work\\demo","originator":"Codex Desktop","cli_version":"0.118.0","source":"vscode"}}
            {"timestamp":"2026-04-08T03:45:08.426Z","type":"response_item","payload":{"type":"message","role":"user","content":[{"type":"input_text","text":"第一条用户消息"}]}}
            {"timestamp":"2026-04-08T03:45:10.000Z","type":"response_item","payload":{"type":"message","role":"assistant","content":[{"type":"output_text","text":"第一条助手消息"}]}}
            {"timestamp":"2026-04-08T03:45:12.000Z","type":"response_item","payload":{"type":"message","role":"developer","content":[{"type":"text","text":"开发者消息"}]}}
            """);

        var service = new ArchiveSessionService(_archivedSessionsDirectory);
        var detail = service.GetSessionDetail(filePath);

        Assert.Equal("session-2", detail.Summary.SessionId);
        Assert.Equal(3, detail.Messages.Count);
        Assert.Equal("user", detail.Messages[0].Role);
        Assert.Equal("第一条用户消息", detail.Messages[0].Content);
        Assert.Equal("assistant", detail.Messages[1].Role);
        Assert.Equal("developer", detail.Messages[2].Role);
    }

    [Fact]
    public void GetSessions_HandlesBrokenJsonWithoutFailingWholeList()
    {
        WriteArchiveFile(
            "broken.jsonl",
            """
            {"timestamp":"2026-04-08T03:45:08.420Z","type":"session_meta","payload":{"id":"session-3","cwd":"C:\\work\\demo","originator":"Codex Desktop","cli_version":"0.118.0","source":"vscode"}}
            {not-json}
            {"timestamp":"2026-04-08T03:45:08.426Z","type":"response_item","payload":{"type":"message","role":"user","content":[{"type":"input_text","text":"broken line after meta"}]}}
            """);

        var service = new ArchiveSessionService(_archivedSessionsDirectory);
        var session = Assert.Single(service.GetSessions());
        Assert.Equal(ArchiveParseStatus.Partial, session.ParseStatus);
        Assert.Equal("broken line after meta", session.FirstUserMessagePreview);
    }

    [Fact]
    public void DeleteSession_Permanent_RemovesFile()
    {
        var filePath = WriteArchiveFile("delete.jsonl", """{"type":"session_meta","payload":{"id":"session-4"}}""");
        var service = new ArchiveSessionService(_archivedSessionsDirectory);

        service.DeleteSession(filePath, DeletionMode.Permanent);

        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void DeleteSession_RejectsPathOutsideArchiveDirectory()
    {
        var outsideFile = Path.Combine(_rootDirectory, "outside.jsonl");
        File.WriteAllText(outsideFile, "{}", System.Text.Encoding.UTF8);
        var service = new ArchiveSessionService(_archivedSessionsDirectory);

        var ex = Assert.Throws<InvalidOperationException>(() => service.DeleteSession(outsideFile, DeletionMode.Permanent));
        Assert.Contains("archived_sessions", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeleteSession_RecycleBin_RemovesFileFromOriginalPath()
    {
        var filePath = WriteArchiveFile("recycle.jsonl", """{"type":"session_meta","payload":{"id":"session-5"}}""");
        var service = new ArchiveSessionService(_archivedSessionsDirectory);

        service.DeleteSession(filePath, DeletionMode.RecycleBin);

        Assert.False(File.Exists(filePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, true);
        }
    }

    private string WriteArchiveFile(string fileName, string content)
    {
        var filePath = Path.Combine(_archivedSessionsDirectory, fileName);
        File.WriteAllText(filePath, content.Replace("\n", Environment.NewLine), System.Text.Encoding.UTF8);
        return filePath;
    }
}
