using CodexAtm.Core.Models;

namespace CodexAtm.Core.Services;

public interface IArchiveSessionService
{
    IReadOnlyList<ArchiveSessionSummary> GetSessions();

    ArchiveSessionDetail GetSessionDetail(string filePath);

    void DeleteSession(string filePath, DeletionMode deletionMode);
}
