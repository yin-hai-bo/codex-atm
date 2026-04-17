using System.Windows;

namespace CodexAtm.App;

public partial class App : Application
{
    public ThemeService ThemeService { get; private set; } = null!;
    public LocalizationService LocalizationService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new AppSettingsService();
        LocalizationService = new LocalizationService(settingsService);
        LocalizationService.Initialize();
        ThemeService = new ThemeService(settingsService);
        ThemeService.Initialize();

        var window = new ArchiveManagerWindow();
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeService.Dispose();
        base.OnExit(e);
    }
}
