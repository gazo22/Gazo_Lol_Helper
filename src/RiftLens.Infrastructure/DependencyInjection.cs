using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RiftLens.Infrastructure.Collection;
using RiftLens.Infrastructure.Local;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Persistence;
using RiftLens.Infrastructure.Players;
using RiftLens.Infrastructure.Riot;
using RiftLens.Infrastructure.Riot.RateLimit;
using RiftLens.Infrastructure.StaticData;
using RiftLens.Infrastructure.Stats;

namespace RiftLens.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRiftLensInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RiftLens")
            ?? throw new InvalidOperationException("ConnectionStrings:RiftLens manquante.");

        services.AddDbContext<RiftLensDbContext>(o => o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();

        services.AddRiotApi(configuration);
        services.AddDataDragon(configuration);

        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddOptions<StatsOptions>().Bind(configuration.GetSection(StatsOptions.SectionName));
        services.AddScoped<IStatsAggregator, StatsAggregator>();
        services.AddScoped<IStatsQueryService, StatsQueryService>();
        services.AddOptions<CollectorOptions>().Bind(configuration.GetSection(CollectorOptions.SectionName));
        services.AddScoped<CollectorPipeline>();
        return services;
    }

    public static IServiceCollection AddRiotApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RiotApiOptions>().Bind(configuration.GetSection(RiotApiOptions.SectionName));
        services.AddOptions<LocalModeOptions>().Bind(configuration.GetSection(LocalModeOptions.SectionName));
        services.AddSingleton<IRiotKeyStore, RiotKeyStore>();
        services.AddOptions<RiotRateLimitOptions>()
            .Bind(configuration.GetSection(RiotRateLimitOptions.SectionName))
            .Validate(o => o.BudgetShare is > 0 and <= 1, "Riot:RateLimit:BudgetShare doit être dans ]0, 1].")
            .ValidateOnStart();

        // Un seul limiteur par processus : c'est la file centralisée par route.
        services.AddSingleton<RiotRateLimiter>();
        services.AddTransient<RiotRateLimitHandler>();
        services.AddHttpClient<IRiotApiClient, RiotApiClient>(c => c.Timeout = TimeSpan.FromSeconds(100))
            .AddHttpMessageHandler<RiotRateLimitHandler>();
        return services;
    }

    private static void AddDataDragon(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DataDragonOptions>().Bind(configuration.GetSection(DataDragonOptions.SectionName));
        services.AddHttpClient<IStaticDataService, DataDragonService>((sp, c) =>
        {
            c.BaseAddress = sp.GetRequiredService<IOptions<DataDragonOptions>>().Value.BaseUrl;
            c.Timeout = TimeSpan.FromSeconds(30);
        });
    }
}
