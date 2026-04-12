using System.IO;
using System.Text.Json;
using CodexAtm.Core.Models;

namespace CodexAtm.App;

public sealed class AppSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;

    public AppSettingsService()
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CodexAtm");
        _settingsFilePath = Path.Combine(settingsDirectory, "settings.json");
    }

    public ThemeMode LoadThemeMode()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return ThemeMode.System;
            }

            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings?.ThemeMode ?? ThemeMode.System;
        }
        catch
        {
            return ThemeMode.System;
        }
    }

    public void SaveThemeMode(ThemeMode themeMode)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(new AppSettings(themeMode), SerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private sealed record AppSettings(ThemeMode ThemeMode);
}
