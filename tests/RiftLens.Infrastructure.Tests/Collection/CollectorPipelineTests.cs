using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RiftLens.Domain.Entities;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Collection;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Riot;
using RiftLens.Infrastructure.Riot.Dtos;
using RiftLens.Infrastructure.Stats;
using RiftLens.Infrastructure.Tests.Support;

namespace RiftLens.Infrastructure.Tests.Collection;

public sealed class CollectorPipelineTests : IDisposable
{
    private readonly SqliteDb _db = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-13T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
    private readonly FixtureRiotClient _riot = new()
    {
        // Chaque joueur du ladder a 2 matchs, dont un partagé avec le joueur suivant.
        MatchIdsByPuuid = puuid => [$"EUW1_{puuid[^2..]}A", $"EUW1_{(int.Parse(puuid[^2..], System.Globalization.CultureInfo.InvariantCulture) + 1):D2}A"],
    };

    private CollectorPipeline Create(CollectorOptions? options = null)
    {
        var ctx = _db.Create();
        var aggregator = new StatsAggregator(ctx, new FixtureStaticData(), _time, NullLogger<StatsAggregator>.Instance);
        return new CollectorPipeline(ctx, _riot, aggregator, Options.Create(options ?? new CollectorOptions()), _time, NullLogger<CollectorPipeline>.Instance);
    }

    [Fact]
    public async Task Cycle_complet_seed_crawl_fetch_aggregate()
    {
        Assert.True(await Create().IsSeedDueAsync(default));
        Assert.Equal(3, await Create().SeedAsync(default));
        Assert.False(await Create().IsSeedDueAsync(default));

        Assert.Equal(4, await Create().CrawlAsync(default)); // 90A, 91A, 92A, 93A (doublons fusionnés)
        Assert.Equal(4, await Create().FetchAsync(default));
        Assert.Equal(0, await Create().CountPendingAsync(default));

        Assert.Equal(4, await Create().AggregateAsync(default));
        await using var ctx = _db.Create();
        Assert.All(await ctx.Matches.ToListAsync(), m => Assert.Equal(RankTier.Challenger, m.SourceTier));
    }

    [Fact]
    public async Task Reprise_apres_interruption_sans_double_telechargement()
    {
        await Create().SeedAsync(default);
        await Create().CrawlAsync(default);

        // « Crash » après un premier lot partiel.
        await Create(new CollectorOptions { FetchBatchSize = 1 }).FetchAsync(default);
        Assert.Equal(1, _riot.MatchCalls);

        // Redémarrage : nouveau pipeline, même base. Les Pending restants sont repris.
        Assert.Equal(3, await Create().CountPendingAsync(default));
        Assert.Equal(3, await Create().FetchAsync(default));
        Assert.Equal(4, _riot.MatchCalls);

        // Un nouveau crawl ne remet pas en file les matchs déjà traités.
        _time.Advance(TimeSpan.FromHours(1));
        Assert.Equal(0, await Create().CrawlAsync(default));
    }

    [Fact]
    public async Task Un_match_deja_stocke_via_un_profil_est_rattache_sans_retelechargement()
    {
        await using (var ctx = _db.Create())
        {
            var dto = Fixture.Match();
            ctx.Matches.Add(MatchMapper.ToEntity(dto with { Metadata = dto.Metadata with { MatchId = "EUW1_90A" } }));
            await ctx.SaveChangesAsync();
        }

        await Create().SeedAsync(default);
        Assert.Equal(3, await Create().CrawlAsync(default));
        await Create().FetchAsync(default);

        Assert.Equal(3, _riot.MatchCalls);
        await using var check = _db.Create();
        Assert.Equal(RankTier.Challenger, (await check.Matches.SingleAsync(m => m.MatchId == "EUW1_90A")).SourceTier);
    }

    [Fact]
    public async Task Le_crawl_visite_d_abord_les_joueurs_jamais_parcourus()
    {
        await Create().SeedAsync(default);
        await Create(new CollectorOptions { PlayersPerCycle = 2 }).CrawlAsync(default);
        _time.Advance(TimeSpan.FromMinutes(1));
        await Create(new CollectorOptions { PlayersPerCycle = 1 }).CrawlAsync(default);

        await using var ctx = _db.Create();
        Assert.Equal(0, await ctx.CollectorPlayers.CountAsync(p => p.LastCrawledAt == null));
    }

    [Fact]
    public async Task Les_echecs_repetes_passent_le_match_en_Failed()
    {
        await using (var ctx = _db.Create())
        {
            ctx.QueuedMatches.Add(new QueuedMatch { MatchId = "EUW1_X", SourceTier = RankTier.Master, EnqueuedAt = _time.GetUtcNow() });
            await ctx.SaveChangesAsync();
        }

        var failing = new FailingRiotClient();
        for (var i = 0; i < 3; i++)
        {
            var ctx = _db.Create();
            await new CollectorPipeline(ctx, failing, null!, Options.Create(new CollectorOptions()), _time, NullLogger<CollectorPipeline>.Instance).FetchAsync(default);
        }

        await using var check = _db.Create();
        var item = await check.QueuedMatches.SingleAsync();
        Assert.Equal((QueueStatus.Failed, 3), (item.Status, item.Attempts));
    }

    private sealed class FailingRiotClient : IRiotApiClient
    {
        private readonly FixtureRiotClient _inner = new();
        public Task<RiftLens.Infrastructure.Riot.Dtos.AccountDto?> GetAccountByRiotIdAsync(RegionalRoute r, string g, string t, CancellationToken ct = default) => _inner.GetAccountByRiotIdAsync(r, g, t, ct);
        public Task<RiftLens.Infrastructure.Riot.Dtos.SummonerDto?> GetSummonerByPuuidAsync(Platform p, string id, CancellationToken ct = default) => _inner.GetSummonerByPuuidAsync(p, id, ct);
        public Task<IReadOnlyList<RiftLens.Infrastructure.Riot.Dtos.LeagueEntryDto>> GetLeagueEntriesByPuuidAsync(Platform p, string id, CancellationToken ct = default) => _inner.GetLeagueEntriesByPuuidAsync(p, id, ct);
        public Task<RiftLens.Infrastructure.Riot.Dtos.LeagueListDto?> GetApexLeagueAsync(Platform p, RankTier t, CancellationToken ct = default) => _inner.GetApexLeagueAsync(p, t, ct);
        public Task<IReadOnlyList<RiftLens.Infrastructure.Riot.Dtos.ChampionMasteryDto>> GetTopMasteriesAsync(Platform p, string id, int c, CancellationToken ct = default) => _inner.GetTopMasteriesAsync(p, id, c, ct);
        public Task<IReadOnlyList<string>> GetMatchIdsAsync(RegionalRoute r, string id, MatchIdQuery q, CancellationToken ct = default) => _inner.GetMatchIdsAsync(r, id, q, ct);
        public Task<RiftLens.Infrastructure.Riot.Dtos.MatchDto?> GetMatchAsync(RegionalRoute r, string id, CancellationToken ct = default) => throw new RiotApiException(System.Net.HttpStatusCode.InternalServerError, "boom");
        public Task<bool> ValidateKeyAsync(Platform p, string key, CancellationToken ct = default) => _inner.ValidateKeyAsync(p, key, ct);
        public Task<RiftLens.Infrastructure.Riot.Dtos.TimelineDto?> GetTimelineAsync(RegionalRoute r, string id, CancellationToken ct = default) => _inner.GetTimelineAsync(r, id, ct);
    }

    public void Dispose() => _db.Dispose();
}
