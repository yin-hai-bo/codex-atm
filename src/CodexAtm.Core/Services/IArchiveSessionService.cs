using CodexAtm.Core.Models;

namespace CodexAtm.Core.Services;

public interface IArchiveSessionService
{
    Task<IReadOnlyList<ArchiveSessionSummary>> GetSessionsAsync(CancellationToken cancellationToken);

    IReadOnlyList<ArchiveSessionSummary> GetSessions();

    ArchiveSessionDetail GetSessionDetail(string filePath);

    void DeleteSession(string filePath, DeletionMode deletionMode);
}
