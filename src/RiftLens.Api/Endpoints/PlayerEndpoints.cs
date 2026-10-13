using Microsoft.AspNetCore.Http.HttpResults;
using RiftLens.Contracts;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Players;

namespace RiftLens.Api.Endpoints;

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players/{platform}");

        group.MapGet("/{gameName}/{tagLine}", GetProfileAsync).WithName("GetProfile");
        group.MapGet("/by-puuid/{puuid}/matches", GetHistoryAsync).WithName("GetMatchHistory");
        return app;
    }

    private static async Task<Results<Ok<ProfileDto>, ProblemHttpResult>> GetProfileAsync(
        string platform, string gameName, string tagLine, bool? refresh, IPlayerService players, CancellationToken ct)
    {
        if (!TryParsePlatform(platform, out var p))
        {
            return InvalidPlatform(platform);
        }

        if (!RiotId.TryParse($"{gameName}#{tagLine}", out var riotId))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Riot ID invalide (attendu : Nom#TAG).");
        }

        return TypedResults.Ok(await players.GetProfileAsync(p, riotId.Value, refresh ?? false, ct));
    }

    private static async Task<Results<Ok<IReadOnlyList<MatchSummaryDto>>, ProblemHttpResult>> GetHistoryAsync(
        string platform, string puuid, int? start, int? count, IMatchService matches, CancellationToken ct)
    {
        if (!TryParsePlatform(platform, out var p))
        {
            return InvalidPlatform(platform);
        }

        return TypedResults.Ok(await matches.GetHistoryAsync(p, puuid, Math.Max(0, start ?? 0), count ?? 10, ct));
    }

    internal static bool TryParsePlatform(string value, out Platform platform) =>
        Enum.TryParse(value, ignoreCase: true, out platform) && Enum.IsDefined(platform);

    private static ProblemHttpResult InvalidPlatform(string value) =>
        TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Plateforme inconnue : {value}.");
}
