using System.Windows;
using CodexAtm.Core.Models;
using Microsoft.Win32;

namespace CodexAtm.App;

using ThemeMode = CodexAtm.Core.Models.ThemeMode;

public sealed class ThemeService : IDisposable
{
    private readonly AppSettingsService _settingsService;
    private ResourceDictionary? _themeDictionary;
    private bool _isInitialized;

    public ThemeService(AppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public ThemeMode CurrentThemeMode { get; private set; } = ThemeMode.System;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        CurrentThemeMode = _settingsService.LoadThemeMode();
        ApplyTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _isInitialized = true;
    }

    public void SetThemeMode(ThemeMode themeMode)
    {
        if (CurrentThemeMode == themeMode && _themeDictionary is not null)
        {
            return;
        }

        CurrentThemeMode = themeMode;
        _settingsService.SaveThemeMode(themeMode);
        ApplyTheme();
    }

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (CurrentThemeMode != ThemeMode.System)
        {
            return;
        }

        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var actualTheme = CurrentThemeMode == ThemeMode.System
            ? DetectSystemTheme()
            : CurrentThemeMode;

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                actualTheme == ThemeMode.Dark
                    ? "Themes/DarkTheme.xaml"
                    : "Themes/LightTheme.xaml",
                UriKind.Relative)
        };

        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
        if (_themeDictionary is not null)
        {
            mergedDictionaries.Remove(_themeDictionary);
        }

        mergedDictionaries.Add(dictionary);
        _themeDictionary = dictionary;
    }

    private static ThemeMode DetectSystemTheme()
    {
        const string personalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(personalizeKeyPath);
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0 ? ThemeMode.Dark : ThemeMode.Light;
        }
        catch
        {
            return ThemeMode.Light;
        }
    }
}
