using CodexAtm.Core.Localization;
using CodexAtm.Core.ViewModels;

namespace CodexAtm.Core.Models;

public sealed class LanguageModeOption : ObservableObject
{
    private string _displayName;

    public LanguageModeOption(LanguageMode mode)
    {
        Mode = mode;
        _displayName = GetDisplayName(mode);
    }

    public LanguageMode Mode { get; }

    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    public void RefreshLocalizedText()
    {
        DisplayName = GetDisplayName(Mode);
    }

    private static string GetDisplayName(LanguageMode mode)
    {
        return mode switch
        {
            LanguageMode.System => CoreText.LanguageModeSystem,
            LanguageMode.SimplifiedChinese => CoreText.LanguageModeSimplifiedChinese,
            LanguageMode.English => CoreText.LanguageModeEnglish,
            _ => CoreText.LanguageModeSystem
        };
    }
}
