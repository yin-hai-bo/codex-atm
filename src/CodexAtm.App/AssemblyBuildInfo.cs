using System.Reflection;

namespace CodexAtm.App;

public static class AssemblyBuildInfo
{
    private const string DefaultRepositoryUrl = "https://github.com/yin-hai-bo/codex-atm";

    public static string CommitId => GetCommitId() ?? "unknown";

    public static string RepositoryUrl =>
        NormalizeRepositoryUrl(GetAssemblyMetadata("RepositoryUrl") ?? DefaultRepositoryUrl);

    private static string? GetAssemblyMetadata(string key)
    {
        return typeof(AppVersionInfo).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))?
            .Value;
    }

    private static string? GetCommitId()
    {
        var informationalVersion = typeof(AppVersionInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return null;
        }

        var separatorIndex = informationalVersion.LastIndexOf('+');
        if (separatorIndex < 0 || separatorIndex == informationalVersion.Length - 1)
        {
            return null;
        }

        var commitId = informationalVersion[(separatorIndex + 1)..].Trim();
        return string.IsNullOrWhiteSpace(commitId) ? null : commitId;
    }

    private static string NormalizeRepositoryUrl(string repositoryUrl)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            return "unknown";
        }

        var normalized = repositoryUrl.Trim();
        if (normalized.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "https://github.com/" + normalized["git@github.com:".Length..];
        }

        if (normalized.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        return normalized;
    }
}
