using Microsoft.Extensions.Options;
using RiftLens.Contracts;
using RiftLens.Infrastructure.Riot;

namespace RiftLens.Api.Endpoints;

public static class MetaEndpoints
{
    public static IEndpointRouteBuilder MapMetaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/meta", (IOptions<RiotApiOptions> riot) =>
            TypedResults.Ok(new ServerInfoDto(
                typeof(MetaEndpoints).Assembly.GetName().Version?.ToString() ?? "0.0.0",
                riot.Value.DefaultPlatform.ToString(),
                DataDragonVersion: null)))
            .WithName("GetMeta");
        return app;
    }
}
