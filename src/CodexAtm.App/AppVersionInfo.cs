namespace CodexAtm.App;

public static class AppVersionInfo
{
    public static string ProductName => AppText.ProductName;

    public static string Version { get; } =
        typeof(AppVersionInfo).Assembly.GetName().Version?.ToString()
        ?? "0.0.0.0";

    public static string DisplayVersion => $"v{Version}";

    public static string WindowTitle => $"{ProductName} {DisplayVersion}";
}
