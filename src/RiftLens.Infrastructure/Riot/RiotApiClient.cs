using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Riot.Dtos;
using RiftLens.Infrastructure.Riot.RateLimit;

namespace RiftLens.Infrastructure.Riot;

internal sealed class RiotApiClient(HttpClient http) : IRiotApiClient
{
    private static readonly RiotJsonContext Json = RiotJsonContext.Default;

    public Task<AccountDto?> GetAccountByRiotIdAsync(RegionalRoute route, string gameName, string tagLine, CancellationToken ct = default) =>
        GetAsync(Regional(route, $"/riot/account/v1/accounts/by-riot-id/{Esc(gameName)}/{Esc(tagLine)}"), "account-v1.by-riot-id", Json.AccountDto, null, ct);

    public Task<SummonerDto?> GetSummonerByPuuidAsync(Platform platform, string puuid, CancellationToken ct = default) =>
        GetAsync(OnPlatform(platform, $"/lol/summoner/v4/summoners/by-puuid/{Esc(puuid)}"), "summoner-v4.by-puuid", Json.SummonerDto, null, ct);

    public async Task<IReadOnlyList<LeagueEntryDto>> GetLeagueEntriesByPuuidAsync(Platform platform, string puuid, CancellationToken ct = default) =>
        await GetAsync(OnPlatform(platform, $"/lol/league/v4/entries/by-puuid/{Esc(puuid)}"), "league-v4.entries-by-puuid", Json.IReadOnlyListLeagueEntryDto, null, ct) ?? [];

    public Task<LeagueListDto?> GetApexLeagueAsync(Platform platform, RankTier tier, CancellationToken ct = default)
    {
        var segment = tier switch
        {
            RankTier.Challenger => "challengerleagues",
            RankTier.Grandmaster => "grandmasterleagues",
            RankTier.Master => "masterleagues",
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Seuls Master, Grandmaster et Challenger ont un ladder."),
        };
        return GetAsync(OnPlatform(platform, $"/lol/league/v4/{segment}/by-queue/RANKED_SOLO_5x5"), $"league-v4.{segment}", Json.LeagueListDto, null, ct);
    }

    public async Task<IReadOnlyList<ChampionMasteryDto>> GetTopMasteriesAsync(Platform platform, string puuid, int count, CancellationToken ct = default) =>
        await GetAsync(
            OnPlatform(platform, $"/lol/champion-mastery/v4/champion-masteries/by-puuid/{Esc(puuid)}/top?count={count.ToString(CultureInfo.InvariantCulture)}"),
            "champion-mastery-v4.top", Json.IReadOnlyListChampionMasteryDto, null, ct) ?? [];

    public async Task<IReadOnlyList<string>> GetMatchIdsAsync(RegionalRoute route, string puuid, MatchIdQuery query, CancellationToken ct = default)
    {
        var path = FormattableString.Invariant($"/lol/match/v5/matches/by-puuid/{Esc(puuid)}/ids?start={query.Start}&count={query.Count}");
        if (query.Queue is { } queue)
        {
            path += FormattableString.Invariant($"&queue={queue}");
        }

        if (query.StartTime is { } startTime)
        {
            path += FormattableString.Invariant($"&startTime={startTime.ToUnixTimeSeconds()}");
        }

        return await GetAsync(Regional(route, path), "match-v5.ids-by-puuid", Json.IReadOnlyListString, query.Priority, ct) ?? [];
    }

    public Task<MatchDto?> GetMatchAsync(RegionalRoute route, string matchId, CancellationToken ct = default) =>
        GetAsync(Regional(route, $"/lol/match/v5/matches/{Esc(matchId)}"), "match-v5.match", Json.MatchDto, null, ct);

    public Task<TimelineDto?> GetTimelineAsync(RegionalRoute route, string matchId, CancellationToken ct = default) =>
        GetAsync(Regional(route, $"/lol/match/v5/matches/{Esc(matchId)}/timeline"), "match-v5.timeline", Json.TimelineDto, null, ct);

    private async Task<T?> GetAsync<T>(Uri uri, string method, JsonTypeInfo<T> typeInfo, RequestPriority? priority, CancellationToken ct)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Options.Set(RiotRateLimitHandler.MethodKey, method);
        if (priority is { } p)
        {
            request.Options.Set(RiotRateLimitHandler.PriorityKey, p);
        }

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                return null;
            case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
                throw new RiotApiAuthException(response.StatusCode, "Clé API Riot refusée (absente, invalide ou expirée).");
            case var status when !response.IsSuccessStatusCode:
                throw new RiotApiException(status, $"Erreur Riot {(int)status} sur {method}.");
        }

        return await response.Content.ReadFromJsonAsync(typeInfo, ct).ConfigureAwait(false);
    }

    private static Uri Regional(RegionalRoute route, string path) => new($"https://{route.ToHost()}.api.riotgames.com{path}");

    private static Uri OnPlatform(Platform platform, string path) => new($"https://{platform.ToHost()}.api.riotgames.com{path}");

    private static string Esc(string value) => Uri.EscapeDataString(value);
}
