namespace CodexAtm.Core.Models;

public sealed class ArchiveSessionMessage
{
    public DateTimeOffset? Timestamp { get; init; }

    public string Role { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
}
