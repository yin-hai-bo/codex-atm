namespace CodexAtm.Core.Models;

public sealed record ThemeModeOption(ThemeMode Mode, string DisplayName)
{
    public static IReadOnlyList<ThemeModeOption> DefaultOptions { get; } =
    [
        new(ThemeMode.System, "跟随系统"),
        new(ThemeMode.Light, "浅色"),
        new(ThemeMode.Dark, "深色")
    ];
}
