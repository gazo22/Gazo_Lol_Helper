using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RiftLens.Infrastructure.Persistence;
using RiftLens.Infrastructure.Riot;

namespace RiftLens.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRiftLensInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RiftLens")
            ?? throw new InvalidOperationException("ConnectionStrings:RiftLens manquante.");

        services.AddDbContext<RiftLensDbContext>(o => o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddOptions<RiotApiOptions>()
            .Bind(configuration.GetSection(RiotApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
