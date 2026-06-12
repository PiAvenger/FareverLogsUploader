using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FareverLogs.Uploader.Config;
using FareverLogs.Uploader.Navigation;
using FareverLogs.Uploader.Updates;
using FareverLogs.Uploader.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FareverLogs.Uploader;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var sc = new ServiceCollection();
        ConfigureServices(sc);
        Services = sc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();

            var nav    = Services.GetRequiredService<NavigationService>();
            var config = Services.GetRequiredService<AppConfig>();

            LogMaintenance.Run(config);

            if (string.IsNullOrEmpty(config.JwtToken))
                nav.NavigateTo<LoginViewModel>();
            else
                nav.NavigateTo<HomeViewModel>();

            _ = CheckForUpdatesAsync(nav);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task CheckForUpdatesAsync(NavigationService nav)
    {
        var currentVersion = typeof(App).Assembly.GetName().Version;
        if (currentVersion is null) return;

        using var http  = new HttpClient();
        var       notice = await UpdateChecker.CheckAsync(http, currentVersion);

        if (notice is { } n)
            nav.ShowUpdateNotice(n);
    }

    private static void ConfigureServices(IServiceCollection sc)
    {
        var config = AppConfig.Load();
        sc.AddSingleton(config);
        sc.AddSingleton<NavigationService>();

        sc.AddSingleton<LoginViewModel>();
        sc.AddSingleton<HomeViewModel>();
        sc.AddSingleton<LiveLogViewModel>();
        sc.AddSingleton<UploadExistingViewModel>();
        sc.AddSingleton<SettingsViewModel>();

        sc.AddSingleton<MainWindow>();
    }
}
