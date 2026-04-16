using System.Globalization;
using System.Threading;
using CodexAtm.Core.Models;

namespace CodexAtm.App;

public sealed class LocalizationService
{
    private readonly AppSettingsService _settingsService;

    public LocalizationService(AppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public event EventHandler? LanguageChanged;

    public LanguageMode CurrentLanguageMode { get; private set; } = LanguageMode.System;

    public void Initialize()
    {
        CurrentLanguageMode = _settingsService.LoadLanguageMode();
        ApplyLanguage();
    }

    public void SetLanguageMode(LanguageMode languageMode)
    {
        if (CurrentLanguageMode == languageMode)
        {
            return;
        }

        CurrentLanguageMode = languageMode;
        _settingsService.SaveLanguageMode(languageMode);
        ApplyLanguage();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyLanguage()
    {
        var culture = ResolveCulture(CurrentLanguageMode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private static CultureInfo ResolveCulture(LanguageMode languageMode)
    {
        return languageMode switch
        {
            LanguageMode.SimplifiedChinese => CultureInfo.GetCultureInfo("zh-CN"),
            LanguageMode.English => CultureInfo.GetCultureInfo("en-US"),
            _ => CultureInfo.InstalledUICulture
        };
    }
}
