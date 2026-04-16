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
        return LoadSettings().ThemeMode;
    }

    public LanguageMode LoadLanguageMode()
    {
        return LoadSettings().LanguageMode;
    }

    public void SaveThemeMode(ThemeMode themeMode)
    {
        var settings = LoadSettings() with
        {
            ThemeMode = themeMode
        };
        SaveSettings(settings);
    }

    public void SaveLanguageMode(LanguageMode languageMode)
    {
        var settings = LoadSettings() with
        {
            LanguageMode = languageMode
        };
        SaveSettings(settings);
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    private void SaveSettings(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private sealed record AppSettings
    {
        public ThemeMode ThemeMode { get; init; } = ThemeMode.System;

        public LanguageMode LanguageMode { get; init; } = LanguageMode.System;
    }
}
