using System.Windows;

namespace CodexAtm.App;

public partial class App : Application
{
    public ThemeService ThemeService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeService = new ThemeService(new AppSettingsService());
        ThemeService.Initialize();

        var window = new ArchiveManagerWindow();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeService.Dispose();
        base.OnExit(e);
    }
}
