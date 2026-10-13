using System.Text.Json;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Riot;
using RiftLens.Infrastructure.Riot.Dtos;

namespace RiftLens.Infrastructure.Tests.Support;

/// <summary>Faux client Riot servant les fixtures et comptant les appels.</summary>
internal sealed class FixtureRiotClient : IRiotApiClient
{
    public int MatchCalls { get; private set; }
    public int AccountCalls { get; private set; }
    public int TimelineCalls { get; private set; }
    public List<string> MatchIds { get; set; } = ["EUW1_7512345678"];

    private static T Load<T>(string path) => JsonSerializer.Deserialize<T>(Fixture.Read(path), JsonSerializerOptions.Web)!;

    public Task<AccountDto?> GetAccountByRiotIdAsync(RegionalRoute route, string gameName, string tagLine, CancellationToken ct = default)
    {
        AccountCalls++;
        return Task.FromResult(gameName == "Gazo" ? Load<AccountDto>("riot/account.json") : null);
    }

    public Task<SummonerDto?> GetSummonerByPuuidAsync(Platform platform, string puuid, CancellationToken ct = default) =>
        Task.FromResult<SummonerDto?>(Load<SummonerDto>("riot/summoner.json"));

    public Task<IReadOnlyList<LeagueEntryDto>> GetLeagueEntriesByPuuidAsync(Platform platform, string puuid, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<LeagueEntryDto>>(Load<List<LeagueEntryDto>>("riot/league-entries.json"));

    public int MatchIdCalls { get; private set; }

    public Task<LeagueListDto?> GetApexLeagueAsync(Platform platform, RankTier tier, CancellationToken ct = default) =>
        Task.FromResult(tier == RankTier.Challenger ? Load<LeagueListDto>("riot/challenger-league.json") : null);

    public Task<IReadOnlyList<ChampionMasteryDto>> GetTopMasteriesAsync(Platform platform, string puuid, int count, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ChampionMasteryDto>>(Load<List<ChampionMasteryDto>>("riot/masteries.json"));

    public Func<string, IReadOnlyList<string>>? MatchIdsByPuuid { get; set; }

    public Task<IReadOnlyList<string>> GetMatchIdsAsync(RegionalRoute route, string puuid, MatchIdQuery query, CancellationToken ct = default)
    {
        MatchIdCalls++;
        var ids = MatchIdsByPuuid?.Invoke(puuid) ?? MatchIds;
        return Task.FromResult<IReadOnlyList<string>>(ids.Skip(query.Start).Take(query.Count).ToList());
    }

    public Task<MatchDto?> GetMatchAsync(RegionalRoute route, string matchId, CancellationToken ct = default)
    {
        MatchCalls++;
        var match = Fixture.Match();
        return Task.FromResult<MatchDto?>(match with { Metadata = match.Metadata with { MatchId = matchId } });
    }

    public Task<bool> ValidateKeyAsync(Platform platform, string key, CancellationToken ct = default) => Task.FromResult(key.EndsWith('0'));

    public Task<TimelineDto?> GetTimelineAsync(RegionalRoute route, string matchId, CancellationToken ct = default)
    {
        TimelineCalls++;
        return Task.FromResult<TimelineDto?>(Fixture.Timeline());
    }
}
