using Microsoft.Extensions.DependencyInjection;
using RiftLens.Contracts;

namespace RiftLens.UI.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRiftLensUi(this IServiceCollection services, BackendOptions backend)
    {
        if (backend.BaseUrl.Scheme != Uri.UriSchemeHttps && !backend.BaseUrl.IsLoopback)
        {
            throw new InvalidOperationException("Backend:BaseUrl doit être en HTTPS pour un serveur distant.");
        }

        services.AddHttpClient<IBackendStatus, HttpBackendStatus>(c => Configure(c, backend));

        services.AddSingleton<CommandPaletteState>();
        services.AddSingleton<IPaletteSource, NavigationPaletteSource>();
        return services;
    }

    private static void Configure(HttpClient client, BackendOptions backend)
    {
        client.BaseAddress = backend.BaseUrl;
        client.Timeout = TimeSpan.FromSeconds(10);
        if (backend.AccessKey.Length > 0)
        {
            client.DefaultRequestHeaders.Add(ApiConventions.AccessKeyHeader, backend.AccessKey);
        }
    }
}
