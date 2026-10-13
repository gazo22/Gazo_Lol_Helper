using Microsoft.AspNetCore.Http.HttpResults;
using RiftLens.Contracts;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.StaticData;

namespace RiftLens.Api.Endpoints;

public static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/matches/{matchId}", async Task<Results<Ok<MatchDetailDto>, NotFound>> (string matchId, IMatchService matches, CancellationToken ct) =>
            await matches.GetDetailAsync(matchId, ct) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound())
            .WithName("GetMatch");

        app.MapGet("/api/matches/{matchId}/gold-diff", async Task<Results<Ok<IReadOnlyList<GoldDiffPointDto>>, NotFound>> (string matchId, IMatchService matches, CancellationToken ct) =>
            await matches.GetGoldDiffAsync(matchId, ct) is { } points ? TypedResults.Ok(points) : TypedResults.NotFound())
            .WithName("GetGoldDiff");

        app.MapGet("/api/static/manifest", async (IStaticDataService data, HttpContext http, CancellationToken ct) =>
            {
                http.Response.Headers.CacheControl = "private, max-age=3600";
                return TypedResults.Ok(await data.GetManifestAsync(ct));
            })
            .WithName("GetStaticManifest");
        return app;
    }
}
