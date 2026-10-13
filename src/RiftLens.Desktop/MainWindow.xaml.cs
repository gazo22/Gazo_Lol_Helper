using System.IO;
using System.Windows;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using RiftLens.UI.Services;

namespace RiftLens.Desktop;

public partial class MainWindow : Window
{
    private readonly DiskAssetCache _cache;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _cache = services.GetRequiredService<DiskAssetCache>();
        _logger = services.GetRequiredService<ILogger<MainWindow>>();
        WebView.Services = services;
        WebView.BlazorWebViewInitializing += (_, e) => e.UserDataFolder = Path.Combine(App.LocalDataDirectory, "WebView2");
        WebView.BlazorWebViewInitialized += OnWebViewInitialized;
    }

    /// <summary>Les images Data Dragon sont servies depuis le cache disque local (téléchargées une seule fois).</summary>
    private void OnWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        var core = e.WebView.CoreWebView2;
#pragma warning disable CA1848, CA2254, CA1873 // diagnostics ponctuels
        _logger.LogInformation("WebView2 {Version} initialisé, profil {Folder}", core.Environment.BrowserVersionString, core.Environment.UserDataFolder);
        core.ProcessFailed += (_, a) => _logger.LogError("Processus WebView2 en échec : {Kind} ({Reason})", a.ProcessFailedKind, a.Reason);
        core.NavigationCompleted += (_, a) => _logger.LogInformation("Navigation terminée : succès={Success}, statut={Status}", a.IsSuccess, a.WebErrorStatus);
        // Erreurs JavaScript / Blazor côté page (rien ne s'affiche si le rendu échoue).
        _ = core.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}");
        core.GetDevToolsProtocolEventReceiver("Runtime.exceptionThrown").DevToolsProtocolEventReceived +=
            (_, a) => _logger.LogError("Erreur JavaScript : {Details}", a.ParameterObjectAsJson);
        core.GetDevToolsProtocolEventReceiver("Runtime.consoleAPICalled").DevToolsProtocolEventReceived +=
            (_, a) => { if (a.ParameterObjectAsJson.Contains("\"type\":\"error\"", StringComparison.Ordinal)) { _logger.LogError("Console : {Details}", a.ParameterObjectAsJson); } };
#pragma warning restore CA1848, CA2254, CA1873
        core.AddWebResourceRequestedFilter($"https://{DiskAssetCache.Host}/*", CoreWebView2WebResourceContext.Image);
        core.WebResourceRequested += async (_, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                if (await _cache.GetAsync(new Uri(args.Request.Uri)) is { } path)
                {
                    var stream = File.OpenRead(path);
                    args.Response = core.Environment.CreateWebResourceResponse(stream, 200, "OK",
                        $"Content-Type: {DiskAssetCache.ContentType(path)}\nCache-Control: max-age=31536000, immutable");
                }

                // Sinon : la requête part normalement vers le CDN.
            }
            finally
            {
                deferral.Complete();
            }
        };
    }
}
