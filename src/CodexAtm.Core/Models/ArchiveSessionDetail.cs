namespace CodexAtm.Core.Models;

public sealed class ArchiveSessionDetail
{
    public required ArchiveSessionSummary Summary { get; init; }

    public string Originator { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string CliVersion { get; init; } = string.Empty;

    public IReadOnlyList<ArchiveSessionMessage> Messages { get; init; } = [];
}
