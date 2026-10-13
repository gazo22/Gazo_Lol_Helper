using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Riot;
using RiftLens.Infrastructure.Riot.RateLimit;
using RiftLens.Infrastructure.Tests.Support;

namespace RiftLens.Infrastructure.Tests.Riot;

public class RiotApiClientTests
{
    private static (RiotApiClient Client, StubHttpHandler Stub) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var stub = new StubHttpHandler(respond);
        var rateOptions = Options.Create(new RiotRateLimitOptions { DefaultRetryAfter = TimeSpan.FromMilliseconds(1) });
        var limiter = new RiotRateLimiter(rateOptions, TimeProvider.System, NullLogger<RiotRateLimiter>.Instance);
        var handler = new RiotRateLimitHandler(limiter, rateOptions, Options.Create(new RiotApiOptions { ApiKey = "RGAPI-test" }), TimeProvider.System)
        {
            InnerHandler = stub,
        };
        return (new RiotApiClient(new HttpClient(handler)), stub);
    }

    [Fact]
    public async Task Account_utilise_le_routage_regional_et_la_cle()
    {
        var (client, stub) = Create(_ => StubHttpHandler.Json(Fixture.Read("riot/account.json")));

        var account = await client.GetAccountByRiotIdAsync(RegionalRoute.Europe, "Le Gazo", "EUW");

        Assert.Equal("Gazo", account!.GameName);
        var request = Assert.Single(stub.Requests);
        Assert.Equal("https://europe.api.riotgames.com/riot/account/v1/accounts/by-riot-id/Le%20Gazo/EUW", request.RequestUri!.AbsoluteUri);
        Assert.Equal("RGAPI-test", request.Headers.GetValues("X-Riot-Token").Single());
    }

    [Fact]
    public async Task League_utilise_le_routage_plateforme()
    {
        var (client, stub) = Create(_ => StubHttpHandler.Json(Fixture.Read("riot/league-entries.json")));

        var entries = await client.GetLeagueEntriesByPuuidAsync(Platform.Euw1, "abc");

        Assert.Equal(2, entries.Count);
        Assert.Equal("EMERALD", entries[0].Tier);
        Assert.Equal("euw1.api.riotgames.com", stub.Requests[0].RequestUri!.Host);
    }

    [Fact]
    public async Task Match_et_timeline_sont_deserialises()
    {
        var (client, _) = Create(r => StubHttpHandler.Json(Fixture.Read(r.RequestUri!.AbsolutePath.EndsWith("/timeline", StringComparison.Ordinal) ? "riot/timeline.json" : "riot/match.json")));

        var match = await client.GetMatchAsync(RegionalRoute.Europe, "EUW1_7512345678");
        var timeline = await client.GetTimelineAsync(RegionalRoute.Europe, "EUW1_7512345678");

        Assert.Equal(10, match!.Info.Participants.Count);
        Assert.Equal(8112, match.Info.Participants[0].Perks!.Styles[0].Selections[0].Perk);
        Assert.Equal(32, timeline!.Info.Frames.Count);
    }

    [Fact]
    public async Task MatchIds_transmet_la_pagination_et_la_file()
    {
        var (client, stub) = Create(_ => StubHttpHandler.Json(Fixture.Read("riot/match-ids.json")));

        var ids = await client.GetMatchIdsAsync(RegionalRoute.Europe, "abc", new MatchIdQuery(Start: 20, Count: 10, Queue: 420));

        Assert.Equal(3, ids.Count);
        Assert.Equal("?start=20&count=10&queue=420", stub.Requests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task Un_404_renvoie_null()
    {
        var (client, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Assert.Null(await client.GetAccountByRiotIdAsync(RegionalRoute.Europe, "x", "y"));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Une_cle_refusee_leve_RiotApiAuthException(HttpStatusCode status)
    {
        var (client, _) = Create(_ => new HttpResponseMessage(status));
        await Assert.ThrowsAsync<RiotApiAuthException>(() => client.GetSummonerByPuuidAsync(Platform.Euw1, "x"));
    }

    [Fact]
    public async Task Un_429_est_rejoue_apres_retry_after()
    {
        var calls = 0;
        var (client, stub) = Create(_ =>
        {
            if (calls++ == 0)
            {
                var limited = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                limited.Headers.Add("Retry-After", "0");
                limited.Headers.Add("X-Rate-Limit-Type", "service");
                return limited;
            }

            return StubHttpHandler.Json(Fixture.Read("riot/summoner.json"));
        });

        var summoner = await client.GetSummonerByPuuidAsync(Platform.Euw1, "x");

        Assert.Equal(412, summoner!.SummonerLevel);
        Assert.Equal(2, stub.Requests.Count);
    }

    [Fact]
    public async Task Les_429_repetes_finissent_par_une_erreur()
    {
        var (client, stub) = Create(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var ex = await Assert.ThrowsAsync<RiotApiException>(() => client.GetSummonerByPuuidAsync(Platform.Euw1, "x"));
        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Equal(4, stub.Requests.Count); // 1 + 3 tentatives
    }
}
