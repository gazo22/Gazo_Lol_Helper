using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RiftLens.LocalClient;
using RiftLens.UI.Services;
using Serilog;

namespace RiftLens.Desktop;

public partial class App : Application
{
    private IHost? _host;

    internal static string LocalDataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RiftLens");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            ReportFatal(args.Exception);
            args.Handled = true;
            Shutdown(1);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) => ReportFatal(args.ExceptionObject as Exception);

        try
        {
            await StartAsync();
        }
#pragma warning disable CA1031 // dernier rempart : afficher l'erreur plutôt que de quitter silencieusement
        catch (Exception ex)
#pragma warning restore CA1031
        {
            ReportFatal(ex);
            Shutdown(1);
        }
    }

    /// <summary>Écrit l'erreur dans %LocalAppData%\RiftLens\logs\crash.log et l'affiche.</summary>
    private static void ReportFatal(Exception? ex)
    {
        var path = Path.Combine(LocalDataDirectory, "logs", "crash.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"[{DateTimeOffset.Now:O}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // On affiche quand même le message.
        }

        MessageBox.Show(
            $"RiftLens n'a pas pu démarrer :\n\n{ex?.Message}\n\nDétails : {path}",
            "RiftLens", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task StartAsync()
    {
        // WebView2 écrit son profil à côté de l'exe par défaut : impossible dans Program Files.
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", Path.Combine(LocalDataDirectory, "WebView2"));

        var logDirectory = Path.Combine(LocalDataDirectory, "logs");
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Production,
        });
        builder.Configuration.AddUserSecrets<App>(optional: true);
        builder.Services.AddSerilog(cfg => cfg
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.File(Path.Combine(logDirectory, "desktop-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7));

        var backend = builder.Configuration.GetSection(BackendOptions.SectionName).Get<BackendOptions>() ?? new BackendOptions();
        if (backend.AccessKey.Length == 0)
        {
            // Installation tout-en-un : clé générée par le service local au premier démarrage (relue tant qu'absente).
            backend.AccessKeyProvider = LocalServer.TryReadAccessKey;
        }

        var cacheDirectory = Path.Combine(LocalDataDirectory, "cache", "images");
        builder.Services.AddSingleton(sp => new DiskAssetCache(sp.GetRequiredService<IHttpClientFactory>().CreateClient("assets"), cacheDirectory));
        builder.Services.AddHttpClient("assets", c => c.Timeout = TimeSpan.FromSeconds(20));
        builder.Services.AddWpfBlazorWebView();
        // Outils de développement (F12 / Ctrl+Maj+I) : utiles pour diagnostiquer l'affichage.
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Services.AddRiftLensUi(backend);
        builder.Services.AddRiftLensLocalClient(builder.Configuration);
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
        await _host.StartAsync();

        MainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
