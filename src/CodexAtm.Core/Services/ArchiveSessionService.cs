using System.Text;
using System.Text.Json;
using CodexAtm.Core.Models;
using Microsoft.VisualBasic.FileIO;
using SearchOption = System.IO.SearchOption;

namespace CodexAtm.Core.Services;

public sealed class ArchiveSessionService(string archivedSessionsDirectory) : IArchiveSessionService
{
    private const int MAX_PREVIEW_LENGTH = 140;
    private const int MAX_RECENT_MESSAGES = 8;
    private readonly string _archivedSessionsDirectory = NormalizeDirectory(archivedSessionsDirectory);

    public IReadOnlyList<ArchiveSessionSummary> GetSessions()
    {
        if (!Directory.Exists(_archivedSessionsDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(_archivedSessionsDirectory, "*.jsonl", SearchOption.TopDirectoryOnly)
            .Select(CreateSummary)
            .OrderByDescending(item => item.LastWriteTime)
            .ToArray();
    }

    public ArchiveSessionDetail GetSessionDetail(string filePath)
    {
        var validatedPath = ValidateSessionPath(filePath);
        var fileInfo = new FileInfo(validatedPath);
        var parseResult = ParseFile(validatedPath, includeRecentMessages: true);
        return new ArchiveSessionDetail
        {
            Summary = CreateSummary(fileInfo, parseResult),
            Originator = parseResult.Originator,
            Source = parseResult.Source,
            CliVersion = parseResult.CliVersion,
            Messages = parseResult.RecentMessages
        };
    }

    public void DeleteSession(string filePath, DeletionMode deletionMode)
    {
        var validatedPath = ValidateSessionPath(filePath);
        if (!File.Exists(validatedPath))
        {
            throw new FileNotFoundException("归档文件不存在。", validatedPath);
        }

        if (deletionMode == DeletionMode.RecycleBin)
        {
            FileSystem.DeleteFile(
                validatedPath,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.ThrowException);
            return;
        }

        File.Delete(validatedPath);
    }

    private ArchiveSessionSummary CreateSummary(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var parseResult = ParseFile(filePath, includeRecentMessages: false);
        return CreateSummary(fileInfo, parseResult);
    }

    private static ArchiveSessionSummary CreateSummary(FileInfo fileInfo, ArchiveParseResult parseResult)
    {
        return new ArchiveSessionSummary
        {
            FilePath = fileInfo.FullName,
            FileName = fileInfo.Name,
            SessionId = parseResult.SessionId,
            Cwd = parseResult.Cwd,
            LastWriteTime = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero).ToLocalTime(),
            FileSize = fileInfo.Exists ? fileInfo.Length : 0,
            FirstUserMessagePreview = parseResult.FirstUserMessagePreview,
            ParseStatus = parseResult.Status,
            ParseError = parseResult.ParseError
        };
    }

    private ArchiveParseResult ParseFile(string filePath, bool includeRecentMessages)
    {
        if (!File.Exists(filePath))
        {
            return ArchiveParseResult.Failed("文件不存在。");
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            string? line;
            var state = new ArchiveParseAccumulator(includeRecentMessages);
            while ((line = reader.ReadLine()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    using var document = JsonDocument.Parse(line);
                    ProcessLine(document.RootElement, state);
                }
                catch (JsonException)
                {
                    state.MarkLineError();
                }
            }

            return state.ToResult();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ArchiveParseResult.Failed(ex.Message);
        }
    }

    private static void ProcessLine(JsonElement root, ArchiveParseAccumulator state)
    {
        var lineType = GetString(root, "type");
        if (lineType == "session_meta")
        {
            state.CaptureSessionMeta(root);
            return;
        }

        if (lineType == "response_item")
        {
            state.CaptureResponseItem(root);
        }
    }

    private string ValidateSessionPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("文件路径不能为空。", nameof(filePath));
        }

        var normalizedPath = Path.GetFullPath(filePath);
        var root = EnsureTrailingSeparator(_archivedSessionsDirectory);
        if (!normalizedPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("只允许操作 archived_sessions 目录下的文件。");
        }

