using Microsoft.AspNetCore.Http.HttpResults;
using RiftLens.Contracts;
using RiftLens.Infrastructure.Stats;

namespace RiftLens.Api.Endpoints;

public static class StatsEndpoints
{
    public static IEndpointRouteBuilder MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stats/patches", async (IStatsQueryService stats, CancellationToken ct) =>
                TypedResults.Ok(new PatchListDto(await stats.GetPatchesAsync(ct))))
            .WithName("GetStatPatches");

        // 404 = pas encore de données agrégées pour ce patch / elo (Collector pas encore passé).
        app.MapGet("/api/tierlist", async Task<Results<Ok<TierListDto>, NotFound>> (string? patch, string? role, string? elo, IStatsQueryService stats, CancellationToken ct) =>
                await stats.GetTierListAsync(patch, role, elo, ct) is { } list ? TypedResults.Ok(list) : TypedResults.NotFound())
            .WithName("GetTierList");

        app.MapGet("/api/champions/{championId:int}", async Task<Results<Ok<ChampionDetailDto>, NotFound>> (int championId, string? patch, string? role, string? elo, IStatsQueryService stats, CancellationToken ct) =>
                await stats.GetChampionAsync(championId, patch, role, elo, ct) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound())
            .WithName("GetChampionStats");
        return app;
    }
}
