using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RiftLens.LocalClient.Connection;

namespace RiftLens.LocalClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRiftLensLocalClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LocalClientOptions>().Bind(configuration.GetSection(LocalClientOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ILockfileLocator, LockfileLocator>();
        services.AddSingleton<LeagueClientService>();
        services.AddSingleton<ILeagueClientService>(sp => sp.GetRequiredService<LeagueClientService>());
        services.AddHostedService(sp => sp.GetRequiredService<LeagueClientService>());
        return services;
    }
}