        if (!string.Equals(Path.GetExtension(normalizedPath), ".jsonl", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("只允许操作 .jsonl 归档文件。");
        }

        return normalizedPath;
    }

    private static string NormalizeDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("归档目录不能为空。", nameof(path));
        }

        return Path.GetFullPath(path);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static DateTimeOffset? GetTimestamp(JsonElement root)
    {
        var value = GetString(root, "timestamp");
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string BuildMessageContent(JsonElement contentElement)
    {
        if (contentElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var item in contentElement.EnumerateArray())
        {
            var text = GetString(item, "text");
            if (!string.IsNullOrWhiteSpace(text))
            {
                parts.Add(text.Trim());
            }
        }

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    private static string Shorten(string value, int maxLength)
    {
        var compact = value.Replace("\r", " ").Replace("\n", " ").Trim();
        if (compact.Length <= maxLength)
        {
            return compact;
        }

        return compact[..(maxLength - 1)] + "…";
    }

    private sealed class ArchiveParseAccumulator(bool includeRecentMessages)
    {
        private readonly bool _includeRecentMessages = includeRecentMessages;
        private readonly Queue<ArchiveSessionMessage> _recentMessages = new();
        private bool _hasStructuredData;
        private bool _hasLineErrors;

        public string SessionId { get; private set; } = string.Empty;
        public string Cwd { get; private set; } = string.Empty;
        public string Originator { get; private set; } = string.Empty;
        public string Source { get; private set; } = string.Empty;
        public string CliVersion { get; private set; } = string.Empty;
        public string FirstUserMessagePreview { get; private set; } = string.Empty;

        public void MarkLineError()
        {
            _hasLineErrors = true;
        }

        public void CaptureSessionMeta(JsonElement root)
        {
            if (!root.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            _hasStructuredData = true;
            SessionId = GetString(payload, "id");
            Cwd = GetString(payload, "cwd");
            Originator = GetString(payload, "originator");
            Source = GetString(payload, "source");
            CliVersion = GetString(payload, "cli_version");
        }

        public void CaptureResponseItem(JsonElement root)
        {
            if (!root.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (!string.Equals(GetString(payload, "type"), "message", StringComparison.Ordinal))
            {
                return;
            }

            _hasStructuredData = true;
            var role = GetString(payload, "role");
            if (string.IsNullOrWhiteSpace(role))
            {
                return;
            }

            var content = payload.TryGetProperty("content", out var contentElement)
                ? BuildMessageContent(contentElement)
                : string.Empty;
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(FirstUserMessagePreview) && role == "user")
            {
                FirstUserMessagePreview = Shorten(content, MAX_PREVIEW_LENGTH);
            }

            if (!_includeRecentMessages)
            {
                return;
            }

            _recentMessages.Enqueue(new ArchiveSessionMessage
            {
                Timestamp = GetTimestamp(root),
                Role = role,
                Content = content
            });

            while (_recentMessages.Count > MAX_RECENT_MESSAGES)
            {
                _recentMessages.Dequeue();
            }
        }

        public ArchiveParseResult ToResult()
        {
            if (!_hasStructuredData)
            {
                return ArchiveParseResult.Failed(_hasLineErrors ? "文件内容无法解析。" : "文件为空或缺少可识别数据。");
            }

            var status = _hasLineErrors ? ArchiveParseStatus.Partial : ArchiveParseStatus.Success;
            return new ArchiveParseResult(
                status,
                _hasLineErrors ? "部分行解析失败。" : string.Empty,
                SessionId,
                Cwd,
                Originator,
                Source,
                CliVersion,
                FirstUserMessagePreview,
                _recentMessages.ToArray());
        }
    }

    private sealed record ArchiveParseResult(
        ArchiveParseStatus Status,
        string ParseError,
        string SessionId,
        string Cwd,
        string Originator,
        string Source,
        string CliVersion,
        string FirstUserMessagePreview,
        IReadOnlyList<ArchiveSessionMessage> RecentMessages)
    {
        public static ArchiveParseResult Failed(string error)
        {
            return new ArchiveParseResult(
                ArchiveParseStatus.Failed,
                error,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                []);
        }
    }
}
