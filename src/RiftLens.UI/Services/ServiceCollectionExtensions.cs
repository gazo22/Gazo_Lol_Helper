using Microsoft.Extensions.DependencyInjection;

namespace RiftLens.UI.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRiftLensUi(this IServiceCollection services, BackendOptions backend)
    {
        if (backend.BaseUrl.Scheme != Uri.UriSchemeHttps && !backend.BaseUrl.IsLoopback)
        {
            throw new InvalidOperationException("Backend:BaseUrl doit être en HTTPS pour un serveur distant.");
        }

        services.AddTransient(_ => new AccessKeyHandler(backend));
        services.AddHttpClient<IBackendStatus, HttpBackendStatus>(c => Configure(c, backend)).AddHttpMessageHandler<AccessKeyHandler>();
        services.AddHttpClient<RiftLensApiClient>(c => Configure(c, backend)).AddHttpMessageHandler<AccessKeyHandler>()
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(60)); // première synchro d'un profil : jusqu'à ~20 matchs
        services.AddScoped<GameAssets>();

        services.AddSingleton<CommandPaletteState>();
        services.AddSingleton<IPaletteSource, NavigationPaletteSource>();
        services.AddScoped<IPaletteSource, ChampionPaletteSource>();
        return services;
    }

    private static void Configure(HttpClient client, BackendOptions backend)
    {
        client.BaseAddress = backend.BaseUrl;
        client.Timeout = TimeSpan.FromSeconds(10);
    }
}
