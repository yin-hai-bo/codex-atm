namespace CodexAtm.Core.Models;

public sealed class ArchiveSessionSummary
{
    public const string UngroupedLabel = "未分组";

    public required string FilePath { get; init; }

    public required string FileName { get; init; }

    public string SessionId { get; init; } = string.Empty;

    public string Cwd { get; init; } = string.Empty;

    public DateTimeOffset LastWriteTime { get; init; }

    public long FileSize { get; init; }

    public string ThreadTitle { get; init; } = string.Empty;

    public string FirstUserMessagePreview { get; init; } = string.Empty;

    public ArchiveParseStatus ParseStatus { get; init; }

    public string ParseError { get; init; } = string.Empty;

    public string DisplayFileSize => FileSize switch
    {
        >= 1024L * 1024L * 1024L => $"{FileSize / (1024d * 1024d * 1024d):F2} GB",
        >= 1024L * 1024L => $"{FileSize / (1024d * 1024d):F2} MB",
        >= 1024L => $"{FileSize / 1024d:F2} KB",
        _ => $"{FileSize} B"
    };

    public string DisplayTitle => !string.IsNullOrWhiteSpace(ThreadTitle)
        ? ThreadTitle
        : string.IsNullOrWhiteSpace(FirstUserMessagePreview)
            ? FileName
            : FirstUserMessagePreview;

    public string GroupDisplayName => string.IsNullOrWhiteSpace(Cwd)
        ? UngroupedLabel
        : Cwd;
}
