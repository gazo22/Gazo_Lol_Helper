using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Riot.Dtos;
using RiftLens.Infrastructure.Riot.RateLimit;

namespace RiftLens.Infrastructure.Riot;

/// <summary>Client typé des endpoints Riot utilisés par RiftLens. Les méthodes renvoient <c>null</c> sur 404.</summary>
public interface IRiotApiClient
{
    Task<AccountDto?> GetAccountByRiotIdAsync(RegionalRoute route, string gameName, string tagLine, CancellationToken ct = default);

    Task<SummonerDto?> GetSummonerByPuuidAsync(Platform platform, string puuid, CancellationToken ct = default);

    Task<IReadOnlyList<LeagueEntryDto>> GetLeagueEntriesByPuuidAsync(Platform platform, string puuid, CancellationToken ct = default);

    Task<IReadOnlyList<ChampionMasteryDto>> GetTopMasteriesAsync(Platform platform, string puuid, int count, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetMatchIdsAsync(RegionalRoute route, string puuid, MatchIdQuery query, CancellationToken ct = default);

    Task<MatchDto?> GetMatchAsync(RegionalRoute route, string matchId, CancellationToken ct = default);

    Task<TimelineDto?> GetTimelineAsync(RegionalRoute route, string matchId, CancellationToken ct = default);
}

public sealed record MatchIdQuery(int Start = 0, int Count = 20, int? Queue = null, RequestPriority? Priority = null);
