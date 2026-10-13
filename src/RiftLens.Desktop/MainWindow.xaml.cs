using System.IO;
using System.Windows;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using RiftLens.UI.Services;

namespace RiftLens.Desktop;

public partial class MainWindow : Window
{
    private readonly DiskAssetCache _cache;

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _cache = services.GetRequiredService<DiskAssetCache>();
        WebView.Services = services;
        WebView.BlazorWebViewInitializing += (_, e) => e.UserDataFolder = Path.Combine(App.LocalDataDirectory, "WebView2");
        WebView.BlazorWebViewInitialized += OnWebViewInitialized;
    }

    /// <summary>Les images Data Dragon sont servies depuis le cache disque local (téléchargées une seule fois).</summary>
    private void OnWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        var core = e.WebView.CoreWebView2;
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
