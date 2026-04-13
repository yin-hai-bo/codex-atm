using System.Runtime.InteropServices;
using CodexAtm.Core.Models;
using CodexAtm.Core.Services;

namespace CodexAtm.Tests;

public sealed class ArchiveSessionServiceTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly string _archivedSessionsDirectory;
    private readonly string _threadStateDatabasePath;

    public ArchiveSessionServiceTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "CodexAtm.Tests", Guid.NewGuid().ToString("N"));
        _archivedSessionsDirectory = Path.Combine(_rootDirectory, "archived_sessions");
        _threadStateDatabasePath = Path.Combine(_rootDirectory, "state_5.sqlite");
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
    public void GetSessions_UsesThreadTitleFromSessionIndex()
    {
        WriteThreadStateDatabase(
            """
            create table if not exists threads (
                id text primary key,
                rollout_path text not null,
                created_at integer not null,
                updated_at integer not null,
                source text not null,
                model_provider text not null,
                cwd text not null,
                title text not null,
                sandbox_policy text not null,
                approval_mode text not null,
                tokens_used integer not null default 0,
                has_user_event integer not null default 0,
                archived integer not null default 0,
                archived_at integer null,
                git_sha text null,
                git_branch text null,
                git_origin_url text null,
                cli_version text not null default '',
                first_user_message text not null default '',
                agent_nickname text null,
                agent_role text null,
                memory_mode text not null default 'enabled',
                model text null,
                reasoning_effort text null,
                agent_path text null
            );
            delete from threads;
            insert into threads (
                id, rollout_path, created_at, updated_at, source, model_provider, cwd, title,
                sandbox_policy, approval_mode, tokens_used, has_user_event, archived, archived_at,
                cli_version, first_user_message, memory_mode
            ) values (
                'session-9', 'ignored', 0, 0, 'vscode', 'openai', 'C:\\work\\demo', 'title from sqlite',
                '{}', 'on-request', 0, 0, 1, 0,
                '0.119.0-alpha.11', 'preview should not be title', 'enabled'
            );
            """);

        WriteArchiveFile(
            "session-title.jsonl",
            """
            {"timestamp":"2026-04-08T03:45:08.420Z","type":"session_meta","payload":{"id":"session-9","cwd":"C:\\work\\demo","originator":"Codex Desktop","cli_version":"0.118.0","source":"vscode"}}
            {"timestamp":"2026-04-08T03:45:08.426Z","type":"response_item","payload":{"type":"message","role":"user","content":[{"type":"input_text","text":"preview should not be title"}]}}
            """);

        var service = new ArchiveSessionService(_archivedSessionsDirectory, _threadStateDatabasePath);

        var session = Assert.Single(service.GetSessions());
        Assert.Equal("title from sqlite", session.ThreadTitle);
        Assert.Equal("title from sqlite", session.DisplayTitle);
        Assert.Equal("preview should not be title", session.FirstUserMessagePreview);
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
    public void GetSessions_PrefersHumanReadablePreviewOverEnvironmentContext()
    {
        WriteArchiveFile(
            "environment.jsonl",
            """
            {"timestamp":"2026-04-08T03:45:08.420Z","type":"session_meta","payload":{"id":"session-6","cwd":"C:\\work\\demo","originator":"Codex Desktop","cli_version":"0.118.0","source":"vscode"}}
            {"timestamp":"2026-04-08T03:45:08.426Z","type":"response_item","payload":{"type":"message","role":"user","content":[{"type":"input_text","text":"<environment_context>\n  <cwd>C:\\work\\dwp-detector</cwd>\n</environment_context>\n请帮我排查登录失败的原因"}]}}
            """);

        var service = new ArchiveSessionService(_archivedSessionsDirectory);

        var session = Assert.Single(service.GetSessions());
        Assert.Equal("请帮我排查登录失败的原因", session.FirstUserMessagePreview);
    }

    [Fact]
    public void GetSessions_SkipsEnvironmentOnlyUserMessageAndUsesNextRealPrompt()
    {
        WriteArchiveFile(
            "environment-only-first.jsonl",
            """
            {"timestamp":"2026-04-08T03:45:08.420Z","type":"session_meta","payload":{"id":"session-7","cwd":"C:\\work\\demo","originator":"Codex Desktop","cli_version":"0.118.0","source":"vscode"}}
            {"timestamp":"2026-04-08T03:45:08.426Z","type":"response_item","payload":{"type":"message","role":"user","content":[{"type":"input_text","text":"<environment_context>\n  <cwd>C:\\work\\dwp-detector</cwd>\n</environment_context>"}]}}
            {"timestamp":"2026-04-08T03:45:09.000Z","type":"response_item","payload":{"type":"message","role":"user","content":[{"type":"input_text","text":"检查项目，排查可能的缺陷"}]}}
            """);

        var service = new ArchiveSessionService(_archivedSessionsDirectory);

        var session = Assert.Single(service.GetSessions());
        Assert.Equal("检查项目，排查可能的缺陷", session.FirstUserMessagePreview);
    }

    [Fact]
    public void GetSessions_UsesMyRequestSectionAsPreviewWhenPresent()
    {
        WriteArchiveFile(
            "review-findings.jsonl",
            """
            {"timestamp":"2026-04-08T03:45:08.420Z","type":"session_meta","payload":{"id":"session-8","cwd":"C:\\work\\demo","originator":"Codex Desktop","cli_version":"0.118.0","source":"vscode"}}
            {"timestamp":"2026-04-08T03:45:08.426Z","type":"response_item","payload":{"type":"message","role":"user","content":[{"type":"input_text","text":"# Review findings:\n\n## Finding 1\n问题描述\n\n## My request for Codex:\n解决你提到的第2个问题"}]}}
            """);

        var service = new ArchiveSessionService(_archivedSessionsDirectory);

        var session = Assert.Single(service.GetSessions());
        Assert.Equal("解决你提到的第2个问题", session.FirstUserMessagePreview);
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

    private void WriteThreadStateDatabase(string sql)
    {
        if (File.Exists(_threadStateDatabasePath))
        {
            File.Delete(_threadStateDatabasePath);
        }

        TestSqlite.ExecuteNonQuery(_threadStateDatabasePath, sql);
    }

    private static class TestSqlite
    {
        private const int SQLITE_OK = 0;
        private const int SQLITE_OPEN_READWRITE = 0x00000002;
        private const int SQLITE_OPEN_CREATE = 0x00000004;

        public static void ExecuteNonQuery(string databasePath, string sql)
        {
            nint databaseHandle = 0;
            nint errorMessage = 0;
            try
            {
                ThrowIfNeeded(sqlite3_open_v2(databasePath, out databaseHandle, SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE, nint.Zero), nint.Zero);
                ThrowIfNeeded(sqlite3_exec(databaseHandle, sql, nint.Zero, nint.Zero, out errorMessage), errorMessage);
            }
            finally
            {
                if (errorMessage != 0)
                {
                    sqlite3_free(errorMessage);
                }

                if (databaseHandle != 0)
                {
                    sqlite3_close(databaseHandle);
                }
            }
        }

        private static void ThrowIfNeeded(int resultCode, nint errorMessage)
        {
            if (resultCode == SQLITE_OK)
            {
                return;
            }

            var message = errorMessage == 0 ? string.Empty : Marshal.PtrToStringAnsi(errorMessage) ?? string.Empty;
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? $"SQLite error {resultCode}." : message);
        }

        [DllImport("winsqlite3", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_open_v2(string filename, out nint database, int flags, nint vfs);

        [DllImport("winsqlite3", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_exec(nint database, string sql, nint callback, nint callbackArg, out nint errorMessage);

        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_close(nint database);

        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern void sqlite3_free(nint pointer);
    }
}
